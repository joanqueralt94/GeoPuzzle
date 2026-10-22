namespace GeoPuzzle.Core
{
    /// <summary>Small key -> value storage for player progress, so game rules can be tested without PlayerPrefs.</summary>
    public interface ISaveStore
    {
        bool HasKey(string key);
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        string GetString(string key, string fallback);
        void SetString(string key, string value);
        void Save();
    }
}
