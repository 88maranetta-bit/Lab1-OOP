using System;

namespace La1
{
    public static class CellErrorCatcher
    {
        public static string ResolveError(Exception ex)
        {
            string message = ex.Message;

            if (message.Contains("циклу", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("cycle", StringComparison.OrdinalIgnoreCase) ||
                message == "ERR: CYCLE")
            {
                return "ERR: CYCLE";
            }

            if (ex is DivideByZeroException ||
                message.Contains("ділення на нуль", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("division by zero", StringComparison.OrdinalIgnoreCase) ||
                message == "#DIV/0!")
            {
                return "#DIV/0!";
            }

            if (message.Contains("ім'я", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("імені", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("name", StringComparison.OrdinalIgnoreCase) ||
                message == "#NAME?")
            {
                return "#NAME?";
            }

            if (message.Contains("значення", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("value", StringComparison.OrdinalIgnoreCase) ||
                message == "#VALUE!")
            {
                return "#VALUE!";
            }

            if (message.Contains("посилання", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("reference", StringComparison.OrdinalIgnoreCase) ||
                message == "#REF!")
            {
                return "#REF!";
            }

            return "#REF!";
        }
    }
}