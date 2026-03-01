using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeEditor.Core;

namespace CodeEditor.View
{
    public sealed class SelectionRenderer : MonoBehaviour
    {
        private readonly List<Image> _highlights = new List<Image>();
        private Color _selectionColor = new Color(0.25f, 0.47f, 0.77f, 0.35f);
        private RectTransform _parent;

        public void Initialize(RectTransform parent)
        {
            _parent = parent;
        }

        public void SetSelectionColor(Color color)
        {
            _selectionColor = color;
        }

        public void UpdateSelection(TextRange selection, LinePool linePool, float lineHeight, int firstVisibleLine, int lastVisibleLine)
        {
            if (selection.IsEmpty)
            {
                HideAll();
                return;
            }

            int startLine = selection.Start.Line;
            int endLine = selection.End.Line;

            // Clamp to visible range
            int drawStart = Mathf.Max(startLine, firstVisibleLine);
            int drawEnd = Mathf.Min(endLine, lastVisibleLine);

            int needed = drawEnd - drawStart + 1;
            EnsureHighlightCount(needed);
            HideAll();

            for (int i = 0; i <= drawEnd - drawStart; i++)
            {
                int line = drawStart + i;
                float startX, endX;

                if (line == startLine && line == endLine)
                {
                    startX = linePool.GetPixelPosition(selection.Start).x;
                    endX = linePool.GetPixelPosition(selection.End).x;
                }
                else if (line == startLine)
                {
                    startX = linePool.GetPixelPosition(selection.Start).x;
                    endX = Mathf.Max(startX + linePool.CharWidth, _parent.rect.width);
                }
                else if (line == endLine)
                {
                    startX = 0f;
                    endX = linePool.GetPixelPosition(selection.End).x;
                }
                else
                {
                    startX = 0f;
                    endX = _parent.rect.width;
                }

                float width = endX - startX;
                if (width <= 0) continue;

                var img = _highlights[i];
                img.enabled = true;
                var rt = img.rectTransform;
                rt.anchoredPosition = new Vector2(startX, -line * lineHeight);
                rt.sizeDelta = new Vector2(width, lineHeight);
            }
        }

        public void HideAll()
        {
            for (int i = 0; i < _highlights.Count; i++)
                _highlights[i].enabled = false;
        }

        private void EnsureHighlightCount(int count)
        {
            while (_highlights.Count < count)
            {
                var go = new GameObject($"SelectionHighlight_{_highlights.Count}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_parent, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);

                var img = go.GetComponent<Image>();
                img.color = _selectionColor;
                img.raycastTarget = false;
                img.enabled = false;

                _highlights.Add(img);
            }
        }
    }
}
