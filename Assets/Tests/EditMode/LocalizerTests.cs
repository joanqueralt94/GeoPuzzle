using GeoPuzzle.Core;
using GeoPuzzle.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GeoPuzzle.Tests
{
    public class LocalizerTests
    {
        [Test]
        public void LaterFilesOverrideAndMissingKeysShowTheKey()
        {
            var texts = new Localizer("en", new[]
            {
                "{\"ui.a\": \"First\", \"ui.count\": \"{0} of {1}\"}",
                "{\"ui.a\": \"Second\"}",
            });

            Assert.AreEqual("Second", texts.Get("ui.a"));
            Assert.AreEqual("3 of 10", texts.Format("ui.count", 3, 10));
            Assert.AreEqual("ui.missing", texts.Get("ui.missing"));
            Assert.IsFalse(texts.Has("ui.missing"));
        }

        [Test]
        public void ShippedTextsCoverEveryCountryAndUiKey()
        {
            var countries = CountryDatabase.FromJson(Load("Assets/_Project/Data/countries.json"));
            var texts = new Localizer("en", new[]
            {
                Load("Assets/_Project/Data/Localization/countries.en.json"),
                Load("Assets/_Project/Data/Localization/ui.en.json"),
            });

            foreach (var country in countries.All)
                Assert.IsTrue(texts.Has(country.nameKey), $"{country.id} has no English name");
            Assert.IsTrue(texts.Has("ui.quiz.which_country"));
        }

        static string Load(string path) => AssetDatabase.LoadAssetAtPath<TextAsset>(path).text;
    }
}
