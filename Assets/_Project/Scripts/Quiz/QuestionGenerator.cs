using System;
using System.Collections.Generic;
using System.Linq;
using GeoPuzzle.Data;

namespace GeoPuzzle.Quiz
{
    /// <summary>
    /// Builds questions from country data. Wrong options come from the target's subregion first,
    /// then its continent, then anywhere, so they stay fair but not trivial.
    /// </summary>
    public class QuestionGenerator
    {
        readonly CountryDatabase countries;
        readonly Random random;

        public QuestionGenerator(CountryDatabase countries, int seed)
        {
            this.countries = countries;
            random = new Random(seed);
        }

        public GeneratedQuestion Create(QuestionKind kind, string targetId, int optionCount = 4)
        {
            var target = countries.Get(targetId);
            if (kind == QuestionKind.FindOnMap)
                return new GeneratedQuestion(kind, target.id, new[] { target.id }, 0);

            var options = PickDistractors(target, optionCount - 1);
            var correctIndex = random.Next(options.Count + 1);
            options.Insert(correctIndex, target.id);
            return new GeneratedQuestion(kind, target.id, options, correctIndex);
        }

        public GeneratedQuestion CreateRandom(QuestionKind kind, int optionCount = 4)
        {
            var target = countries.All[random.Next(countries.All.Count)];
            return Create(kind, target.id, optionCount);
        }

        List<string> PickDistractors(CountryRecord target, int count)
        {
            var picked = new List<string>();
            var pools = new[]
            {
                countries.InSubregion(target.subregion),
                countries.InContinent(target.continent),
                countries.All,
            };
            foreach (var pool in pools)
            {
                var candidates = pool
                    .Where(c => c.id != target.id && !picked.Contains(c.id))
                    .Select(c => c.id)
                    .OrderBy(_ => random.Next())
                    .Take(count - picked.Count);
                picked.AddRange(candidates);
                if (picked.Count == count)
                    break;
            }
            return picked;
        }
    }
}
