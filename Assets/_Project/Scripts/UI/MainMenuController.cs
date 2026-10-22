using GeoPuzzle.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeoPuzzle.UI
{
    /// <summary>Placeholder main menu: the game title and one button per mode.</summary>
    public class MainMenuController : MonoBehaviour
    {
        public const string FlagQuizScene = "FlagQuiz";
        public const string WorldMapScene = "Game";

        Text livesLabel;
        Button flagQuizButton;

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
            flagQuizButton = flagQuiz;

            livesLabel = UiKit.Label(canvas, "Lives", 44, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(livesLabel.rectTransform, 0.1f, 0.52f, 0.9f, 0.60f);

            var worldMap = UiKit.Button(canvas, "WorldMapButton", texts.Get("ui.menu.world_map"), 64,
                () => SceneManager.LoadScene(WorldMapScene));
            UiKit.Place((RectTransform)worldMap.transform, 0.32f, 0.16f, 0.68f, 0.30f);
            UpdateLives();
        }

        void Update()
        {
            if (livesLabel != null)
                UpdateLives();
        }

        /// <summary>Lives and the regen countdown; levels can't start without a life.</summary>
        void UpdateLives()
        {
            var texts = GameServices.Texts;
            var lives = GameServices.Lives;
            var text = texts.Format("ui.lives.count", lives.Count, lives.Max);
            if (lives.Count < lives.Max)
                text += "   " + texts.Format("ui.lives.next", UiKit.Clock(lives.UntilNextLife));
            livesLabel.text = text;
            flagQuizButton.interactable = lives.HasLife;
        }
    }
}
