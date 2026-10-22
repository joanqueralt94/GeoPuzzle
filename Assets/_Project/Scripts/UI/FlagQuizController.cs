using System;
using System.Collections;
using System.Linq;
using GeoPuzzle.Core;
using GeoPuzzle.Data;
using GeoPuzzle.Quiz;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GeoPuzzle.UI
{
    /// <summary>
    /// Plays one Flag Quizz level: shows the flag, four country names, hearts, the timer and progress,
    /// then a results panel. Level numbers come from the economy config. The look is a placeholder
    /// until the art pass.
    /// </summary>
    public class FlagQuizController : MonoBehaviour
    {
        const string QuestionTag = "sovereign";

        [SerializeField] FlagLibrary flags;
        [Tooltip("How long the right/wrong colours stay before the next flag")]
        [SerializeField] float feedbackSeconds = 0.8f;

        Localizer texts;
        FlagQuizRound round;
        PlayOnSession playOn;
        PlayOnPanel playOnPanel;
        bool showingFeedback;

        Text progressLabel;
        Text timerLabel;
        Text heartsLabel;
        Image flagImage;
        Button[] optionButtons;
        GameObject resultsPanel;
        Text resultsTitle;
        Text resultsStars;
        Text resultsScore;
        Text resultsCoins;

        void Start()
        {
            if (!GameServices.IsReady)
                return;
            texts = GameServices.Texts;
            BuildScreen();
            StartRound();
        }

        void Update()
        {
            if (round == null || showingFeedback)
                return;
            round.Tick(Time.deltaTime);
            UpdateStatus();
        }

        static FlagQuizSettings ReadSettings(GameConfig config) => new FlagQuizSettings
        {
            Questions = config.GetInt("level.flag.questions"),
            Hearts = config.GetInt("level.hearts"),
            TimerSeconds = config.GetFloat("level.flag.timer_seconds"),
            ThreeStarMaxHeartsLost = config.GetInt("stars.three_max_hearts_lost"),
            TwoStarMaxHeartsLost = config.GetInt("stars.two_max_hearts_lost"),
        };

        void StartRound()
        {
            var pool = new CountryDatabase(GameServices.Countries.All.Where(c => c.tags.Contains(QuestionTag)));
            var generator = new QuestionGenerator(pool, Environment.TickCount);
            round = new FlagQuizRound(generator, ReadSettings(GameServices.Config));
            playOn = new PlayOnSession(GameRules.PlayOn(GameServices.Config));
            resultsPanel.SetActive(false);
            ShowQuestion();
        }

        void ShowQuestion()
        {
            var question = round.Current;
            var country = GameServices.Countries.Get(question.TargetId);
            flagImage.sprite = flags != null ? flags.Get(country.flag) : null;
            flagImage.enabled = flagImage.sprite != null;
            if (!flagImage.enabled)
                Debug.LogWarning($"No flag sprite for {country.id} ({country.flag}); run GeoPuzzle > Setup > Build Flag Library");

            for (var i = 0; i < optionButtons.Length; i++)
            {
                var button = optionButtons[i];
                var visible = i < question.Options.Count;
                button.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                UiKit.ButtonLabel(button).text = texts.Get(GameServices.Countries.Get(question.Options[i]).nameKey);
                button.image.color = UiKit.ButtonColor;
                button.interactable = true;
            }
            UpdateStatus();
        }

        void UpdateStatus()
        {
            progressLabel.text = texts.Format("ui.quiz.question", round.QuestionNumber, round.QuestionCount);
            heartsLabel.text = texts.Format("ui.quiz.hearts", round.HeartsLeft);
            var seconds = Mathf.CeilToInt(round.TimeLeft);
            timerLabel.text = $"{seconds / 60}:{seconds % 60:00}";
            if (round.State == RoundState.Lost && !showingFeedback && !playOnPanel.IsOpen && !resultsPanel.activeSelf)
                OfferPlayOn();
        }

        void OfferPlayOn() => playOnPanel.Show(playOn, round, OnPlayedOn, ShowResults);

        void OnPlayedOn()
        {
            // A heart play-on on the last question can finish the level straight away.
            if (round.State == RoundState.Won)
                ShowResults();
            else
                ShowQuestion();
        }

        void OnOptionClicked(int index)
        {
            if (showingFeedback || round.State != RoundState.Playing)
                return;
            var result = round.Answer(index);
            StartCoroutine(ShowFeedback(index, result));
        }

        IEnumerator ShowFeedback(int chosen, AnswerResult result)
        {
            showingFeedback = true;
            foreach (var button in optionButtons)
                button.interactable = false;
            optionButtons[result.CorrectIndex].image.color = UiKit.Correct;
            if (!result.Correct)
                optionButtons[chosen].image.color = UiKit.Wrong;
            heartsLabel.text = texts.Format("ui.quiz.hearts", round.HeartsLeft);

            yield return new WaitForSeconds(feedbackSeconds);
            showingFeedback = false;

            if (round.State == RoundState.Playing)
                ShowQuestion();
            else if (round.State == RoundState.Lost)
                OfferPlayOn();
            else
                ShowResults();
        }

        void ShowResults()
        {
            resultsTitle.text = texts.Get(round.State == RoundState.Won ? "ui.quiz.won"
                : round.LoseReason == LoseReason.OutOfTime ? "ui.quiz.lost_time" : "ui.quiz.lost_hearts");
            resultsStars.text = texts.Format("ui.quiz.stars", round.Stars);
            resultsScore.text = texts.Format("ui.quiz.score", round.CorrectAnswers, round.QuestionCount);
            resultsCoins.text = "";
            if (round.State == RoundState.Won)
            {
                var coins = GameRules.LevelClearCoins(GameServices.Config, round.Stars);
                GameServices.Wallet.Add(coins);
                resultsCoins.text = texts.Format("ui.quiz.coins_earned", coins);
            }
            resultsPanel.SetActive(true);
        }

        void BuildScreen()
        {
            var canvas = UiKit.CreateCanvas("FlagQuizCanvas").transform;
            UiKit.Panel(canvas, "Background", UiKit.Background);

            progressLabel = UiKit.Label(canvas, "Progress", 48, TextAnchor.MiddleLeft, UiKit.TextLight);
            UiKit.Place(progressLabel.rectTransform, 0.14f, 0.88f, 0.38f, 0.98f);
            timerLabel = UiKit.Label(canvas, "Timer", 56, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(timerLabel.rectTransform, 0.40f, 0.88f, 0.60f, 0.98f);
            heartsLabel = UiKit.Label(canvas, "Hearts", 48, TextAnchor.MiddleRight, UiKit.TextLight);
            UiKit.Place(heartsLabel.rectTransform, 0.66f, 0.88f, 0.96f, 0.98f);

            var prompt = UiKit.Label(canvas, "Prompt", 44, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(prompt.rectTransform, 0.1f, 0.79f, 0.9f, 0.87f);
            prompt.text = texts.Get("ui.quiz.which_country");

            flagImage = UiKit.New<Image>(canvas, "Flag");
            flagImage.preserveAspect = true;
            flagImage.raycastTarget = false;
            UiKit.Place(flagImage.rectTransform, 0.3f, 0.42f, 0.7f, 0.77f);

            optionButtons = new Button[4];
            for (var i = 0; i < optionButtons.Length; i++)
            {
                var index = i;
                var column = i % 2;
                var row = i / 2;
                var button = UiKit.Button(canvas, $"Option{i + 1}", "", 46, () => OnOptionClicked(index));
                var xMin = column == 0 ? 0.08f : 0.52f;
                var yMax = row == 0 ? 0.38f : 0.21f;
                UiKit.Place((RectTransform)button.transform, xMin, yMax - 0.14f, xMin + 0.40f, yMax);
                optionButtons[i] = button;
            }

            playOnPanel = new PlayOnPanel(canvas, texts);
            BuildResultsPanel(canvas);
        }

        void BuildResultsPanel(Transform canvas)
        {
            var panel = UiKit.Panel(canvas, "Results", new Color(0f, 0f, 0f, 0.8f));
            resultsPanel = panel.gameObject;

            resultsTitle = UiKit.Label(panel.transform, "Title", 96, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(resultsTitle.rectTransform, 0.1f, 0.66f, 0.9f, 0.86f);
            resultsStars = UiKit.Label(panel.transform, "Stars", 64, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(resultsStars.rectTransform, 0.1f, 0.54f, 0.9f, 0.65f);
            resultsScore = UiKit.Label(panel.transform, "Score", 52, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(resultsScore.rectTransform, 0.1f, 0.44f, 0.9f, 0.54f);
            resultsCoins = UiKit.Label(panel.transform, "Coins", 52, TextAnchor.MiddleCenter, UiKit.Coin);
            UiKit.Place(resultsCoins.rectTransform, 0.1f, 0.34f, 0.9f, 0.44f);

            var again = UiKit.Button(panel.transform, "PlayAgain", texts.Get("ui.quiz.play_again"), 56, StartRound);
            UiKit.Place((RectTransform)again.transform, 0.18f, 0.16f, 0.48f, 0.30f);
            var menu = UiKit.Button(panel.transform, "Menu", texts.Get("ui.quiz.menu"), 56,
                () => SceneManager.LoadScene(Bootstrap.MainMenuScene));
            UiKit.Place((RectTransform)menu.transform, 0.52f, 0.16f, 0.82f, 0.30f);

            resultsPanel.SetActive(false);
        }
    }
}
