using System;
using System.Collections.Generic;

namespace GeoPuzzle.Quiz
{
    /// <summary>Play-on numbers from the economy config (playon.*), shared by every mode.</summary>
    public class PlayOnSettings
    {
        /// <summary>Hearts a level starts with; a coin play-on after losing all hearts restores this many.</summary>
        public int FullHearts = 3;
        /// <summary>Coin price of the 1st, 2nd, 3rd... hearts play-on in one level; the last price repeats.</summary>
        public IReadOnlyList<int> HeartsCoinPrices = new[] { 100, 200, 300 };
        public int RewardedHearts = 1;
        /// <summary>Coin price of the 1st, 2nd, 3rd... time play-on in one level; the last price repeats.</summary>
        public IReadOnlyList<int> TimeCoinPrices = new[] { 80, 160, 240 };
        public float CoinSeconds = 30f;
        public float RewardedSeconds = 10f;
    }

    /// <summary>What the player can buy to continue a lost level.</summary>
    public struct PlayOnOffer
    {
        public LoseReason Reason;
        public int CoinPrice;
        public int CoinHearts;
        public float CoinSeconds;
        public int RewardedHearts;
        public float RewardedSeconds;
    }

    /// <summary>
    /// Play-on rules for one level: prices rise with each coin play-on of the same kind, a rewarded video
    /// gives a smaller boost and does not raise the price. Paying (coins or the ad) is the caller's job;
    /// call <see cref="ApplyCoins"/> or <see cref="ApplyRewarded"/> once it succeeded.
    /// </summary>
    public class PlayOnSession
    {
        readonly PlayOnSettings settings;
        int heartsCoinUses;
        int timeCoinUses;

        public PlayOnSession(PlayOnSettings settings)
        {
            if (settings.HeartsCoinPrices.Count == 0 || settings.TimeCoinPrices.Count == 0)
                throw new ArgumentException("Play-on needs at least one coin price per kind");
            this.settings = settings;
        }

        public int CoinPlayOnsUsed => heartsCoinUses + timeCoinUses;

        public PlayOnOffer OfferFor(IPlayOnLevel level)
        {
            if (level.State != RoundState.Lost)
                throw new InvalidOperationException("Play-on is only offered for a lost level");

            if (level.LoseReason == LoseReason.OutOfHearts)
                return new PlayOnOffer
                {
                    Reason = LoseReason.OutOfHearts,
                    CoinPrice = Price(settings.HeartsCoinPrices, heartsCoinUses),
                    CoinHearts = Math.Max(0, settings.FullHearts - level.HeartsLeft),
                    RewardedHearts = settings.RewardedHearts,
                };

            return new PlayOnOffer
            {
                Reason = LoseReason.OutOfTime,
                CoinPrice = Price(settings.TimeCoinPrices, timeCoinUses),
                CoinSeconds = settings.CoinSeconds,
                RewardedSeconds = settings.RewardedSeconds,
            };
        }

        /// <summary>Continues the level after the coin price was paid.</summary>
        public void ApplyCoins(IPlayOnLevel level)
        {
            var offer = OfferFor(level);
            if (offer.Reason == LoseReason.OutOfHearts)
                heartsCoinUses++;
            else
                timeCoinUses++;
            level.PlayOn(offer.CoinHearts, offer.CoinSeconds);
        }

        /// <summary>Continues the level after a rewarded video was watched to the end.</summary>
        public void ApplyRewarded(IPlayOnLevel level)
        {
            var offer = OfferFor(level);
            level.PlayOn(offer.RewardedHearts, offer.RewardedSeconds);
        }

        static int Price(IReadOnlyList<int> prices, int uses) => prices[Math.Min(uses, prices.Count - 1)];
    }
}
