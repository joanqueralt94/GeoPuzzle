using GeoPuzzle.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GeoPuzzle.UI
{
    /// <summary>
    /// A Back button in the top-left corner of every screen except Boot, the main menu and level screens
    /// (which have Home instead), plus the Android back gesture (Escape in the editor). Added automatically,
    /// so new screens get it too.
    /// </summary>
    public class BackButton : MonoBehaviour
    {
        static readonly string[] ScenesWithoutBack =
            { RequireBoot.BootScene, Bootstrap.MainMenuScene, MainMenuController.FlagQuizScene };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || System.Array.IndexOf(ScenesWithoutBack, scene.name) >= 0)
                return;
            // Without game data the scene is about to hand over to Boot (see RequireBoot).
            if (!GameServices.IsReady || FindAnyObjectByType<BackButton>() != null)
                return;

            var canvas = UiKit.CreateCanvas("BackCanvas");
            canvas.sortingOrder = 100;
            canvas.gameObject.AddComponent<BackButton>();
            UiKit.CornerButton(canvas.transform, "Back", GameServices.Texts.Get("ui.back"), SceneNavigator.Back);
        }

        void Update()
        {
            // Android's back gesture arrives as the Escape key.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SceneNavigator.Back();
        }
    }
}
