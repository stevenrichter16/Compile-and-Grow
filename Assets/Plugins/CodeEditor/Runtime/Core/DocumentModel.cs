using System;
using System.Collections.Generic;
using System.Text;

namespace CodeEditor.Core
{
    public readonly struct DocumentChangeEventArgs
    {
        public readonly TextRange DeletedRange;
        public readonly TextPosition InsertStart;
        public readonly string InsertedText;
        public readonly int OldVersion;
        public readonly int NewVersion;

        public DocumentChangeEventArgs(TextRange deletedRange, TextPosition insertStart, string insertedText, int oldVersion, int newVersion)
        {
            DeletedRange = deletedRange;
            InsertStart = insertStart;
            InsertedText = insertedText;
            OldVersion = oldVersion;
            NewVersion = newVersion;
        }
    }

    public sealed class DocumentModel
    {
        private readonly List<string> _lines = new List<string> { string.Empty };
        private TextPosition _cursor = TextPosition.Zero;
        private TextPosition _selectionAnchor = TextPosition.Zero;
        private int _version;

        public event Action<DocumentChangeEventArgs> Changed;
        public event Action<TextPosition> CursorMoved;

        public int LineCount => _lines.Count;
        public int Version => _version;

        public TextPosition Cursor => _cursor;
        public TextPosition SelectionAnchor => _selectionAnchor;
        public bool HasSelection => !_cursor.Equals(_selectionAnchor);

        public TextRange SelectionRange => new TextRange(_selectionAnchor, _cursor);

        public string GetLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _lines.Count)
                throw new ArgumentOutOfRangeException(nameof(lineIndex));
            return _lines[lineIndex];
        }

        public int GetLineLength(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _lines.Count)
                throw new ArgumentOutOfRangeException(nameof(lineIndex));
            return _lines[lineIndex].Length;
        }

        public string GetText()
        {
            return string.Join("\n", _lines);
        }

        public string GetText(TextRange range)
        {
            if (range.IsEmpty) return string.Empty;

            var start = ClampPos(range.Start);
            var end = ClampPos(range.End);

            if (start.Line == end.Line)
                return _lines[start.Line].Substring(start.Column, end.Column - start.Column);

            var sb = new StringBuilder();
            sb.Append(_lines[start.Line], start.Column, _lines[start.Line].Length - start.Column);
            for (int i = start.Line + 1; i < end.Line; i++)
            {
                sb.Append('\n');
                sb.Append(_lines[i]);
            }
            sb.Append('\n');
            sb.Append(_lines[end.Line], 0, end.Column);
            return sb.ToString();
        }

        public int GetCharOffset(TextPosition pos)
        {
            var p = ClampPos(pos);
            int offset = 0;
            for (int i = 0; i < p.Line; i++)
                offset += _lines[i].Length + 1; // +1 for \n
            return offset + p.Column;
        }

        public TextPosition GetPosition(int charOffset)
        {
            if (charOffset <= 0) return TextPosition.Zero;

            int remaining = charOffset;
            for (int i = 0; i < _lines.Count; i++)
            {
                int lineLen = _lines[i].Length;
                if (remaining <= lineLen)
                    return new TextPosition(i, remaining);
                remaining -= lineLen + 1; // +1 for \n
            }
            return new TextPosition(_lines.Count - 1, _lines[_lines.Count - 1].Length);
        }

        public void Insert(TextPosition at, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var pos = ClampPos(at);
            int oldVersion = _version;

            string line = _lines[pos.Line];
            string before = line.Substring(0, pos.Column);
            string after = line.Substring(pos.Column);

            string[] newLines = text.Split('\n');

            if (newLines.Length == 1)
            {
                _lines[pos.Line] = before + newLines[0] + after;
            }
            else
            {
                _lines[pos.Line] = before + newLines[0];
                for (int i = 1; i < newLines.Length - 1; i++)
                    _lines.Insert(pos.Line + i, newLines[i]);
                _lines.Insert(pos.Line + newLines.Length - 1, newLines[newLines.Length - 1] + after);
            }

            _version++;
            Changed?.Invoke(new DocumentChangeEventArgs(
                new TextRange(pos, pos),
                pos,
                text,
                oldVersion,
                _version));
        }

        public void Delete(TextRange range)
        {
            if (range.IsEmpty) return;

            var start = ClampPos(range.Start);
            var end = ClampPos(range.End);
            int oldVersion = _version;

            string before = _lines[start.Line].Substring(0, start.Column);
            string after = _lines[end.Line].Substring(end.Column);
            _lines[start.Line] = before + after;

            if (end.Line > start.Line)
                _lines.RemoveRange(start.Line + 1, end.Line - start.Line);

            _version++;
            Changed?.Invoke(new DocumentChangeEventArgs(
                new TextRange(start, end),
                start,
                string.Empty,
                oldVersion,
                _version));
        }

        public void Replace(TextRange range, string text)
        {
            Delete(range);
            Insert(range.Start, text);
        }

        public void SetCursor(TextPosition pos, bool extendSelection = false)
        {
            var clamped = ClampPos(pos);
            _cursor = clamped;
            if (!extendSelection)
                _selectionAnchor = clamped;
            CursorMoved?.Invoke(_cursor);
        }

        public void SetSelection(TextPosition anchor, TextPosition cursor)
        {
            _selectionAnchor = ClampPos(anchor);
            _cursor = ClampPos(cursor);
            CursorMoved?.Invoke(_cursor);
        }

        public void SelectAll()
        {
            _selectionAnchor = TextPosition.Zero;
            var lastLine = _lines.Count - 1;
            _cursor = new TextPosition(lastLine, _lines[lastLine].Length);
            CursorMoved?.Invoke(_cursor);
        }

        public void SetText(string fullText)
        {
            int oldVersion = _version;
            var oldEnd = new TextPosition(_lines.Count - 1, _lines[_lines.Count - 1].Length);

            _lines.Clear();
            if (string.IsNullOrEmpty(fullText))
            {
                _lines.Add(string.Empty);
            }
            else
            {
                string[] split = fullText.Split('\n');
                for (int i = 0; i < split.Length; i++)
                    _lines.Add(split[i]);
            }

            _cursor = TextPosition.Zero;
            _selectionAnchor = TextPosition.Zero;
            _version++;

            Changed?.Invoke(new DocumentChangeEventArgs(
                new TextRange(TextPosition.Zero, oldEnd),
                TextPosition.Zero,
                fullText ?? string.Empty,
                oldVersion,
                _version));
            CursorMoved?.Invoke(_cursor);
        }

        private TextPosition ClampPos(TextPosition pos)
        {
            int line = Math.Max(0, Math.Min(pos.Line, _lines.Count - 1));
            int col = Math.Max(0, Math.Min(pos.Column, _lines[line].Length));
            return new TextPosition(line, col);
        }
    }
}
