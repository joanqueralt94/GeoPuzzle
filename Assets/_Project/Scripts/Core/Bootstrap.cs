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

        void Start()
        {
            if (countriesJson == null)
            {
                Debug.LogError("Bootstrap: countries.json is not assigned");
                return;
            }

            var countries = CountryDatabase.FromJson(countriesJson.text);
            foreach (var error in countries.Validate())
                Debug.LogWarning($"Country data: {error}");

            GameServices.Initialize(countries);
            Debug.Log($"Bootstrap: loaded {countries.All.Count} countries");
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
