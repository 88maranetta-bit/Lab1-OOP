using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace La1
{
    public class TableEngine
    {
        private readonly Dictionary<string, string> _cellFormulas = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> CellFormulas => _cellFormulas;

        public void SetCellFormula(string cellName, string formula)
        {
            cellName = cellName?.Trim().ToUpper() ?? "";
            if (string.IsNullOrWhiteSpace(formula))
            {
                _cellFormulas.Remove(cellName);
            }
            else
            {
                formula = formula.Trim();
                if (!formula.StartsWith("=") && (formula.Contains("+") || formula.Contains("-") || formula.Contains("*") || formula.Contains("/") || formula.Contains("^") || formula.Contains("(") || Regex.IsMatch(formula, @"[a-zA-Z]")))
                {
                    formula = "=" + formula;
                }

                if (formula.StartsWith("="))
                {
                    _cellFormulas[cellName] = formula;
                }
                else
                {
                    _cellFormulas.Remove(cellName);
                }
            }
        }

        public void Clear()
        {
            _cellFormulas.Clear();
        }

        public void ValidateFormula(string formula)
        {
            string lower = formula.ToLower();

            var matches = Regex.Matches(lower, @"[a-z]+");
            foreach (Match m in matches)
            {
                string word = m.Value;

                if (word == "max" || word == "min" || word == "div" || word == "mod" || word.Length == 1)
                    continue;

                int index = m.Index + m.Length;
                bool isCellRef = index < lower.Length && char.IsDigit(lower[index]);

                if (!isCellRef || word.Length > 2)
                {
                    throw new Exception("Помилка імені");
                }
            }
        }

        public double EvaluateFormula(string cellName, string formulaText, Func<string, HashSet<string>, double> cellValueGetter, HashSet<string>? visiting = null)
        {
            cellName = cellName?.Trim().ToUpper() ?? "";
            visiting ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string formula = formulaText.StartsWith("=") ? formulaText.Substring(1).Trim() : formulaText.Trim();
            ValidateFormula(formula);

            var evaluator = new FormulaEvaluatorVisitor(name => cellValueGetter(name?.ToUpper(), visiting), visiting);
            return evaluator.Evaluate(formula);
        }

        public void SaveState(string filePath, Dictionary<string, string> currentGridValues)
        {
            var cellData = new Dictionary<string, string>(_cellFormulas);
            foreach (var kvp in currentGridValues)
            {
                if (!string.IsNullOrEmpty(kvp.Value) && !kvp.Value.StartsWith("=") && !cellData.ContainsKey(kvp.Key))
                {
                    cellData[kvp.Key.ToUpper()] = kvp.Value;
                }
            }
            string jsonString = JsonSerializer.Serialize(cellData);
            File.WriteAllText(filePath, jsonString);
        }

        public void LoadState(string filePath)
        {
            if (!File.Exists(filePath)) return;
            string jsonString = File.ReadAllText(filePath);
            var cellData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            _cellFormulas.Clear();
            if (cellData == null) return;

            foreach (var kvp in cellData)
            {
                if (kvp.Value.StartsWith("="))
                {
                    _cellFormulas[kvp.Key.ToUpper()] = kvp.Value;
                }
            }
        }
    }
}