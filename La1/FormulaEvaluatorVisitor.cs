using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace La1
{
    public class FormulaEvaluatorVisitor
    {
        private readonly Func<string, double> _cellValueGetter;
        private readonly HashSet<string> _visitedCells;

        public FormulaEvaluatorVisitor(Func<string, double> cellValueGetter, HashSet<string>? visitedCells = null)
        {
            _cellValueGetter = cellValueGetter;
            _visitedCells = visitedCells ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public double Evaluate(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return 0.0;

            formula = formula.Trim();

            if (formula.Count(c => c == '=') > 1 || (formula.Contains("=") && !formula.StartsWith("=")))
            {
                throw new Exception("Помилка значення");
            }

            if (formula.StartsWith("="))
                formula = formula.Substring(1);

            formula = formula.Trim();

            if (formula.Contains("="))
            {
                throw new Exception("Помилка значення");
            }

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

            return ParseExpression(ref formula);
        }

        private double ParseExpression(ref string s)
        {
            double value = ParseTerm(ref s);
            s = s.Trim();
            while (s.Length > 0 && (s[0] == '+' || s[0] == '-'))
            {
                char op = s[0];
                s = s.Substring(1).Trim();
                double right = ParseTerm(ref s);
                value = op == '+' ? value + right : value - right;
                s = s.Trim();
            }
            return value;
        }

        private double ParseTerm(ref string s)
        {
            double value = ParsePower(ref s);
            s = s.Trim();
            while (s.Length > 0)
            {
                if (s.StartsWith("div", StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Substring(3).Trim();
                    double right = ParsePower(ref s);
                    if (right == 0) throw new DivideByZeroException("Ділення на нуль");
                    value = Math.Truncate(value / right);
                }
                else if (s.StartsWith("mod", StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Substring(3).Trim();
                    double right = ParsePower(ref s);
                    if (right == 0) throw new DivideByZeroException("Ділення на нуль");
                    value = value % right;
                }
                else if (s[0] == '*' || s[0] == '/')
                {
                    char op = s[0];
                    s = s.Substring(1).Trim();
                    double right = ParsePower(ref s);
                    if (op == '/' && right == 0) throw new DivideByZeroException("Ділення на нуль");
                    value = op == '*' ? value * right : value / right;
                }
                else
                {
                    break;
                }
                s = s.Trim();
            }
            return value;
        }

        private double ParsePower(ref string s)
        {
            double value = ParseFactor(ref s);
            s = s.Trim();
            if (s.Length > 0 && s[0] == '^')
            {
                s = s.Substring(1).Trim();
                double right = ParsePower(ref s);
                value = Math.Pow(value, right);
            }
            return value;
        }

        private double ParseFactor(ref string s)
        {
            s = s.Trim();

            if (s.StartsWith("("))
            {
                int depth = 0;
                int closeIndex = -1;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == '(') depth++;
                    else if (s[i] == ')') depth--;

                    if (depth == 0)
                    {
                        closeIndex = i;
                        break;
                    }
                }

                if (closeIndex > 0)
                {
                    string inner = s.Substring(1, closeIndex - 1);
                    s = s.Substring(closeIndex + 1).Trim();
                    return Evaluate(inner);
                }
            }

            if (s.StartsWith("max(", StringComparison.OrdinalIgnoreCase) || s.StartsWith("min(", StringComparison.OrdinalIgnoreCase))
            {
                bool isMax = s.StartsWith("max(", StringComparison.OrdinalIgnoreCase);
                int len = 4;
                int depth = 0;
                int closeIndex = -1;
                for (int i = len - 1; i < s.Length; i++)
                {
                    if (s[i] == '(') depth++;
                    else if (s[i] == ')') depth--;

                    if (depth == 0)
                    {
                        closeIndex = i;
                        break;
                    }
                }

                if (closeIndex > 0)
                {
                    string argsStr = s.Substring(len, closeIndex - len);
                    s = s.Substring(closeIndex + 1).Trim();
                    var parts = SplitArguments(argsStr);
                    if (parts.Length == 2)
                    {
                        double v1 = Evaluate(parts[0]);
                        double v2 = Evaluate(parts[1]);
                        return isMax ? Math.Max(v1, v2) : Math.Min(v1, v2);
                    }
                }
            }

            int matchLen = 0;
            while (matchLen < s.Length && (char.IsLetterOrDigit(s[matchLen]) || s[matchLen] == '.'))
            {
                matchLen++;
            }

            if (matchLen > 0)
            {
                string token = s.Substring(0, matchLen);
                s = s.Substring(matchLen).Trim();
                return GetValue(token);
            }

            throw new Exception("Помилка імені");
        }

        private string[] SplitArguments(string argsStr)
        {
            var parts = new List<string>();
            int parenthesisDepth = 0;
            int lastIndex = 0;

            for (int i = 0; i < argsStr.Length; i++)
            {
                if (argsStr[i] == '(') parenthesisDepth++;
                else if (argsStr[i] == ')') parenthesisDepth--;
                else if (parenthesisDepth == 0 && argsStr[i] == ',')
                {
                    parts.Add(argsStr.Substring(lastIndex, i - lastIndex));
                    lastIndex = i + 1;
                }
            }
            parts.Add(argsStr.Substring(lastIndex));
            return parts.ToArray();
        }

        private double GetValue(string token)
        {
            token = token.Trim().ToUpper();

            if (Regex.IsMatch(token, @"^[A-Z]+\d+$"))
            {
                if (_visitedCells.Contains(token))
                {
                    throw new Exception("Помилка циклу");
                }

                _visitedCells.Add(token);
                try
                {
                    double val = _cellValueGetter(token);
                    return val;
                }
                finally
                {
                    _visitedCells.Remove(token);
                }
            }

            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double resultVal))
            {
                return resultVal;
            }

            throw new Exception("Помилка імені");
        }
    }
}