using System.Linq;
using GeoPuzzle.Data;
using GeoPuzzle.Quiz;
using NUnit.Framework;

namespace GeoPuzzle.Tests
{
    public class QuestionGeneratorTests
    {
        static CountryDatabase MakeDatabase() => new CountryDatabase(new[]
        {
            Country("ESP", "Europe", "Southern Europe"),
            Country("PRT", "Europe", "Southern Europe"),
            Country("ITA", "Europe", "Southern Europe"),
            Country("GRC", "Europe", "Southern Europe"),
            Country("FRA", "Europe", "Western Europe"),
            Country("JPN", "Asia", "Eastern Asia"),
            Country("KEN", "Africa", "Eastern Africa"),
        });

        static CountryRecord Country(string id, string continent, string subregion) =>
            new CountryRecord { id = id, nameKey = id, flag = id, continent = continent, subregion = subregion };

        [Test]
        public void OptionsContainTargetOnceAtCorrectIndex()
        {
            var generator = new QuestionGenerator(MakeDatabase(), seed: 1);

            for (var i = 0; i < 50; i++)
            {
                var q = generator.Create(QuestionKind.WhichFlag, "ESP");
                Assert.AreEqual(4, q.Options.Count);
                Assert.AreEqual("ESP", q.Options[q.CorrectIndex]);
                Assert.AreEqual(1, q.Options.Count(o => o == "ESP"));
                Assert.AreEqual(q.Options.Count, q.Options.Distinct().Count());
            }
        }

        [Test]
        public void WrongOptionsPreferTheSameSubregion()
        {
            var generator = new QuestionGenerator(MakeDatabase(), seed: 7);

            var q = generator.Create(QuestionKind.CapitalOf, "ESP");

            var wrong = q.Options.Where(o => o != "ESP");
            CollectionAssert.AreEquivalent(new[] { "PRT", "ITA", "GRC" }, wrong);
        }

        [Test]
        public void FallsBackToContinentThenWorld()
        {
            var generator = new QuestionGenerator(MakeDatabase(), seed: 3);

            var q = generator.Create(QuestionKind.WhichFlag, "JPN", optionCount: 3);

            Assert.AreEqual(3, q.Options.Count);
            Assert.Contains("JPN", q.Options.ToList());
        }

        [Test]
        public void FindOnMapHasOnlyTheTarget()
        {
            var generator = new QuestionGenerator(MakeDatabase(), seed: 1);

            var q = generator.Create(QuestionKind.FindOnMap, "KEN");

            Assert.IsTrue(q.IsCorrect("KEN"));
            Assert.IsFalse(q.IsCorrect("JPN"));
        }
    }
}
