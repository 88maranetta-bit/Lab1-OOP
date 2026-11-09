using System;

namespace La1
{
    public static class CellErrorCatcher
    {
        public static string ResolveError(Exception ex)
        {
            if (ex is DivideByZeroException || 
                ex.Message.Contains("ділення на нуль", StringComparison.OrdinalIgnoreCase) || 
                ex.Message.Contains("division by zero", StringComparison.OrdinalIgnoreCase))
            {
                return "#DIV/0!";
            }

            string message = ex.Message;

            if (message.Contains("циклу", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("cycle", StringComparison.OrdinalIgnoreCase))
            {
                return "ERR: CYCLE";
            }

            if (message.Contains("ім'я", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("name", StringComparison.OrdinalIgnoreCase))
            {
                return "#NAME?";
            }

            if (message.Contains("значення", StringComparison.OrdinalIgnoreCase) || 
                message.Contains("value", StringComparison.OrdinalIgnoreCase))
            {
                return "#VALUE!";
            }

            return "#REF!";
        }
    }
}