using GeoPuzzle.Data;

namespace GeoPuzzle.Core
{
    /// <summary>Shared game data and player state, filled once by <see cref="Bootstrap"/>.</summary>
    public static class GameServices
    {
        public static CountryDatabase Countries { get; private set; }

        public static GameConfig Config { get; private set; }

        public static Localizer Texts { get; private set; }

        public static Wallet Wallet { get; private set; }

        public static IRewardedAds RewardedAds { get; private set; }

        public static bool IsReady =>
            Countries != null && Config != null && Texts != null && Wallet != null && RewardedAds != null;

        public static void Initialize(CountryDatabase countries, GameConfig config, Localizer texts,
            Wallet wallet, IRewardedAds rewardedAds)
        {
            Countries = countries;
            Config = config;
            Texts = texts;
            Wallet = wallet;
            RewardedAds = rewardedAds;
        }
    }
}
