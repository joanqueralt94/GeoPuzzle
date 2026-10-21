using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace GeoPuzzle.Core
{
    /// <summary>
    /// Tunable game values (economy, timers, ads, prices) read from a flat key -> value JSON file
    /// exported from Tools/economy/economy_config.xlsx. Key names match Remote Config keys, so remote
    /// values can later be layered on top with <see cref="ApplyOverrides"/>.
    /// Game code should read values from here instead of hardcoding them.
    /// </summary>
    public class GameConfig
    {
        readonly Dictionary<string, object> defaults;
        readonly Dictionary<string, object> overrides = new Dictionary<string, object>(StringComparer.Ordinal);

        public GameConfig(IDictionary<string, object> defaults)
        {
            this.defaults = new Dictionary<string, object>(defaults, StringComparer.Ordinal);
        }

        public static GameConfig FromJson(string json) => new GameConfig(ParseFlatJson(json));

        public IEnumerable<string> Keys => defaults.Keys;

        public bool Has(string key) => overrides.ContainsKey(key) || defaults.ContainsKey(key);

        /// <summary>Layers values on top of the local defaults, for example from Remote Config.</summary>
        public void ApplyOverrides(IDictionary<string, object> values)
        {
            foreach (var pair in values)
                overrides[pair.Key] = pair.Value;
        }

        public void ClearOverrides() => overrides.Clear();

        public int GetInt(string key) => Convert.ToInt32(Get(key), CultureInfo.InvariantCulture);

        public float GetFloat(string key) => Convert.ToSingle(Get(key), CultureInfo.InvariantCulture);

        public bool GetBool(string key) => Convert.ToBoolean(Get(key), CultureInfo.InvariantCulture);

        public string GetString(string key) => Convert.ToString(Get(key), CultureInfo.InvariantCulture);

        object Get(string key)
        {
            if (overrides.TryGetValue(key, out var value) || defaults.TryGetValue(key, out value))
                return value;
            throw new KeyNotFoundException($"Config key '{key}' is missing; add it to economy_config.xlsx and re-export");
        }

        static Dictionary<string, object> ParseFlatJson(string json)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var property in JObject.Parse(json).Properties())
            {
                if (!(property.Value is JValue value) || value.Value == null)
                    throw new FormatException($"Config key '{property.Name}' must be a number, bool or string");
                result[property.Name] = value.Value;
            }
            return result;
        }
    }
}
