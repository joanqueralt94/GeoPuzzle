using System.Collections.Generic;
using GeoPuzzle.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GeoPuzzle.Tests
{
    public class GameConfigTests
    {
        const string EconomyConfigPath = "Assets/_Project/Data/Config/economy_config.json";

        const string SampleJson = @"{
            ""level.hearts"": 3,
            ""level.easy_start.timer_multiplier"": 2.0,
            ""ads.banner.enabled"": true,
            ""iap.no_ads.price_eur"": 3.99,
            ""shop.currency"": ""EUR""
        }";

        [Test]
        public void ShippedEconomyConfigLoads()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(EconomyConfigPath);
            Assert.IsNotNull(asset, $"{EconomyConfigPath} is missing");

            var config = GameConfig.FromJson(asset.text);

            CollectionAssert.IsNotEmpty(config.Keys);
            Assert.Greater(config.GetInt("level.hearts"), 0);
        }

        [Test]
        public void ReadsEachValueType()
        {
            var config = GameConfig.FromJson(SampleJson);

            Assert.AreEqual(3, config.GetInt("level.hearts"));
            Assert.AreEqual(2, config.GetInt("level.easy_start.timer_multiplier"));
            Assert.AreEqual(3.99f, config.GetFloat("iap.no_ads.price_eur"), 0.0001f);
            Assert.IsTrue(config.GetBool("ads.banner.enabled"));
            Assert.AreEqual("EUR", config.GetString("shop.currency"));
        }

        [Test]
        public void OverridesWinOverDefaultsAndCanBeCleared()
        {
            var config = GameConfig.FromJson(SampleJson);

            config.ApplyOverrides(new Dictionary<string, object> { ["level.hearts"] = 5L });
            Assert.AreEqual(5, config.GetInt("level.hearts"));

            config.ClearOverrides();
            Assert.AreEqual(3, config.GetInt("level.hearts"));
        }

        [Test]
        public void MissingKeyThrows()
        {
            var config = GameConfig.FromJson(SampleJson);

            Assert.IsFalse(config.Has("lives.max"));
            Assert.Throws<KeyNotFoundException>(() => config.GetInt("lives.max"));
        }
    }
}
