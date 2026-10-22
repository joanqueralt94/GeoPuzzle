using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeoPuzzle.Core
{
    /// <summary>
    /// Remembers which screens the player came through so Back returns to the previous one.
    /// Boot is never remembered; with no history, Back goes to the main menu.
    /// </summary>
    public static class SceneNavigator
    {
        static readonly List<string> history = new List<string>();
        static bool goingBack;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            history.Clear();
            goingBack = false;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        /// <summary>The scene Back would open.</summary>
        public static string PreviousScene => history.Count > 0 ? history[history.Count - 1] : Bootstrap.MainMenuScene;

        public static void Back()
        {
            var target = PreviousScene;
            if (history.Count > 0)
                history.RemoveAt(history.Count - 1);
            goingBack = true;
            SceneManager.LoadScene(target);
        }

        static void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (goingBack)
            {
                goingBack = false;
                return;
            }
            if (!from.IsValid() || from.name == to.name || from.name == RequireBoot.BootScene || to.name == RequireBoot.BootScene)
                return;
            // Returning to a screen already in the history unwinds to it instead of making a loop.
            var index = history.LastIndexOf(to.name);
            if (index >= 0)
                history.RemoveRange(index, history.Count - index);
            else
                history.Add(from.name);
        }
    }
}
