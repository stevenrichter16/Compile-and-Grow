using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeEditor.Core;
using CodeEditor.Language;

namespace CodeEditor.View
{
    public sealed class LinePool : MonoBehaviour
    {
        private readonly List<TMP_Text> _pool = new List<TMP_Text>();
        private readonly Dictionary<int, int> _lineToPoolIndex = new Dictionary<int, int>();
        private readonly HashSet<int> _usedSlots = new HashSet<int>();
        private readonly Dictionary<int, string> _slotRawText = new Dictionary<int, string>();
        private RectTransform _parent;
        private TMP_FontAsset _font;
        private float _fontSize;
        private float _lineHeight;
        private float _charWidth;

        public float LineHeight => _lineHeight;
        public float CharWidth => _charWidth;

        public void Initialize(RectTransform parent, TMP_FontAsset font, float fontSize, int poolSize = 45)
        {
            _parent = parent;
            _font = font;
            _fontSize = fontSize;

            // Compute metrics from font
            ComputeMetrics();

            for (int i = 0; i < poolSize; i++)
                CreatePooledLine(i);
        }

        public void UpdateVisibleLines(int firstVisible, int lastVisible, DocumentModel doc)
        {
            int totalLines = doc.LineCount;
            lastVisible = Math.Min(lastVisible, totalLines - 1);

            // Release pool slots for lines no longer visible
            var toRelease = new List<int>();
            foreach (var kvp in _lineToPoolIndex)
            {
                if (kvp.Key < firstVisible || kvp.Key > lastVisible)
                    toRelease.Add(kvp.Key);
            }
            foreach (int line in toRelease)
            {
                int slot = _lineToPoolIndex[line];
                _pool[slot].gameObject.SetActive(false);
                _usedSlots.Remove(slot);
                _slotRawText.Remove(slot);
                _lineToPoolIndex.Remove(line);
            }

            // Assign pool slots for newly visible lines
            for (int line = firstVisible; line <= lastVisible; line++)
            {
                if (line < 0 || line >= totalLines) continue;

                string lineText = doc.GetLine(line);

                if (_lineToPoolIndex.TryGetValue(line, out int existingSlot))
                {
                    // Already assigned — update text only if raw content changed
                    string cached;
                    if (!_slotRawText.TryGetValue(existingSlot, out cached) || cached != lineText)
                    {
                        _slotRawText[existingSlot] = lineText;
                        _pool[existingSlot].text = lineText;
                    }
                    PositionSlot(existingSlot, line);
                    continue;
                }

                int freeSlot = GetFreeSlot();
                if (freeSlot < 0) continue; // pool exhausted

                _lineToPoolIndex[line] = freeSlot;
                _usedSlots.Add(freeSlot);
                _slotRawText[freeSlot] = lineText;
                var text = _pool[freeSlot];
                text.text = lineText;
                text.gameObject.SetActive(true);
                PositionSlot(freeSlot, line);
            }
        }

        public Vector2 GetPixelPosition(TextPosition pos)
        {
            float x = pos.Column * _charWidth; // fallback
            float y = pos.Line * _lineHeight;

            // Use TMP's actual rendered character positions for accurate X
            if (_lineToPoolIndex.TryGetValue(pos.Line, out int slot))
            {
                var tmp = _pool[slot];
                tmp.ForceMeshUpdate();
                var textInfo = tmp.textInfo;
                if (textInfo != null && textInfo.characterCount > 0)
                {
                    if (pos.Column < textInfo.characterCount)
                    {
                        x = textInfo.characterInfo[pos.Column].origin;
                    }
                    else
                    {
                        // Cursor after last character — use xAdvance of the last char
                        x = textInfo.characterInfo[textInfo.characterCount - 1].xAdvance;
                    }
                }
            }

            return new Vector2(x, -y);
        }

        private void ComputeMetrics()
        {
            if (_font == null) return;

            // Use font face info directly — avoids needing a laid-out Canvas
            var faceInfo = _font.faceInfo;
            float scale = _fontSize / faceInfo.pointSize;

            _lineHeight = faceInfo.lineHeight * scale;
            if (_lineHeight <= 0f)
                _lineHeight = _fontSize * 1.2f;

            // Get monospaced char width from the 'M' glyph
            _charWidth = _fontSize * 0.6f; // fallback
            if (_font.characterLookupTable != null && _font.characterLookupTable.ContainsKey('M'))
            {
                var glyph = _font.characterLookupTable['M'].glyph;
                _charWidth = glyph.metrics.horizontalAdvance * scale;
            }

            if (_charWidth <= 0f)
                _charWidth = _fontSize * 0.6f;
        }

        private void CreatePooledLine(int index)
        {
            var go = new GameObject($"Line_{index}", typeof(RectTransform), typeof(Canvas), typeof(TextMeshProUGUI));
            go.transform.SetParent(_parent, false);
            go.SetActive(false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = _font;
            tmp.fontSize = _fontSize;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.richText = true;
            tmp.raycastTarget = false;

            _pool.Add(tmp);
        }

        private void PositionSlot(int slot, int lineIndex)
        {
            var rt = _pool[slot].rectTransform;
            rt.anchoredPosition = new Vector2(0, -lineIndex * _lineHeight);
            rt.sizeDelta = new Vector2(0, _lineHeight);
        }

        public void UpdateFontSize(float fontSize)
        {
            _fontSize = fontSize;
            ComputeMetrics();

            // Update all existing pooled line objects
            foreach (var tmp in _pool)
            {
                tmp.fontSize = _fontSize;
            }

            // Reposition all currently-mapped lines with new line height
            foreach (var kvp in _lineToPoolIndex)
            {
                PositionSlot(kvp.Value, kvp.Key);
            }

            // Expand pool if smaller font means more visible lines
            if (_parent != null)
            {
                int neededSlots = Mathf.CeilToInt(_parent.rect.height / _lineHeight) + 5;
                while (_pool.Count < neededSlots)
                    CreatePooledLine(_pool.Count);
            }
        }

        public TextPosition GetTextPosition(Vector2 localPos, DocumentModel doc)
        {
            int totalLines = doc.LineCount;

            int line = Mathf.FloorToInt(-localPos.y / _lineHeight);
            line = Mathf.Clamp(line, 0, totalLines - 1);

            int column;
            if (_lineToPoolIndex.TryGetValue(line, out int slot))
            {
                var tmp = _pool[slot];
                tmp.ForceMeshUpdate();
                var textInfo = tmp.textInfo;

                if (textInfo == null || textInfo.characterCount == 0)
                {
                    column = 0;
                }
                else
                {
                    column = textInfo.characterCount;
                    for (int i = 0; i < textInfo.characterCount; i++)
                    {
                        var charInfo = textInfo.characterInfo[i];
                        float midX = (charInfo.origin + charInfo.xAdvance) * 0.5f;
                        if (localPos.x < midX)
                        {
                            column = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                column = Mathf.Max(0, Mathf.RoundToInt(localPos.x / _charWidth));
            }

            int lineLength = doc.GetLineLength(line);
            column = Mathf.Clamp(column, 0, lineLength);

            return new TextPosition(line, column);
        }

        internal string GetDisplayedText(int lineIndex)
        {
            if (_lineToPoolIndex.TryGetValue(lineIndex, out int slot))
            {
                if (_slotRawText.TryGetValue(slot, out string raw))
                    return raw;
                return _pool[slot].text;
            }
            return null;
        }

        public void ApplyHighlighting(int lineIndex, IReadOnlyList<HighlightToken> tokens,
            Func<TokenCategory, Color32> colorMapper, Color32 defaultColor)
        {
            if (!_lineToPoolIndex.TryGetValue(lineIndex, out int slot)) return;
            if (!_slotRawText.TryGetValue(slot, out string rawText)) return;
            if (string.IsNullOrEmpty(rawText) || tokens == null || tokens.Count == 0) return;

            var sb = new StringBuilder(rawText.Length * 3);
            int pos = 0;

            for (int t = 0; t < tokens.Count; t++)
            {
                int tokStart = Math.Max(0, Math.Min(tokens[t].StartColumn, rawText.Length));
                int tokEnd = Math.Max(tokStart, Math.Min(tokens[t].StartColumn + tokens[t].Length, rawText.Length));

                // Uncolored text before this token
                if (pos < tokStart)
                    AppendNoparse(sb, rawText, pos, tokStart - pos);

                // Colored token text
                if (tokStart < tokEnd)
                {
                    Color32 c = colorMapper(tokens[t].Category);
                    sb.Append("<color=#");
                    AppendHex(sb, c);
                    sb.Append('>');
                    AppendNoparse(sb, rawText, tokStart, tokEnd - tokStart);
                    sb.Append("</color>");
                }

                pos = tokEnd;
            }

            // Remaining text after last token
            if (pos < rawText.Length)
                AppendNoparse(sb, rawText, pos, rawText.Length - pos);

            _pool[slot].text = sb.ToString();
        }

        private static readonly char[] s_hex = "0123456789ABCDEF".ToCharArray();

        private static void AppendHex(StringBuilder sb, Color32 c)
        {
            sb.Append(s_hex[c.r >> 4]); sb.Append(s_hex[c.r & 0xF]);
            sb.Append(s_hex[c.g >> 4]); sb.Append(s_hex[c.g & 0xF]);
            sb.Append(s_hex[c.b >> 4]); sb.Append(s_hex[c.b & 0xF]);
        }

        private static void AppendNoparse(StringBuilder sb, string text, int start, int length)
        {
            sb.Append("<noparse>");
            sb.Append(text, start, length);
            sb.Append("</noparse>");
        }

        private int GetFreeSlot()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_usedSlots.Contains(i))
                    return i;
            }
            return -1;
        }
    }
}
