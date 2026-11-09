using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace La1
{
    public class StorageHandler
    {
        public void Save(string filePath, IReadOnlyDictionary<string, string> cellFormulas, Dictionary<string, string> currentGridValues)
        {
            var cellData = new Dictionary<string, string>(cellFormulas);
            foreach (var kvp in currentGridValues)
            {
                if (!string.IsNullOrEmpty(kvp.Value) && !kvp.Value.StartsWith("=") && !cellData.ContainsKey(kvp.Key))
                {
                    cellData[kvp.Key] = kvp.Value;
                }
            }
            string jsonString = JsonSerializer.Serialize(cellData);
            File.WriteAllText(filePath, jsonString);
        }

        public Dictionary<string, string> Load(string filePath)
        {
            if (!File.Exists(filePath)) return new Dictionary<string, string>();
            string jsonString = File.ReadAllText(filePath);
            var cellData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
            return cellData ?? new Dictionary<string, string>();
        }
    }
}