using System;
using UnityEngine;
using UnityEngine.UI;

namespace GeoPuzzle.UI
{
    /// <summary>A yes/no question over the current screen, such as "Quit this level?".</summary>
    public class ConfirmDialog
    {
        readonly GameObject root;
        readonly Text title;
        readonly Text message;
        readonly Button confirmButton;
        readonly Button cancelButton;
        Action onConfirm;
        Action onCancel;

        public bool IsOpen => root.activeSelf;

        public ConfirmDialog(Transform canvas)
        {
            var panel = UiKit.Panel(canvas, "Confirm", new Color(0f, 0f, 0f, 0.85f));
            root = panel.gameObject;
            title = UiKit.Label(panel.transform, "Title", 88, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(title.rectTransform, 0.1f, 0.62f, 0.9f, 0.82f);
            message = UiKit.Label(panel.transform, "Message", 50, TextAnchor.MiddleCenter, UiKit.TextLight);
            UiKit.Place(message.rectTransform, 0.1f, 0.46f, 0.9f, 0.62f);
            confirmButton = UiKit.Button(panel.transform, "Confirm", "", 52, () => Close(onConfirm));
            UiKit.Place((RectTransform)confirmButton.transform, 0.18f, 0.18f, 0.48f, 0.32f);
            cancelButton = UiKit.Button(panel.transform, "Cancel", "", 52, () => Close(onCancel));
            UiKit.Place((RectTransform)cancelButton.transform, 0.52f, 0.18f, 0.82f, 0.32f);
            root.SetActive(false);
        }

        public void Show(string titleText, string messageText, string confirmText, string cancelText,
            Action confirmed, Action cancelled)
        {
            title.text = titleText;
            message.text = messageText;
            UiKit.ButtonLabel(confirmButton).text = confirmText;
            UiKit.ButtonLabel(cancelButton).text = cancelText;
            onConfirm = confirmed;
            onCancel = cancelled;
            root.transform.SetAsLastSibling();
            root.SetActive(true);
        }

        void Close(Action then)
        {
            root.SetActive(false);
            then?.Invoke();
        }
    }
}
