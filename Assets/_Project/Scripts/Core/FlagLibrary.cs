using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeoPuzzle.Core
{
    /// <summary>
    /// Maps flag names from countries.json ("flag_fr") to sprites. Filled by
    /// GeoPuzzle > Setup > Build Flag Library from Assets/_Project/Art/Flags.
    /// </summary>
    [CreateAssetMenu(menuName = "GeoPuzzle/Flag Library")]
    public class FlagLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string flag;
            public Sprite sprite;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        Dictionary<string, Sprite> lookup;

        public int Count => entries.Count;

        public Sprite Get(string flag)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (var entry in entries)
                    lookup[entry.flag] = entry.sprite;
            }
            return flag != null && lookup.TryGetValue(flag, out var sprite) ? sprite : null;
        }

        public void SetEntries(IEnumerable<Entry> newEntries)
        {
            entries = new List<Entry>(newEntries);
            lookup = null;
        }
    }
}
