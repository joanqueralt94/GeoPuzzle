using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GeoPuzzle.UI
{
    /// <summary>
    /// Builds plain uGUI screens from code. Placeholder look until the art pass replaces them with prefabs;
    /// layouts use anchors (fractions of the screen) so they scale to any landscape phone.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Background = new Color(0.11f, 0.20f, 0.30f);
        public static readonly Color ButtonColor = new Color(0.95f, 0.95f, 0.92f);
        public static readonly Color TextDark = new Color(0.12f, 0.12f, 0.14f);
        public static readonly Color TextLight = Color.white;
        public static readonly Color Correct = new Color(0.35f, 0.75f, 0.38f);
        public static readonly Color Wrong = new Color(0.90f, 0.33f, 0.30f);

        static Font font;

        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var image = New<Image>(parent, name);
            image.color = color;
            Place(image.rectTransform, 0, 0, 1, 1);
            return image;
        }

        public static Text Label(Transform parent, string name, int size, TextAnchor alignment, Color color)
        {
            var text = New<Text>(parent, name);
            text.font = Font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string name, string label, int size, Action onClick)
        {
            var image = New<Image>(parent, name);
            image.color = ButtonColor;
            var button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());
            var text = Label(image.transform, "Label", size, TextAnchor.MiddleCenter, TextDark);
            Place(text.rectTransform, 0.04f, 0, 0.96f, 1);
            text.text = label;
            return button;
        }

        public static Text ButtonLabel(Button button) => button.GetComponentInChildren<Text>();

        public static T New<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }
    }
}
