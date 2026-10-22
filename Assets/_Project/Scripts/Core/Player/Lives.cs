using System;
using System.Globalization;

namespace GeoPuzzle.Core
{
    /// <summary>
    /// Lives that a failed or quit level costs. Below the maximum, one life comes back every regen period,
    /// counted from real time so it also refills while the game is closed.
    /// </summary>
    public class Lives
    {
        public const string CountKey = "lives.count";
        public const string RegenStartKey = "lives.regen_start";

        readonly ISaveStore store;
        readonly Func<DateTime> utcNow;
        readonly TimeSpan regenTime;
        int count;
        DateTime regenStart;

        public int Max { get; }

        public Lives(ISaveStore store, int max, TimeSpan regenTime, Func<DateTime> utcNow = null)
        {
            this.store = store;
            this.regenTime = regenTime;
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
            Max = max;
            count = store.HasKey(CountKey) ? Math.Min(store.GetInt(CountKey, max), max) : max;
            var saved = store.GetString(RegenStartKey, null);
            regenStart = saved != null && long.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks)
                ? new DateTime(ticks, DateTimeKind.Utc)
                : this.utcNow();
        }

        public int Count
        {
            get
            {
                Regenerate();
                return count;
            }
        }

        public bool HasLife => Count > 0;

        /// <summary>Time until the next life comes back; zero when lives are full.</summary>
        public TimeSpan UntilNextLife
        {
            get
            {
                Regenerate();
                if (count >= Max)
                    return TimeSpan.Zero;
                var left = regenStart + regenTime - utcNow();
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        public void Lose(int amount)
        {
            Regenerate();
            if (count >= Max)
                regenStart = utcNow();
            count = Math.Max(0, count - amount);
            Persist();
        }

        public void Add(int amount)
        {
            Regenerate();
            count = Math.Min(Max, count + amount);
            Persist();
        }

        void Regenerate()
        {
            if (count >= Max || regenTime <= TimeSpan.Zero)
                return;
            var now = utcNow();
            if (now < regenStart)
            {
                // The device clock went back; restart the wait rather than locking the player out.
                regenStart = now;
                Persist();
                return;
            }
            var regained = (int)((now - regenStart).Ticks / regenTime.Ticks);
            if (regained <= 0)
                return;
            count = Math.Min(Max, count + regained);
            regenStart += TimeSpan.FromTicks(regenTime.Ticks * regained);
            Persist();
        }

        void Persist()
        {
            store.SetInt(CountKey, count);
            store.SetString(RegenStartKey, regenStart.Ticks.ToString(CultureInfo.InvariantCulture));
            store.Save();
        }
    }
}
