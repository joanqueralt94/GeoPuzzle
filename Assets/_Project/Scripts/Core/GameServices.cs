using GeoPuzzle.Data;

namespace GeoPuzzle.Core
{
    /// <summary>Shared game data, filled once by <see cref="Bootstrap"/>.</summary>
    public static class GameServices
    {
        public static CountryDatabase Countries { get; private set; }

        public static bool IsReady => Countries != null;

        public static void Initialize(CountryDatabase countries) => Countries = countries;
    }
}
