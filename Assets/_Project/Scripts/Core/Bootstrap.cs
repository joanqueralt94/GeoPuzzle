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

        /// <summary>Scene to open after loading instead of the main menu (set by <see cref="RequireBoot"/>).</summary>
        public static string ReturnToScene;

        [SerializeField] TextAsset countriesJson;
        [SerializeField] TextAsset economyConfigJson;
        [Tooltip("English text files: countries.en.json and ui.en.json")]
        [SerializeField] TextAsset[] englishTexts = new TextAsset[0];

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
            var texts = new Localizer("en", englishTexts.Where(t => t != null).Select(t => t.text));

            var wallet = new Wallet(new PlayerPrefsStore(), config.GetInt("wallet.start_coins"));
            var rewardedAds = TestRewardedAds.Create(config.GetBool("ads.rewarded.enabled"));

            GameServices.Initialize(countries, config, texts, wallet, rewardedAds);
            Debug.Log($"Bootstrap: loaded {countries.All.Count} countries and {config.Keys.Count()} config values");
            var next = string.IsNullOrEmpty(ReturnToScene) ? MainMenuScene : ReturnToScene;
            ReturnToScene = null;
            SceneManager.LoadScene(next);
        }
    }
}
