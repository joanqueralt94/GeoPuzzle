using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace GeoPuzzle.Map
{
    /// <summary>
    /// Turns a short tap (not a drag or pinch) into a country selection and highlights it.
    /// Game code listens to <see cref="CountryTapped"/>; the id is null when the tap hit the sea.
    /// </summary>
    [RequireComponent(typeof(WorldMapView))]
    public class MapTapInput : MonoBehaviour
    {
        [SerializeField] Color selectedColor = new Color(1f, 0.78f, 0.25f);
        [SerializeField] float maxTapMovePixels = 20f;
        [SerializeField] float maxTapSeconds = 0.4f;
        [Tooltip("How far from a small country's dot a tap still selects it, in inches on screen")]
        [SerializeField] float fingerRadiusInches = 0.15f;
        [SerializeField] bool showDebugLabel = true;

        public event Action<string> CountryTapped;

        public string SelectedId { get; private set; }

        WorldMapView map;
        Camera cam;
        Vector2 downPosition;
        float downTime;
        bool tracking;
        GUIStyle debugStyle;

        void Awake() => map = GetComponent<WorldMapView>();

        void Start() => cam = Camera.main;

        void OnEnable() => EnhancedTouchSupport.Enable();

        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            var touches = Touch.activeTouches;
            if (touches.Count == 1)
            {
                var touch = touches[0];
                if (touch.phase == TouchPhase.Began)
                    Begin(touch.screenPosition);
                else if (touch.phase == TouchPhase.Ended)
                    End(touch.screenPosition);
                return;
            }
            if (touches.Count > 1)
            {
                tracking = false;
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
                return;
            if (mouse.leftButton.wasPressedThisFrame)
                Begin(mouse.position.ReadValue());
            else if (mouse.leftButton.wasReleasedThisFrame)
                End(mouse.position.ReadValue());
        }

        void Begin(Vector2 screenPosition)
        {
            tracking = true;
            downPosition = screenPosition;
            downTime = Time.unscaledTime;
        }

        void End(Vector2 screenPosition)
        {
            if (!tracking)
                return;
            tracking = false;
            if (Time.unscaledTime - downTime > maxTapSeconds)
                return;
            if (Vector2.Distance(screenPosition, downPosition) > maxTapMovePixels)
                return;
            HandleTap(screenPosition);
        }

        void HandleTap(Vector2 screenPosition)
        {
            if (cam == null)
                return;
            Vector2 world = cam.ScreenToWorldPoint(screenPosition);
            var dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            var worldPerPixel = 2f * cam.orthographicSize / Mathf.Max(1, Screen.height);
            var fingerRadius = fingerRadiusInches * dpi * worldPerPixel;

            Select(map.HitTester.Find(world.x, world.y, fingerRadius));
            CountryTapped?.Invoke(SelectedId);
        }

        public void Select(string id)
        {
            if (SelectedId != null)
                map.ResetCountryColor(SelectedId);
            SelectedId = id;
            if (id != null)
                map.SetCountryColor(id, selectedColor);
        }

        void OnGUI()
        {
            if (!showDebugLabel || !Debug.isDebugBuild)
                return;
            debugStyle ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 25) };
            GUI.Label(new Rect(20, 20, Screen.width - 40, debugStyle.fontSize * 2),
                $"Tapped: {SelectedId ?? "sea"}", debugStyle);
        }
    }
}
