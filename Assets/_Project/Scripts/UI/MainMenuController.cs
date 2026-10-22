using GeoPuzzle.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeoPuzzle.UI
{
    /// <summary>Placeholder main menu: the game title and one button per mode.</summary>
    public class MainMenuController : MonoBehaviour
    {
        public const string FlagQuizScene = "FlagQuiz";
        public const string WorldMapScene = "Game";

        void Start()
        {
            if (!GameServices.IsReady)
                return;

            var texts = GameServices.Texts;
            var canvas = UiKit.CreateCanvas("MainMenuCanvas").transform;
            UiKit.Panel(canvas, "Background", UiKit.Background);

            var title = UiKit.Label(canvas, "Title", 140, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(title.rectTransform, 0.1f, 0.62f, 0.9f, 0.88f);
            title.text = texts.Get("ui.title");

            var flagQuiz = UiKit.Button(canvas, "FlagQuizButton", texts.Get("ui.menu.flag_quiz"), 64,
                () => SceneManager.LoadScene(FlagQuizScene));
            UiKit.Place((RectTransform)flagQuiz.transform, 0.32f, 0.36f, 0.68f, 0.50f);

            var worldMap = UiKit.Button(canvas, "WorldMapButton", texts.Get("ui.menu.world_map"), 64,
                () => SceneManager.LoadScene(WorldMapScene));
            UiKit.Place((RectTransform)worldMap.transform, 0.32f, 0.16f, 0.68f, 0.30f);
        }
    }
}
