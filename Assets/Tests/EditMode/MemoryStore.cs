using System.Collections.Generic;
using GeoPuzzle.Core;

namespace GeoPuzzle.Tests
{
    /// <summary>In-memory save data for tests.</summary>
    public class MemoryStore : ISaveStore
    {
        readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public bool HasKey(string key) => values.ContainsKey(key);
        public int GetInt(string key, int fallback) => values.TryGetValue(key, out var v) ? (int)v : fallback;
        public void SetInt(string key, int value) => values[key] = value;
        public string GetString(string key, string fallback) => values.TryGetValue(key, out var v) ? (string)v : fallback;
        public void SetString(string key, string value) => values[key] = value;
        public void Save() { }
    }
}
