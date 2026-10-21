using GeoPuzzle.Data;

namespace GeoPuzzle.Core
{
    /// <summary>Shared game data, filled once by <see cref="Bootstrap"/>.</summary>
    public static class GameServices
    {
        public static CountryDatabase Countries { get; private set; }

        public static GameConfig Config { get; private set; }

        public static bool IsReady => Countries != null && Config != null;

        public static void Initialize(CountryDatabase countries, GameConfig config)
        {
            Countries = countries;
            Config = config;
        }
    }
}
