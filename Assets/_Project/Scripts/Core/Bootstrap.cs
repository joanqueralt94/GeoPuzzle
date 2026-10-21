using System.Linq;
using GeoPuzzle.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeoPuzzle.Core
{
    /// <summary>Lives in the Boot scene: loads game data once, then opens the main menu.</summary>
    public class Bootstrap : MonoBehaviour
    {
        public const string MainMenuScene = "MainMenu";

        [SerializeField] TextAsset countriesJson;
        [SerializeField] TextAsset economyConfigJson;

        void Start()
        {
            if (countriesJson == null || economyConfigJson == null)
            {
                Debug.LogError("Bootstrap: countries.json or economy_config.json is not assigned");
                return;
            }

            var countries = CountryDatabase.FromJson(countriesJson.text);
            foreach (var error in countries.Validate())
                Debug.LogWarning($"Country data: {error}");

            var config = GameConfig.FromJson(economyConfigJson.text);

            GameServices.Initialize(countries, config);
            Debug.Log($"Bootstrap: loaded {countries.All.Count} countries and {config.Keys.Count()} config values");
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
