using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace La1
{
    public class FormulaEvaluatorVisitor
    {
        private readonly Func<string, double> _cellValueGetter;

        public FormulaEvaluatorVisitor(Func<string, double> cellValueGetter)
        {
            _cellValueGetter = cellValueGetter;
        }

        public double Evaluate(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return 0.0;

            formula = formula.Trim();
            if (formula.StartsWith("="))
                formula = formula.Substring(1);

            formula = formula.Trim();

            if (formula.StartsWith("max(", StringComparison.OrdinalIgnoreCase) && formula.EndsWith(")"))
            {
                var argsStr = formula.Substring(4, formula.Length - 5);
                var parts = SplitArguments(argsStr);
                if (parts.Length == 2)
                {
                    return Math.Max(Evaluate(parts[0]), Evaluate(parts[1]));
                }
            }
            if (formula.StartsWith("min(", StringComparison.OrdinalIgnoreCase) && formula.EndsWith(")"))
            {
                var argsStr = formula.Substring(4, formula.Length - 5);
                var parts = SplitArguments(argsStr);
                if (parts.Length == 2)
                {
                    return Math.Min(Evaluate(parts[0]), Evaluate(parts[1]));
                }
            }

            char[] lowPriorityOps = { '+', '-' };
            foreach (var op in lowPriorityOps)
            {
                int index = FindMainOperator(formula, op);
                if (index > 0)
                {
                    string leftPart = formula.Substring(0, index);
                    string rightPart = formula.Substring(index + 1);
                    double leftVal = Evaluate(leftPart);
                    double rightVal = Evaluate(rightPart);
                    return op == '+' ? leftVal + rightVal : leftVal - rightVal;
                }
            }

            int divIndex = FindKeyword(formula, "div");
            if (divIndex > 0)
            {
                string leftPart = formula.Substring(0, divIndex);
                string rightPart = formula.Substring(divIndex + 3);
                double leftVal = Evaluate(leftPart);
                double rightVal = Evaluate(rightPart);
                return rightVal != 0 ? Math.Truncate(leftVal / rightVal) : throw new DivideByZeroException("Ділення на нуль");
            }

            int modIndex = FindKeyword(formula, "mod");
            if (modIndex > 0)
            {
                string leftPart = formula.Substring(0, modIndex);
                string rightPart = formula.Substring(modIndex + 3);
                double leftVal = Evaluate(leftPart);
                double rightVal = Evaluate(rightPart);
                return rightVal != 0 ? leftVal % rightVal : throw new DivideByZeroException("Ділення на нуль");
            }

            char[] midPriorityOps = { '*', '/' };
            foreach (var op in midPriorityOps)
            {
                int index = FindMainOperator(formula, op);
                if (index > 0)
                {
                    string leftPart = formula.Substring(0, index);
                    string rightPart = formula.Substring(index + 1);
                    double leftVal = Evaluate(leftPart);
                    double rightVal = Evaluate(rightPart);
                    return op == '*' ? leftVal * rightVal : (rightVal != 0 ? leftVal / rightVal : throw new DivideByZeroException("Ділення на нуль"));
                }
            }

            int powIndex = FindMainOperator(formula, '^');
            if (powIndex > 0)
            {
                string leftPart = formula.Substring(0, powIndex);
                string rightPart = formula.Substring(powIndex + 1);
                double leftVal = Evaluate(leftPart);
                double rightVal = Evaluate(rightPart);
                return Math.Pow(leftVal, rightVal);
            }

            return GetValue(formula);
        }

        private int FindMainOperator(string s, char op)
        {
            int parenthesisDepth = 0;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == ')') parenthesisDepth++;
                else if (s[i] == '(') parenthesisDepth--;
                else if (parenthesisDepth == 0 && s[i] == op)
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindKeyword(string s, string keyword)
        {
            int parenthesisDepth = 0;
            for (int i = s.Length - keyword.Length; i >= 0; i--)
            {
                if (s[i] == ')') parenthesisDepth++;
                else if (s[i] == '(') parenthesisDepth--;
                else if (parenthesisDepth == 0 && string.Compare(s, i, keyword, 0, keyword.Length, true, CultureInfo.InvariantCulture) == 0)
                {
                    bool boundaryBefore = (i == 0 || !char.IsLetterOrDigit(s[i - 1]));
                    bool boundaryAfter = (i + keyword.Length == s.Length || !char.IsLetterOrDigit(s[i + keyword.Length]));
                    if (boundaryBefore && boundaryAfter)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private string[] SplitArguments(string argsStr)
        {
            var parts = new System.Collections.Generic.List<string>();
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
                return _cellValueGetter(token);
            }
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
            return 0.0;
        }
    }
}