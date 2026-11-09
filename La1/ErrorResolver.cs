using System;

namespace La1
{
    public static class ErrorResolver
    {
        public static string Resolve(string message)
        {
            if (message.Contains("цикл", StringComparison.OrdinalIgnoreCase)) return "ERR: CYCLE";
            if (message.Contains("імені", StringComparison.OrdinalIgnoreCase)) return "ERR: NAME";
            if (message.Contains("посилання", StringComparison.OrdinalIgnoreCase)) return "ERR: REF";
            if (message.Contains("ділення", StringComparison.OrdinalIgnoreCase) || message.Contains("Zero")) return "ERR: DIV/0";
            if (message.Contains("значення", StringComparison.OrdinalIgnoreCase) || message.Contains("Input string was not in a correct format")) return "ERR: VALUE";
            if (message.Contains("N/A", StringComparison.OrdinalIgnoreCase)) return "#N/A";
            return "ERR!";
        }
    }
}