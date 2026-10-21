using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GeoPuzzle.Data
{
    /// <summary>Read-only lookup over all countries, built once at boot.</summary>
    public class CountryDatabase
    {
        readonly Dictionary<string, CountryRecord> byId;

        public IReadOnlyList<CountryRecord> All { get; }

        public CountryDatabase(IEnumerable<CountryRecord> countries)
        {
            All = countries.ToList();
            byId = new Dictionary<string, CountryRecord>(StringComparer.Ordinal);
            foreach (var country in All)
                byId[country.id] = country;
        }

        public static CountryDatabase FromJson(string json)
        {
            var list = JsonUtility.FromJson<CountryList>(json);
            return new CountryDatabase(list?.countries ?? Array.Empty<CountryRecord>());
        }

        public bool TryGet(string id, out CountryRecord country) => byId.TryGetValue(id, out country);

        public CountryRecord Get(string id) =>
            byId.TryGetValue(id, out var country) ? country : throw new KeyNotFoundException($"Unknown country id '{id}'");

        public IEnumerable<CountryRecord> InSubregion(string subregion) => All.Where(c => c.subregion == subregion);

        public IEnumerable<CountryRecord> InContinent(string continent) => All.Where(c => c.continent == continent);

        /// <summary>Returns one message per problem found; an empty list means the data is valid.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in All)
            {
                if (string.IsNullOrEmpty(c.id) || c.id.Length != 3)
                    errors.Add($"Country '{c.id}' needs a 3-letter ISO id");
                else if (!seen.Add(c.id))
                    errors.Add($"Duplicate country id '{c.id}'");
                if (string.IsNullOrEmpty(c.nameKey))
                    errors.Add($"{c.id}: missing nameKey");
                if (string.IsNullOrEmpty(c.flag))
                    errors.Add($"{c.id}: missing flag");
                if (string.IsNullOrEmpty(c.continent))
                    errors.Add($"{c.id}: missing continent");
            }
            foreach (var c in All)
                foreach (var n in c.neighbours)
                    if (!byId.ContainsKey(n))
                        errors.Add($"{c.id}: neighbour '{n}' is not in the database");
            return errors;
        }
    }
}
