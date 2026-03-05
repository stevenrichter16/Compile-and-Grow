using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeEditor.Core;

namespace CodeEditor.Completion
{
    public sealed class CompletionPopup : MonoBehaviour
    {
        private const int MaxVisibleRows = 8;
        private const float RowHeight = 22f;
        private const float PopupWidth = 280f;
        private const float PaddingH = 6f;
        private const float KindLabelWidth = 18f;

        private RectTransform _root;
        private RectTransform _content;
        private ScrollRect _scrollRect;
        private readonly List<PopupRow> _rows = new List<PopupRow>();
        private CompletionResult _currentResult;
        private int _selectedIndex;
        private bool _visible;

        private TMP_FontAsset _font;
        private float _fontSize;
        private Color _bgColor = new Color(0.16f, 0.16f, 0.18f, 0.97f);
        private Color _selectedColor = new Color(0.24f, 0.44f, 0.72f, 0.85f);
        private Color _textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private Color _detailColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        public bool IsVisible => _visible;
        public int SelectedIndex => _selectedIndex;
        public CompletionResult CurrentResult => _currentResult;

        public event Action<CompletionItem> ItemAccepted;

        public void Initialize(RectTransform parent, TMP_FontAsset font, float fontSize)
        {
            _font = font;
            _fontSize = Mathf.Max(10f, fontSize * 0.85f);
            BuildUI(parent);
            Hide();
        }

        public void Show(CompletionResult result, Vector2 caretPixelPos, float lineHeight)
        {
            if (result == null || result.Items.Count == 0)
            {
                Hide();
                return;
            }

            _currentResult = result;
            _selectedIndex = 0;

            int rowCount = Mathf.Min(result.Items.Count, MaxVisibleRows);
            float popupHeight = rowCount * RowHeight + 4f; // 2px top/bottom padding

            // Position below the caret line
            _root.anchoredPosition = new Vector2(caretPixelPos.x, caretPixelPos.y - lineHeight - 2f);
            _root.sizeDelta = new Vector2(PopupWidth, popupHeight);
            _content.sizeDelta = new Vector2(PopupWidth, result.Items.Count * RowHeight);

            // Ensure we have enough pooled rows
            while (_rows.Count < rowCount)
                CreateRow(_rows.Count);

            // Populate visible rows
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i < result.Items.Count)
                {
                    var item = result.Items[i];
                    _rows[i].Root.gameObject.SetActive(true);
                    _rows[i].Label.text = item.Label;
                    _rows[i].Detail.text = item.Detail ?? "";
                    _rows[i].KindLabel.text = KindToLetter(item.Kind);
                    _rows[i].KindLabel.color = KindToColor(item.Kind);
                    PositionRow(i);
                }
                else
                {
                    _rows[i].Root.gameObject.SetActive(false);
                }
            }

            UpdateSelection();
            _root.gameObject.SetActive(true);
            _visible = true;
        }

        public void Hide()
        {
            if (_root != null)
                _root.gameObject.SetActive(false);
            _visible = false;
            _currentResult = null;
        }

        public void MoveSelection(int delta)
        {
            if (_currentResult == null || _currentResult.Items.Count == 0) return;
            _selectedIndex = Mathf.Clamp(_selectedIndex + delta, 0, _currentResult.Items.Count - 1);
            UpdateSelection();
            ScrollToSelected();
        }

        public void AcceptSelected()
        {
            if (_currentResult == null || _currentResult.Items.Count == 0) return;
            if (_selectedIndex < 0 || _selectedIndex >= _currentResult.Items.Count) return;
            var item = _currentResult.Items[_selectedIndex];
            var result = _currentResult; // capture before Hide clears it
            Hide();
            _lastAcceptedResult = result;
            ItemAccepted?.Invoke(item);
            _lastAcceptedResult = null;
        }

        private CompletionResult _lastAcceptedResult;

        /// <summary>
        /// Returns the result that was active when AcceptSelected was called.
        /// Only valid during the ItemAccepted callback.
        /// </summary>
        public CompletionResult LastAcceptedResult => _lastAcceptedResult;

        public void UpdateFilter(CompletionResult newResult)
        {
            if (newResult == null || newResult.Items.Count == 0)
            {
                Hide();
                return;
            }

            // Preserve selection label if possible
            string prevLabel = null;
            if (_currentResult != null && _selectedIndex >= 0 && _selectedIndex < _currentResult.Items.Count)
                prevLabel = _currentResult.Items[_selectedIndex].Label;

            _currentResult = newResult;

            int newSel = 0;
            if (prevLabel != null)
            {
                for (int i = 0; i < newResult.Items.Count; i++)
                {
                    if (newResult.Items[i].Label == prevLabel)
                    {
                        newSel = i;
                        break;
                    }
                }
            }
            _selectedIndex = Mathf.Clamp(newSel, 0, newResult.Items.Count - 1);

            int rowCount = Mathf.Min(newResult.Items.Count, MaxVisibleRows);
            float popupHeight = rowCount * RowHeight + 4f;
            _root.sizeDelta = new Vector2(PopupWidth, popupHeight);
            _content.sizeDelta = new Vector2(PopupWidth, newResult.Items.Count * RowHeight);

            while (_rows.Count < rowCount)
                CreateRow(_rows.Count);

            for (int i = 0; i < _rows.Count; i++)
            {
                if (i < newResult.Items.Count)
                {
                    var item = newResult.Items[i];
                    _rows[i].Root.gameObject.SetActive(true);
                    _rows[i].Label.text = item.Label;
                    _rows[i].Detail.text = item.Detail ?? "";
                    _rows[i].KindLabel.text = KindToLetter(item.Kind);
                    _rows[i].KindLabel.color = KindToColor(item.Kind);
                    PositionRow(i);
                }
                else
                {
                    _rows[i].Root.gameObject.SetActive(false);
                }
            }

            UpdateSelection();
            ScrollToSelected();
        }

        private void BuildUI(RectTransform parent)
        {
            // Root container
            var rootGo = new GameObject("CompletionPopup", typeof(RectTransform), typeof(Canvas), typeof(Image));
            rootGo.transform.SetParent(parent, false);
            _root = rootGo.GetComponent<RectTransform>();
            _root.anchorMin = new Vector2(0, 1);
            _root.anchorMax = new Vector2(0, 1);
            _root.pivot = new Vector2(0, 1);

            var rootCanvas = rootGo.GetComponent<Canvas>();
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = 100;

            var bg = rootGo.GetComponent<Image>();
            bg.color = _bgColor;
            bg.raycastTarget = true;
            bg.maskable = false;

            // Scroll rect + viewport
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(_root, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = new Vector2(-4f, -4f); // 2px padding each side
            viewportRt.anchoredPosition = Vector2.zero;
            var viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.color = Color.clear;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0, 1);
            _content.anchoredPosition = Vector2.zero;

            _scrollRect = rootGo.AddComponent<ScrollRect>();
            _scrollRect.viewport = viewportRt;
            _scrollRect.content = _content;
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = RowHeight;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Pre-create rows
            for (int i = 0; i < MaxVisibleRows; i++)
                CreateRow(i);
        }

        private void CreateRow(int index)
        {
            var rowGo = new GameObject($"Row_{index}", typeof(RectTransform), typeof(Image));
            rowGo.transform.SetParent(_content, false);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0, 1);
            rowRt.anchorMax = new Vector2(1, 1);
            rowRt.pivot = new Vector2(0, 1);
            rowRt.sizeDelta = new Vector2(0, RowHeight);

            var rowBg = rowGo.GetComponent<Image>();
            rowBg.color = Color.clear;
            rowBg.raycastTarget = false;
            rowBg.maskable = false;

            // Kind letter (F, K, V, C, M, etc.)
            var kindGo = new GameObject("Kind", typeof(RectTransform), typeof(TextMeshProUGUI));
            kindGo.transform.SetParent(rowGo.transform, false);
            var kindRt = kindGo.GetComponent<RectTransform>();
            kindRt.anchorMin = new Vector2(0, 0);
            kindRt.anchorMax = new Vector2(0, 1);
            kindRt.pivot = new Vector2(0, 0.5f);
            kindRt.anchoredPosition = new Vector2(PaddingH, 0);
            kindRt.sizeDelta = new Vector2(KindLabelWidth, 0);
            var kindTmp = kindGo.GetComponent<TextMeshProUGUI>();
            kindTmp.font = _font;
            kindTmp.fontSize = _fontSize;
            kindTmp.alignment = TextAlignmentOptions.Center;
            kindTmp.enableWordWrapping = false;
            kindTmp.overflowMode = TextOverflowModes.Truncate;
            kindTmp.raycastTarget = false;
            kindTmp.maskable = false;

            // Label
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(rowGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(0.6f, 1);
            labelRt.pivot = new Vector2(0, 0.5f);
            labelRt.anchoredPosition = new Vector2(PaddingH + KindLabelWidth + 4f, 0);
            labelRt.sizeDelta = new Vector2(-(PaddingH + KindLabelWidth + 4f), 0);
            var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.font = _font;
            labelTmp.fontSize = _fontSize;
            labelTmp.color = _textColor;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.enableWordWrapping = false;
            labelTmp.overflowMode = TextOverflowModes.Truncate;
            labelTmp.raycastTarget = false;
            labelTmp.maskable = false;

            // Detail (right side)
            var detailGo = new GameObject("Detail", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailGo.transform.SetParent(rowGo.transform, false);
            var detailRt = detailGo.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.6f, 0);
            detailRt.anchorMax = new Vector2(1, 1);
            detailRt.pivot = new Vector2(1, 0.5f);
            detailRt.anchoredPosition = new Vector2(-PaddingH, 0);
            detailRt.sizeDelta = new Vector2(-PaddingH, 0);
            var detailTmp = detailGo.GetComponent<TextMeshProUGUI>();
            detailTmp.font = _font;
            detailTmp.fontSize = _fontSize * 0.9f;
            detailTmp.color = _detailColor;
            detailTmp.alignment = TextAlignmentOptions.MidlineRight;
            detailTmp.enableWordWrapping = false;
            detailTmp.overflowMode = TextOverflowModes.Truncate;
            detailTmp.raycastTarget = false;
            detailTmp.maskable = false;

            _rows.Add(new PopupRow
            {
                Root = rowRt,
                Background = rowBg,
                KindLabel = kindTmp,
                Label = labelTmp,
                Detail = detailTmp,
            });

            PositionRow(index);
            rowGo.SetActive(false);
        }

        private void PositionRow(int index)
        {
            _rows[index].Root.anchoredPosition = new Vector2(0, -index * RowHeight);
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (!_rows[i].Root.gameObject.activeSelf) continue;
                _rows[i].Background.color = (i == _selectedIndex) ? _selectedColor : Color.clear;
            }
        }

        private void ScrollToSelected()
        {
            if (_scrollRect == null || _currentResult == null) return;
            int totalItems = _currentResult.Items.Count;
            if (totalItems <= MaxVisibleRows) return;

            float normalizedPos = 1f - (float)_selectedIndex / (totalItems - 1);
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
        }

        private static string KindToLetter(CompletionKind kind)
        {
            switch (kind)
            {
                case CompletionKind.Keyword:  return "K";
                case CompletionKind.Function: return "F";
                case CompletionKind.Variable: return "V";
                case CompletionKind.Constant: return "C";
                case CompletionKind.Method:   return "M";
                case CompletionKind.Property: return "P";
                case CompletionKind.Snippet:  return "S";
                default: return "?";
            }
        }

        private static Color KindToColor(CompletionKind kind)
        {
            switch (kind)
            {
                case CompletionKind.Keyword:  return new Color(0.34f, 0.61f, 0.84f, 1f); // blue
                case CompletionKind.Function: return new Color(0.86f, 0.78f, 0.47f, 1f); // yellow
                case CompletionKind.Variable: return new Color(0.61f, 0.86f, 0.99f, 1f); // light blue
                case CompletionKind.Constant: return new Color(0.71f, 0.56f, 0.85f, 1f); // purple
                case CompletionKind.Method:   return new Color(0.86f, 0.78f, 0.47f, 1f); // yellow
                case CompletionKind.Property: return new Color(0.61f, 0.86f, 0.99f, 1f); // light blue
                case CompletionKind.Snippet:  return new Color(0.6f, 0.6f, 0.6f, 1f);    // gray
                default: return Color.white;
            }
        }

        private struct PopupRow
        {
            public RectTransform Root;
            public Image Background;
            public TMP_Text KindLabel;
            public TMP_Text Label;
            public TMP_Text Detail;
        }
    }
}
