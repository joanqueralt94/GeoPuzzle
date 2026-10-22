using System;
using GeoPuzzle.Core;
using NUnit.Framework;

namespace GeoPuzzle.Tests
{
    public class LivesTests
    {
        static readonly TimeSpan Regen = TimeSpan.FromMinutes(20);
        DateTime now;

        Lives Make(MemoryStore store) => new Lives(store, 5, Regen, () => now);

        [SetUp]
        public void SetUp() => now = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void NewPlayerHasFullLives()
        {
            var lives = Make(new MemoryStore());
            Assert.AreEqual(5, lives.Count);
            Assert.AreEqual(TimeSpan.Zero, lives.UntilNextLife);
        }

        [Test]
        public void LostLivesComeBackOneEveryRegenPeriod()
        {
            var lives = Make(new MemoryStore());
            lives.Lose(1);
            lives.Lose(1);
            Assert.AreEqual(3, lives.Count);
            Assert.AreEqual(Regen, lives.UntilNextLife);

            now += TimeSpan.FromMinutes(25);
            Assert.AreEqual(4, lives.Count);
            Assert.AreEqual(TimeSpan.FromMinutes(15), lives.UntilNextLife, "the extra 5 minutes count toward the next life");

            now += TimeSpan.FromHours(3);
            Assert.AreEqual(5, lives.Count);
        }

        [Test]
        public void LosingAgainDoesNotResetTheRunningTimer()
        {
            var lives = Make(new MemoryStore());
            lives.Lose(1);
            now += TimeSpan.FromMinutes(10);
            lives.Lose(1);
            Assert.AreEqual(TimeSpan.FromMinutes(10), lives.UntilNextLife);
        }

        [Test]
        public void NoLivesBelowZeroAndStateSurvivesARestart()
        {
            var store = new MemoryStore();
            var lives = Make(store);
            lives.Lose(9);
            Assert.AreEqual(0, lives.Count);
            Assert.IsFalse(lives.HasLife);

            now += TimeSpan.FromMinutes(45);
            var reopened = Make(store);
            Assert.AreEqual(2, reopened.Count, "lives refill while the game is closed");
        }

        [Test]
        public void ClockGoingBackwardsRestartsTheWait()
        {
            var lives = Make(new MemoryStore());
            lives.Lose(1);
            now -= TimeSpan.FromHours(1);
            Assert.AreEqual(4, lives.Count);
            Assert.AreEqual(Regen, lives.UntilNextLife);
        }
    }
}
