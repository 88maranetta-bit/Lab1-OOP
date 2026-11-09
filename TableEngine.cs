using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace La1
{
	public class TableEngine
	{
		private readonly Dictionary<string, string> _cellFormulas = new(StringComparer.OrdinalIgnoreCase);

		public IReadOnlyDictionary<string, string> CellFormulas => _cellFormulas;

		public void SetCellFormula(string cellName, string formula)
		{
			if (string.IsNullOrWhiteSpace(formula))
			{
				_cellFormulas.Remove(cellName);
			}
			else
			{
				_cellFormulas[cellName] = formula.Trim();
			}
		}

		public void Clear()
		{
			_cellFormulas.Clear();
		}

		public void ValidateFormula(string formula)
		{
			string lower = formula.ToLower();
			var matches = Regex.Matches(lower, @"[a-zA-Z]+");
			foreach (Match m in matches)
			{
				string w = m.Value;
				if (w != "max" && w != "min" && w != "div" && w != "mod")
				{
					int index = m.Index + m.Length;
					bool isCellRef = index < lower.Length && char.IsDigit(lower[index]);
					if (!isCellRef)
					{
						throw new Exception("Помилка імені");
					}
				}
			}
		}

		public double EvaluateFormula(string cellName, string formulaText, Func<string, double> cellValueGetter)
		{
			string formula = formulaText.StartsWith("=") ? formulaText.Substring(1) : formulaText;
			ValidateFormula(formula);
			var evaluator = new FormulaEvaluatorVisitor(cellValueGetter);
			return evaluator.Evaluate(formula);
		}

		public string GetErrorMessage(Exception ex)
		{
			string msg = ex.Message;
			if (msg.Contains("циклу")) return "ERR: CYCLE";
			if (msg.Contains("ділення на нуль", StringComparison.OrdinalIgnoreCase) || msg.Contains("Division by zero", StringComparison.OrdinalIgnoreCase)) return "#DIV/0!";
			if (msg.Contains("ім'я", StringComparison.OrdinalIgnoreCase) || msg.Contains("name", StringComparison.OrdinalIgnoreCase)) return "#NAME?";
			if (msg.Contains("значення", StringComparison.OrdinalIgnoreCase) || msg.Contains("value", StringComparison.OrdinalIgnoreCase)) return "#VALUE!";
			return "#REF!";
		}

		public void LoadFormulas(Dictionary<string, string> loadedFormulas)
		{
			_cellFormulas.Clear();
			foreach (var kvp in loadedFormulas)
			{
				if (kvp.Value.StartsWith("="))
				{
					_cellFormulas[kvp.Key] = kvp.Value;
				}
			}
		}
	}
}