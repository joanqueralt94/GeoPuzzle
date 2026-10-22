using System.Collections.Generic;
using System.Linq;
using GeoPuzzle.Data;
using GeoPuzzle.Map;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GeoPuzzle.Tests
{
    public class MapHitTesterTests
    {
        const string WorldMapPath = "Assets/_Project/Data/Map/world_map.json";
        const string CountriesJsonPath = "Assets/_Project/Data/countries.json";

        static MapPolygon Square(float minX, float minY, float size, params float[][] holes)
        {
            var vertices = new List<float> { minX, minY, minX + size, minY, minX + size, minY + size, minX, minY + size };
            var ringStarts = new List<int> { 0 };
            foreach (var hole in holes)
            {
                ringStarts.Add(vertices.Count / 2);
                vertices.AddRange(hole);
            }
            return new MapPolygon { vertices = vertices.ToArray(), ringStarts = ringStarts.ToArray(), triangles = new[] { 0, 1, 2 } };
        }

        static MapCountry Country(string id, MapPolygon polygon, bool playable = true, bool small = false)
        {
            var xs = Enumerable.Range(0, polygon.VertexCount).Select(i => polygon.vertices[2 * i]).ToArray();
            var ys = Enumerable.Range(0, polygon.VertexCount).Select(i => polygon.vertices[2 * i + 1]).ToArray();
            return new MapCountry
            {
                id = id,
                playable = playable,
                small = small,
                bounds = new[] { xs.Min(), ys.Min(), xs.Max(), ys.Max() },
                label = new[] { (xs.Min() + xs.Max()) / 2, (ys.Min() + ys.Max()) / 2 },
                polygons = new[] { polygon },
            };
        }

        [Test]
        public void FindsTheCountryUnderThePoint()
        {
            var tester = new MapHitTester(new[] { Country("AAA", Square(0, 0, 10)), Country("BBB", Square(10, 0, 10)) });

            Assert.AreEqual("AAA", tester.Find(5, 5, 0.1f));
            Assert.AreEqual("BBB", tester.Find(15, 5, 0.1f));
            Assert.IsNull(tester.Find(25, 5, 0.1f));
        }

        [Test]
        public void HolesAreNotPartOfTheCountry()
        {
            var lake = new float[] { 4, 4, 6, 4, 6, 6, 4, 6 };
            var tester = new MapHitTester(new[] { Country("AAA", Square(0, 0, 10, lake)) });

            Assert.IsNull(tester.Find(5, 5, 0.1f));
            Assert.AreEqual("AAA", tester.Find(2, 2, 0.1f));
        }

        [Test]
        public void SmallCountryWinsWithinFingerRadius()
        {
            var big = Country("BIG", Square(0, 0, 10));
            var tiny = Country("TNY", Square(5, 5, 0.01f), small: true);
            var tester = new MapHitTester(new[] { big, tiny });

            Assert.AreEqual("TNY", tester.Find(5.3f, 5.3f, 0.5f), "close tap picks the small country");
            Assert.AreEqual("BIG", tester.Find(5.3f, 5.3f, 0.1f), "zoomed in, the finger radius shrinks");
            Assert.AreEqual("BIG", tester.Find(1, 1, 0.5f));
        }

        [Test]
        public void NonPlayableLandIsIgnored()
        {
            var tester = new MapHitTester(new[] { Country("XXX", Square(0, 0, 10), playable: false) });

            Assert.IsNull(tester.Find(5, 5, 0.1f));
        }

        [Test]
        public void ShippedMapMatchesCountryData()
        {
            var map = WorldMapFile.FromJson(AssetDatabase.LoadAssetAtPath<TextAsset>(WorldMapPath).text);
            var countries = CountryDatabase.FromJson(AssetDatabase.LoadAssetAtPath<TextAsset>(CountriesJsonPath).text);

            var playable = map.countries.Where(c => c.playable).Select(c => c.id).OrderBy(id => id).ToList();
            var records = countries.All.Select(c => c.id).OrderBy(id => id).ToList();

            CollectionAssert.AreEqual(records, playable);
            Assert.Greater(playable.Count, 190);
            foreach (var country in map.countries)
                foreach (var polygon in country.polygons)
                    Assert.AreEqual(0, polygon.triangles.Length % 3, country.id);
        }

        [Test]
        public void EveryPlayableCountryCanBeTappedAtItsLabel()
        {
            var map = WorldMapFile.FromJson(AssetDatabase.LoadAssetAtPath<TextAsset>(WorldMapPath).text);
            var tester = new MapHitTester(map.countries);

            var missed = map.countries
                .Where(c => c.playable && tester.Find(c.label[0], c.label[1], 0.01f) != c.id)
                .Select(c => c.id)
                .ToList();

            CollectionAssert.IsEmpty(missed);
        }
    }
}
