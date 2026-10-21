using System.Linq;
using GeoPuzzle.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GeoPuzzle.Tests
{
    public class CountryDatabaseTests
    {
        const string CountriesJsonPath = "Assets/_Project/Data/countries.json";

        static CountryDatabase LoadShipped()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CountriesJsonPath);
            Assert.IsNotNull(asset, $"{CountriesJsonPath} is missing");
            return CountryDatabase.FromJson(asset.text);
        }

        [Test]
        public void ShippedCountryDataIsValid()
        {
            var db = LoadShipped();
            Assert.IsNotEmpty(db.All);
            CollectionAssert.IsEmpty(db.Validate());
        }

        [Test]
        public void LooksUpCountriesById()
        {
            var db = LoadShipped();
            Assert.AreEqual("FR", db.Get("FRA").iso2);
            Assert.IsFalse(db.TryGet("XXX", out _));
        }

        [Test]
        public void ValidateReportsDuplicatesAndUnknownNeighbours()
        {
            var db = new CountryDatabase(new[]
            {
                new CountryRecord { id = "AAA", nameKey = "a", flag = "a", continent = "Europe", neighbours = new[] { "ZZZ" } },
                new CountryRecord { id = "AAA", nameKey = "a", flag = "a", continent = "Europe" },
            });

            var errors = db.Validate();

            Assert.IsTrue(errors.Any(e => e.Contains("Duplicate")));
            Assert.IsTrue(errors.Any(e => e.Contains("ZZZ")));
        }
    }
}
