using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CodeEditor.View
{
    public sealed class LineNumberGutter : MonoBehaviour
    {
        private readonly List<TMP_Text> _pool = new List<TMP_Text>();
        private readonly HashSet<int> _activeSlots = new HashSet<int>();
        private RectTransform _parent;
        private TMP_FontAsset _font;
        private float _fontSize;
        private float _lineHeight;
        private Color _textColor = new Color(1f, 1f, 1f, 0.5f);

        public void Initialize(RectTransform parent, TMP_FontAsset font, float fontSize, float lineHeight, int poolSize = 45)
        {
            _parent = parent;
            _font = font;
            _fontSize = fontSize;
            _lineHeight = lineHeight;

            for (int i = 0; i < poolSize; i++)
                CreatePooledNumber(i);
        }

        public void SetTextColor(Color color)
        {
            _textColor = color;
            foreach (var tmp in _pool)
                tmp.color = _textColor;
        }

        public void UpdateFontSize(float fontSize, float lineHeight)
        {
            _fontSize = fontSize;
            _lineHeight = lineHeight;

            foreach (var tmp in _pool)
            {
                tmp.fontSize = _fontSize;
                tmp.rectTransform.sizeDelta = new Vector2(0, _lineHeight);
            }
        }

        public void UpdateVisibleLines(int firstVisible, int lastVisible, int totalLines)
        {
            lastVisible = Mathf.Min(lastVisible, totalLines - 1);

            // Hide all first
            foreach (int slot in _activeSlots)
                _pool[slot].gameObject.SetActive(false);
            _activeSlots.Clear();

            int slotIndex = 0;
            for (int line = firstVisible; line <= lastVisible && slotIndex < _pool.Count; line++)
            {
                if (line < 0 || line >= totalLines) continue;

                var tmp = _pool[slotIndex];
                tmp.text = (line + 1).ToString();
                tmp.gameObject.SetActive(true);
                var rt = tmp.rectTransform;
                rt.anchoredPosition = new Vector2(0, -line * _lineHeight);
                _activeSlots.Add(slotIndex);
                slotIndex++;
            }
        }

        private void CreatePooledNumber(int index)
        {
            var go = new GameObject($"LineNum_{index}", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_parent, false);
            go.SetActive(false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1); // right-aligned
            rt.sizeDelta = new Vector2(0, _lineHeight);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = _font;
            tmp.fontSize = _fontSize;
            tmp.enableWordWrapping = false;
            tmp.alignment = TextAlignmentOptions.TopRight;
            tmp.color = _textColor;
            tmp.raycastTarget = false;

            _pool.Add(tmp);
        }
    }
}
