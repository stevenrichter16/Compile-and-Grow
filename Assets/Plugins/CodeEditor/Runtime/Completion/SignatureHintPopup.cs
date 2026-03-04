using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CodeEditor.Completion
{
    public sealed class SignatureHintPopup : MonoBehaviour
    {
        private RectTransform _root;
        private TMP_Text _label;
        private bool _visible;

        private TMP_FontAsset _font;
        private float _fontSize;

        public bool IsVisible => _visible;

        public void Initialize(RectTransform parent, TMP_FontAsset font, float fontSize)
        {
            _font = font;
            _fontSize = Mathf.Max(10f, fontSize * 0.85f);
            BuildUI(parent);
            Hide();
        }

        public void Show(SignatureHint hint, int activeParameter, Vector2 caretPixelPos, float lineHeight)
        {
            if (hint == null)
            {
                Hide();
                return;
            }

            _label.text = hint.Format(activeParameter);
            _label.ForceMeshUpdate();

            // Size to fit text
            float textWidth = _label.preferredWidth + 16f; // 8px padding each side
            float textHeight = _label.preferredHeight + 10f;
            _root.sizeDelta = new Vector2(Mathf.Min(textWidth, 400f), textHeight);

            // Position above the caret line
            _root.anchoredPosition = new Vector2(caretPixelPos.x, caretPixelPos.y + 4f);

            _root.gameObject.SetActive(true);
            _visible = true;
        }

        public void UpdateHint(SignatureHint hint, int activeParameter)
        {
            if (hint == null)
            {
                Hide();
                return;
            }
            _label.text = hint.Format(activeParameter);
        }

        public void Hide()
        {
            if (_root != null)
                _root.gameObject.SetActive(false);
            _visible = false;
        }

        private void BuildUI(RectTransform parent)
        {
            // Root
            var rootGo = new GameObject("SignatureHintPopup", typeof(RectTransform), typeof(Canvas), typeof(Image));
            rootGo.transform.SetParent(parent, false);
            _root = rootGo.GetComponent<RectTransform>();
            _root.anchorMin = new Vector2(0, 1);
            _root.anchorMax = new Vector2(0, 1);
            _root.pivot = new Vector2(0, 0); // bottom-left pivot so it grows upward from caret

            var rootCanvas = rootGo.GetComponent<Canvas>();
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = 101; // above completion popup

            var bg = rootGo.GetComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.22f, 0.97f);
            bg.raycastTarget = false;

            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(rootGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            labelRt.anchoredPosition = Vector2.zero;

            _label = labelGo.GetComponent<TextMeshProUGUI>();
            _label.font = _font;
            _label.fontSize = _fontSize;
            _label.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.enableWordWrapping = false;
            _label.overflowMode = TextOverflowModes.Truncate;
            _label.richText = true;
            _label.raycastTarget = false;
            _label.margin = new Vector4(8, 2, 8, 2);
        }
    }
}
