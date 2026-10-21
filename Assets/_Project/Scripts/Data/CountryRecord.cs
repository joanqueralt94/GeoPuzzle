using System;

namespace GeoPuzzle.Data
{
    /// <summary>One country or territory, keyed by its ISO 3166-1 alpha-3 code.</summary>
    [Serializable]
    public class CountryRecord
    {
        public string id;
        public string iso2;
        public string nameKey;
        public string capitalKey;
        public string continent;
        public string subregion;
        public long population;
        public float areaKm2;
        public string[] neighbours = Array.Empty<string>();
        public string flag;
        public int difficulty = 1;
        public string[] tags = Array.Empty<string>();
    }

    /// <summary>Root object of countries.json (JsonUtility cannot read a top-level array).</summary>
    [Serializable]
    public class CountryList
    {
        public CountryRecord[] countries = Array.Empty<CountryRecord>();
    }
}
