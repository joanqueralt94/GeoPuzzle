using System.Collections.Generic;
using System.Linq;
using GeoPuzzle.Core;
using GeoPuzzle.Quiz;
using NUnit.Framework;

namespace GeoPuzzle.Tests
{
    public class PlayOnTests
    {
        class FakeLevel : IPlayOnLevel
        {
            public RoundState State { get; set; } = RoundState.Lost;
            public LoseReason LoseReason { get; set; }
            public int HeartsLeft { get; set; }
            public float TimeLeft { get; set; }

            public void PlayOn(int extraHearts, float extraSeconds)
            {
                HeartsLeft += extraHearts;
                TimeLeft += extraSeconds;
                State = RoundState.Playing;
                LoseReason = LoseReason.None;
            }

            public void Lose(LoseReason reason)
            {
                State = RoundState.Lost;
                LoseReason = reason;
                if (reason == LoseReason.OutOfHearts)
                    HeartsLeft = 0;
                else
                    TimeLeft = 0f;
            }
        }

        class MemoryStore : ISaveStore
        {
            public readonly Dictionary<string, int> Values = new Dictionary<string, int>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public int GetInt(string key, int fallback) => Values.TryGetValue(key, out var v) ? v : fallback;
            public void SetInt(string key, int value) => Values[key] = value;
            public void Save() { }
        }

        static PlayOnSession MakeSession() => new PlayOnSession(new PlayOnSettings
        {
            FullHearts = 3,
            HeartsCoinPrices = new[] { 100, 200, 300 },
            RewardedHearts = 1,
            TimeCoinPrices = new[] { 80, 160, 240 },
            CoinSeconds = 30f,
            RewardedSeconds = 10f,
        });

        [Test]
        public void HeartsPlayOnRestoresAllHeartsAndPriceRises()
        {
            var session = MakeSession();
            var level = new FakeLevel();
            var prices = new List<int>();
            for (var i = 0; i < 4; i++)
            {
                level.Lose(LoseReason.OutOfHearts);
                var offer = session.OfferFor(level);
                Assert.AreEqual(3, offer.CoinHearts);
                Assert.AreEqual(0f, offer.CoinSeconds);
                prices.Add(offer.CoinPrice);
                session.ApplyCoins(level);
                Assert.AreEqual(3, level.HeartsLeft);
                Assert.AreEqual(RoundState.Playing, level.State);
            }
            CollectionAssert.AreEqual(new[] { 100, 200, 300, 300 }, prices);
        }

        [Test]
        public void TimePlayOnAddsSecondsWithItsOwnPrices()
        {
            var session = MakeSession();
            var level = new FakeLevel { HeartsLeft = 2 };
            level.Lose(LoseReason.OutOfHearts);
            session.ApplyCoins(level);

            level.Lose(LoseReason.OutOfTime);
            var offer = session.OfferFor(level);
            Assert.AreEqual(80, offer.CoinPrice, "hearts play-ons don't raise the time price");
            Assert.AreEqual(0, offer.CoinHearts);
            session.ApplyCoins(level);
            Assert.AreEqual(30f, level.TimeLeft);

            level.Lose(LoseReason.OutOfTime);
            Assert.AreEqual(160, session.OfferFor(level).CoinPrice);
        }

        [Test]
        public void RewardedPlayOnGivesTheSmallBoostAndKeepsThePrice()
        {
            var session = MakeSession();
            var level = new FakeLevel();
            level.Lose(LoseReason.OutOfHearts);
            session.ApplyRewarded(level);
            Assert.AreEqual(1, level.HeartsLeft);

            level.Lose(LoseReason.OutOfHearts);
            Assert.AreEqual(100, session.OfferFor(level).CoinPrice);

            level.Lose(LoseReason.OutOfTime);
            session.ApplyRewarded(level);
            Assert.AreEqual(10f, level.TimeLeft);
        }

        [Test]
        public void NoOfferWhileTheLevelIsStillGoing()
        {
            var level = new FakeLevel { State = RoundState.Playing };
            Assert.Throws<System.InvalidOperationException>(() => MakeSession().OfferFor(level));
        }

        [Test]
        public void WorksWithTheFlagQuizRound()
        {
            var database = new GeoPuzzle.Data.CountryDatabase(new[] { "ESP", "PRT", "ITA", "GRC", "FRA", "DEU" }
                .Select(id => new GeoPuzzle.Data.CountryRecord { id = id, nameKey = id, flag = id, continent = "W", subregion = "W" }));
            var round = new FlagQuizRound(new QuestionGenerator(database, 3), new FlagQuizSettings { Questions = 5, Hearts = 3 });
            for (var i = 0; i < 3; i++)
                round.Answer((round.Current.CorrectIndex + 1) % round.Current.Options.Count);
            Assert.AreEqual(LoseReason.OutOfHearts, round.LoseReason);

            MakeSession().ApplyCoins(round);
            Assert.AreEqual(RoundState.Playing, round.State);
            Assert.AreEqual(3, round.HeartsLeft);
        }

        [Test]
        public void WalletStartsWithStartCoinsOnceAndSaves()
        {
            var store = new MemoryStore();
            var wallet = new Wallet(store, startCoins: 200);
            Assert.AreEqual(200, wallet.Coins);

            Assert.IsTrue(wallet.TrySpend(150));
            Assert.IsFalse(wallet.TrySpend(100));
            wallet.Add(30);
            Assert.AreEqual(80, wallet.Coins);

            var reloaded = new Wallet(store, startCoins: 200);
            Assert.AreEqual(80, reloaded.Coins, "start coins are only given to a new player");
        }
    }
}
