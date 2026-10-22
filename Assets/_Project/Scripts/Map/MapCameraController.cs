using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GeoPuzzle.Map
{
    /// <summary>
    /// Pans and zooms an orthographic camera over the world map.
    /// One finger drags, two fingers pinch around their midpoint. In the editor, mouse drag and wheel do the same.
    /// Pointer movement is measured from positions remembered each frame rather than the input deltas,
    /// which can use different units from positions on high-DPI screens and made the map trail the cursor.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCameraController : MonoBehaviour
    {
        [Tooltip("World-space rectangle the camera view must stay inside")]
        [SerializeField] Rect mapBounds = new Rect(-18f, -9f, 36f, 18f);
        [SerializeField] float minOrthoSize = 0.15f;
        [SerializeField] float maxOrthoSize = 9f;
        [SerializeField] float mouseWheelZoomSpeed = 0.1f;
        [SerializeField] float inertiaDamping = 4f;

        readonly Dictionary<int, Vector2> lastTouchPositions = new Dictionary<int, Vector2>();
        Camera cam;
        Vector2 velocity;
        Vector2? lastMousePosition;

        void Awake() => cam = GetComponent<Camera>();

        void OnEnable() => EnhancedTouchSupport.Enable();

        void OnDisable() => EnhancedTouchSupport.Disable();

        /// <summary>Limits panning to the given map area and zooms out to show all of it.</summary>
        public void FitTo(Rect bounds)
        {
            mapBounds = bounds;
            maxOrthoSize = bounds.height * 0.5f;
            cam.orthographicSize = maxOrthoSize;
            transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
            velocity = Vector2.zero;
        }

        void Update()
        {
            var touches = Touch.activeTouches;
            if (touches.Count >= 2)
                Pinch(touches[0], touches[1]);
            else if (touches.Count == 1)
                Drag(PreviousPosition(touches[0]), touches[0].screenPosition);
            else if (!HandleMouse())
                ApplyInertia();

            RememberTouches(touches);
            Clamp();
        }

        Vector2 PreviousPosition(Touch touch) =>
            lastTouchPositions.TryGetValue(touch.touchId, out var previous) ? previous : touch.screenPosition;

        void RememberTouches(UnityEngine.InputSystem.Utilities.ReadOnlyArray<Touch> touches)
        {
            lastTouchPositions.Clear();
            foreach (var touch in touches)
                lastTouchPositions[touch.touchId] = touch.screenPosition;
        }

        bool HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            var scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                ZoomAround(mouse.position.ReadValue(), 1f - Mathf.Sign(scroll) * mouseWheelZoomSpeed);

            if (!mouse.leftButton.isPressed)
            {
                lastMousePosition = null;
                return false;
            }

            var position = mouse.position.ReadValue();
            Drag(lastMousePosition ?? position, position);
            lastMousePosition = position;
            return true;
        }

        void Drag(Vector2 fromScreen, Vector2 toScreen)
        {
            Vector2 worldDelta = ScreenToWorld(fromScreen) - ScreenToWorld(toScreen);
            transform.position += (Vector3)worldDelta;
            // Smoothed so that a still frame just before release does not cancel the fling.
            if (Time.deltaTime > 0f)
                velocity = Vector2.Lerp(velocity, worldDelta / Time.deltaTime, 0.5f);
        }

        void Pinch(Touch a, Touch b)
        {
            var previousA = PreviousPosition(a);
            var previousB = PreviousPosition(b);
            var previousDistance = Vector2.Distance(previousA, previousB);
            var distance = Vector2.Distance(a.screenPosition, b.screenPosition);
            var midpoint = (a.screenPosition + b.screenPosition) * 0.5f;

            Drag((previousA + previousB) * 0.5f, midpoint);
            if (distance > 1f && previousDistance > 1f)
                ZoomAround(midpoint, previousDistance / distance);
            velocity = Vector2.zero;
        }

        void ZoomAround(Vector2 screenPoint, float factor)
        {
            var before = ScreenToWorld(screenPoint);
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, minOrthoSize, maxOrthoSize);
            var after = ScreenToWorld(screenPoint);
            transform.position += (Vector3)(before - after);
        }

        void ApplyInertia()
        {
            if (velocity.sqrMagnitude < 0.0001f)
                return;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity = Vector2.Lerp(velocity, Vector2.zero, inertiaDamping * Time.deltaTime);
        }

        void Clamp()
        {
            var halfHeight = cam.orthographicSize;
            var halfWidth = halfHeight * cam.aspect;
            var p = transform.position;
            p.x = ClampAxis(p.x, mapBounds.xMin + halfWidth, mapBounds.xMax - halfWidth);
            p.y = ClampAxis(p.y, mapBounds.yMin + halfHeight, mapBounds.yMax - halfHeight);
            transform.position = p;
        }

        static float ClampAxis(float value, float min, float max) =>
            min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);

        Vector2 ScreenToWorld(Vector2 screen) => cam.ScreenToWorldPoint(screen);
    }
}
