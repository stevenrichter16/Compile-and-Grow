using System;
using UnityEngine;
using UnityEngine.UI;

namespace CodeEditor.View
{
    public sealed class ScrollManager : MonoBehaviour
    {
        private ScrollRect _scrollRect;
        private RectTransform _content;
        private float _lineHeight;
        private float _viewportHeight;
        private int _totalLines;

        public int FirstVisibleLine { get; private set; }
        public int LastVisibleLine { get; private set; }
        public int VisibleLineCount => LastVisibleLine - FirstVisibleLine + 1;
        public float LineHeight => _lineHeight;

        public void Initialize(ScrollRect scrollRect, float lineHeight)
        {
            _scrollRect = scrollRect;
            _content = scrollRect.content;
            _lineHeight = Mathf.Max(1f, lineHeight);
            _scrollRect.onValueChanged.AddListener(OnScroll);
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScroll);
        }

        public void UpdateLineHeight(float lineHeight)
        {
            _lineHeight = Mathf.Max(1f, lineHeight);
        }

        public void UpdateContentSize(int totalLines)
        {
            _totalLines = totalLines;
            // Content needs at least viewport height so ScrollRect doesn't fight us
            float viewH = _scrollRect.viewport != null ? _scrollRect.viewport.rect.height : 0f;
            float contentHeight = Mathf.Max(totalLines * _lineHeight, viewH);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            RecalculateVisibleRange();
        }

        public void ScrollToLine(int line)
        {
            float contentHeight = _content.rect.height;
            float viewH = _scrollRect.viewport != null ? _scrollRect.viewport.rect.height : 0f;
            float scrollableHeight = contentHeight - viewH;

            if (scrollableHeight <= 0f)
            {
                // Content fits in viewport — pin to top
                _scrollRect.verticalNormalizedPosition = 1f;
            }
            else
            {
                float targetY = line * _lineHeight;
                // normalizedPosition: 1 = top, 0 = bottom
                float normalized = 1f - Mathf.Clamp01(targetY / scrollableHeight);
                _scrollRect.verticalNormalizedPosition = normalized;
            }

            RecalculateVisibleRange();
        }

        public void EnsureLineVisible(int line, int buffer = 2)
        {
            RecalculateVisibleRange();
            if (line < FirstVisibleLine + buffer)
                ScrollToLine(Mathf.Max(0, line - buffer));
            else if (line > LastVisibleLine - buffer)
                ScrollToLine(Mathf.Max(0, line - (VisibleLineCount - buffer - 1)));
        }

        public void ScrollByLines(float linesDelta)
        {
            float contentHeight = _content.rect.height;
            float viewH = _scrollRect.viewport != null ? _scrollRect.viewport.rect.height : 0f;
            float scrollableHeight = contentHeight - viewH;
            if (scrollableHeight <= 0f) return;

            float normalizedDelta = (linesDelta * _lineHeight) / scrollableHeight;
            _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                _scrollRect.verticalNormalizedPosition - normalizedDelta);
            RecalculateVisibleRange();
        }

        private void OnScroll(Vector2 _)
        {
            RecalculateVisibleRange();
        }

        private void RecalculateVisibleRange()
        {
            if (_scrollRect == null || _scrollRect.viewport == null) return;

            _viewportHeight = _scrollRect.viewport.rect.height;

            // Derive scroll offset from the ScrollRect's normalized position
            float contentHeight = _content.rect.height;
            float scrollableHeight = contentHeight - _viewportHeight;

            float scrollY;
            if (scrollableHeight <= 0f)
                scrollY = 0f;
            else
                scrollY = (1f - _scrollRect.verticalNormalizedPosition) * scrollableHeight;

            scrollY = Mathf.Max(0f, scrollY);

            FirstVisibleLine = Mathf.Max(0, Mathf.FloorToInt(scrollY / _lineHeight));
            int count = Mathf.CeilToInt(_viewportHeight / _lineHeight) + 1;
            LastVisibleLine = FirstVisibleLine + count;
        }
    }
}
