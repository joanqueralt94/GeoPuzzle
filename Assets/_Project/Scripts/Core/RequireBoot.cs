using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeoPuzzle.Core
{
    /// <summary>
    /// Put on any scene that needs game data. Pressing Play on that scene in the editor loads the Boot
    /// scene first, so every scene works without remembering to start from Boot.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class RequireBoot : MonoBehaviour
    {
        public const string BootScene = "Boot";

        void Awake()
        {
            if (!GameServices.IsReady)
            {
                Bootstrap.ReturnToScene = SceneManager.GetActiveScene().name;
                SceneManager.LoadScene(BootScene);
            }
        }
    }
}
