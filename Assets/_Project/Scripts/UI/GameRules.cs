using System.Collections.Generic;
using GeoPuzzle.Core;
using GeoPuzzle.Quiz;

namespace GeoPuzzle.UI
{
    /// <summary>Turns economy config keys into the settings objects the quiz rules take.</summary>
    public static class GameRules
    {
        public static PlayOnSettings PlayOn(GameConfig config) => new PlayOnSettings
        {
            FullHearts = config.GetInt("level.hearts"),
            HeartsCoinPrices = Prices(config, "playon.hearts.coins_"),
            RewardedHearts = config.GetInt("playon.hearts.rv_hearts"),
            TimeCoinPrices = Prices(config, "playon.time.coins_"),
            CoinSeconds = config.GetFloat("playon.time.coins_seconds"),
            RewardedSeconds = config.GetFloat("playon.time.rv_seconds"),
        };

        /// <summary>Coins for clearing a level: a base amount plus a bonus per star.</summary>
        public static int LevelClearCoins(GameConfig config, int stars) =>
            config.GetInt("reward.level_clear.coins") + stars * config.GetInt("reward.level_clear.coins_per_star");

        /// <summary>Reads prefix1, prefix2, ... until a key is missing, so the sheet decides how many steps there are.</summary>
        static List<int> Prices(GameConfig config, string prefix)
        {
            var prices = new List<int>();
            for (var i = 1; config.Has(prefix + i); i++)
                prices.Add(config.GetInt(prefix + i));
            return prices;
        }
    }
}
