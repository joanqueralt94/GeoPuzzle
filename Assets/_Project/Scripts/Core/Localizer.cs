using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace GeoPuzzle.Core
{
    /// <summary>
    /// Turns Localization keys ("country.FRA", "ui.menu.play") into text for the current language.
    /// Reads flat key -> text JSON files (Data/Localization/*.&lt;lang&gt;.json); a missing key shows the key itself.
    /// </summary>
    public class Localizer
    {
        readonly Dictionary<string, string> texts = new Dictionary<string, string>(StringComparer.Ordinal);

        public string Language { get; }

        public Localizer(string language, IEnumerable<string> jsonFiles)
        {
            Language = language;
            foreach (var json in jsonFiles)
            {
                var file = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                foreach (var pair in file)
                    texts[pair.Key] = pair.Value;
            }
        }

        public bool Has(string key) => texts.ContainsKey(key);

        public string Get(string key) => key != null && texts.TryGetValue(key, out var text) ? text : key;

        public string Format(string key, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }
}
