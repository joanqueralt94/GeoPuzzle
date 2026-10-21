using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GeoPuzzle.Map
{
    /// <summary>
    /// Pans and zooms an orthographic camera over the world map.
    /// One finger drags, two fingers pinch around their midpoint. In the editor, mouse drag and wheel do the same.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MapCameraController : MonoBehaviour
    {
        [Tooltip("World-space rectangle the camera view must stay inside")]
        [SerializeField] Rect mapBounds = new Rect(-18f, -9f, 36f, 18f);
        [SerializeField] float minOrthoSize = 0.15f;
        [SerializeField] float maxOrthoSize = 9f;
        [SerializeField] float mouseWheelZoomSpeed = 0.1f;
        [SerializeField] float inertiaDamping = 6f;

        Camera cam;
        Vector2 velocity;

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
                Drag(touches[0].screenPosition - touches[0].delta, touches[0].screenPosition);
            else if (!HandleMouse())
                ApplyInertia();

            Clamp();
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
                return false;

            var position = mouse.position.ReadValue();
            Drag(position - mouse.delta.ReadValue(), position);
            return true;
        }

        void Drag(Vector2 fromScreen, Vector2 toScreen)
        {
            Vector2 worldDelta = ScreenToWorld(fromScreen) - ScreenToWorld(toScreen);
            transform.position += (Vector3)worldDelta;
            if (Time.deltaTime > 0f)
                velocity = worldDelta / Time.deltaTime;
        }

        void Pinch(Touch a, Touch b)
        {
            var previousA = a.screenPosition - a.delta;
            var previousB = b.screenPosition - b.delta;
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
