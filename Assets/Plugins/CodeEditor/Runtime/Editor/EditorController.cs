using System;
using CodeEditor.Core;
using CodeEditor.Commands;
using CodeEditor.Language;

namespace CodeEditor.Editor
{
    public enum MoveDirection
    {
        Left, Right, Up, Down,
        Home, End,
        PageUp, PageDown,
        DocumentStart, DocumentEnd,
    }

    public sealed class EditorController
    {
        private readonly DocumentModel _doc;
        private readonly CommandHistory _history;
        private readonly EditorConfig _config;
        private ILanguageService _languageService;

        public EditorController(DocumentModel doc, EditorConfig config, ILanguageService languageService = null)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _languageService = languageService ?? new PlainTextLanguageService();
            _history = new CommandHistory();
        }

        public DocumentModel Document => _doc;
        public CommandHistory History => _history;
        public EditorConfig Config => _config;

        public void SetLanguageService(ILanguageService service)
        {
            _languageService = service ?? new PlainTextLanguageService();
        }

        public void TypeCharacter(char c)
        {
            if (c == '\n' || c == '\r')
            {
                Enter();
                return;
            }

            string text = c.ToString();
            if (_doc.HasSelection)
            {
                var sel = _doc.SelectionRange;
                _history.Execute(new InsertTextCommand(sel.Start, text, sel), _doc);
            }
            else
            {
                _history.Execute(new InsertTextCommand(_doc.Cursor, text), _doc);
            }
        }

        public void TypeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _history.BreakBatch();

            if (_doc.HasSelection)
            {
                var sel = _doc.SelectionRange;
                _history.Execute(new InsertTextCommand(sel.Start, text, sel), _doc);
            }
            else
            {
                _history.Execute(new InsertTextCommand(_doc.Cursor, text), _doc);
            }

            _history.BreakBatch();
        }

        public void Backspace()
        {
            if (_doc.HasSelection)
            {
                _history.Execute(new DeleteTextCommand(_doc.SelectionRange), _doc);
                return;
            }

            var cursor = _doc.Cursor;
            if (cursor.Equals(TextPosition.Zero)) return;

            TextPosition before;
            if (cursor.Column > 0)
            {
                before = new TextPosition(cursor.Line, cursor.Column - 1);
            }
            else
            {
                int prevLine = cursor.Line - 1;
                before = new TextPosition(prevLine, _doc.GetLineLength(prevLine));
            }

            _history.Execute(new DeleteTextCommand(new TextRange(before, cursor)), _doc);
        }

        public void Delete()
        {
            if (_doc.HasSelection)
            {
                _history.Execute(new DeleteTextCommand(_doc.SelectionRange), _doc);
                return;
            }

            var cursor = _doc.Cursor;
            int lineLen = _doc.GetLineLength(cursor.Line);

            TextPosition after;
            if (cursor.Column < lineLen)
            {
                after = new TextPosition(cursor.Line, cursor.Column + 1);
            }
            else if (cursor.Line < _doc.LineCount - 1)
            {
                after = new TextPosition(cursor.Line + 1, 0);
            }
            else
            {
                return; // at end of document
            }

            _history.Execute(new DeleteTextCommand(new TextRange(cursor, after)), _doc);
        }

        public void Enter()
        {
            _history.BreakBatch();

            int cursorLine = _doc.Cursor.Line;
            string currentLine = _doc.GetLine(cursorLine);

            // Trim whitespace-only lines when leaving via Enter (VSCode behavior)
            bool isWhitespaceOnly = currentLine.Length > 0 && currentLine.TrimStart().Length == 0;
            string lineForIndent = isWhitespaceOnly ? "" : currentLine;
            string indent = TextOperations.ComputeAutoIndent(lineForIndent, _config.IndentSize, _languageService);
            string insertion = "\n" + indent;

            if (_doc.HasSelection)
            {
                var sel = _doc.SelectionRange;
                _history.Execute(new InsertTextCommand(sel.Start, insertion, sel), _doc);
            }
            else if (isWhitespaceOnly)
            {
                // Replace whitespace before cursor + insert newline as one undoable operation
                var wsRange = new TextRange(
                    new TextPosition(cursorLine, 0),
                    _doc.Cursor);
                _history.Execute(new InsertTextCommand(new TextPosition(cursorLine, 0), insertion, wsRange), _doc);
            }
            else
            {
                _history.Execute(new InsertTextCommand(_doc.Cursor, insertion), _doc);
            }

            _history.BreakBatch();
        }

        public void Tab(bool shift)
        {
            _history.BreakBatch();

            var sel = _doc.SelectionRange;
            int firstLine = sel.Start.Line;
            int lastLine = sel.End.Line;

            // Multi-line selection or shift always does block indent/outdent
            if (firstLine != lastLine || shift)
            {
                _history.Execute(new IndentCommand(firstLine, lastLine, _config.IndentSize, shift), _doc);
            }
            else if (_config.TabInsertSpaces)
            {
                // Insert spaces to next tab stop
                int col = _doc.Cursor.Column;
                int spaces = _config.IndentSize - (col % _config.IndentSize);
                string indent = new string(' ', spaces);
                if (_doc.HasSelection)
                {
                    _history.Execute(new InsertTextCommand(sel.Start, indent, sel), _doc);
                }
                else
                {
                    _history.Execute(new InsertTextCommand(_doc.Cursor, indent), _doc);
                }
            }
            else
            {
                if (_doc.HasSelection)
                {
                    _history.Execute(new InsertTextCommand(sel.Start, "\t", sel), _doc);
                }
                else
                {
                    _history.Execute(new InsertTextCommand(_doc.Cursor, "\t"), _doc);
                }
            }

            _history.BreakBatch();
        }

        public void Undo() => _history.Undo(_doc);
        public void Redo() => _history.Redo(_doc);
        public void SelectAll() => _doc.SelectAll();

        public void MoveCursor(MoveDirection dir, bool shift, bool word = false)
        {
            var cursor = _doc.Cursor;
            int leavingLine = cursor.Line;
            TextPosition next;

            switch (dir)
            {
                case MoveDirection.Left:
                    next = word ? MoveWordLeft(cursor) : MoveLeft(cursor);
                    break;
                case MoveDirection.Right:
                    next = word ? MoveWordRight(cursor) : MoveRight(cursor);
                    break;
                case MoveDirection.Up:
                    next = cursor.Line > 0
                        ? new TextPosition(cursor.Line - 1, Math.Min(cursor.Column, _doc.GetLineLength(cursor.Line - 1)))
                        : new TextPosition(0, 0);
                    break;
                case MoveDirection.Down:
                    next = cursor.Line < _doc.LineCount - 1
                        ? new TextPosition(cursor.Line + 1, Math.Min(cursor.Column, _doc.GetLineLength(cursor.Line + 1)))
                        : new TextPosition(cursor.Line, _doc.GetLineLength(cursor.Line));
                    break;
                case MoveDirection.Home:
                    next = new TextPosition(cursor.Line, 0);
                    break;
                case MoveDirection.End:
                    next = new TextPosition(cursor.Line, _doc.GetLineLength(cursor.Line));
                    break;
                case MoveDirection.PageUp:
                    int upLine = Math.Max(0, cursor.Line - _config.PageScrollLines);
                    next = new TextPosition(upLine, Math.Min(cursor.Column, _doc.GetLineLength(upLine)));
                    break;
                case MoveDirection.PageDown:
                    int downLine = Math.Min(_doc.LineCount - 1, cursor.Line + _config.PageScrollLines);
                    next = new TextPosition(downLine, Math.Min(cursor.Column, _doc.GetLineLength(downLine)));
                    break;
                case MoveDirection.DocumentStart:
                    next = TextPosition.Zero;
                    break;
                case MoveDirection.DocumentEnd:
                    int last = _doc.LineCount - 1;
                    next = new TextPosition(last, _doc.GetLineLength(last));
                    break;
                default:
                    return;
            }

            // If there's a selection and we're not extending, collapse to the appropriate side
            if (!shift && _doc.HasSelection && !word)
            {
                var sel = _doc.SelectionRange;
                if (dir == MoveDirection.Left || dir == MoveDirection.Up || dir == MoveDirection.Home || dir == MoveDirection.DocumentStart)
                    next = sel.Start;
                else if (dir == MoveDirection.Right || dir == MoveDirection.Down || dir == MoveDirection.End || dir == MoveDirection.DocumentEnd)
                    next = sel.End;
            }

            _doc.SetCursor(next, extendSelection: shift);

            // Trim whitespace-only lines when cursor leaves them (VSCode behavior)
            if (next.Line != leavingLine && leavingLine < _doc.LineCount)
            {
                TrimWhitespaceOnlyLine(leavingLine);
            }

            _history.BreakBatch();
        }

        private void TrimWhitespaceOnlyLine(int line)
        {
            if (line < 0 || line >= _doc.LineCount) return;
            string lineText = _doc.GetLine(line);
            if (lineText.Length > 0 && lineText.TrimStart().Length == 0)
            {
                _doc.Delete(new TextRange(
                    new TextPosition(line, 0),
                    new TextPosition(line, lineText.Length)));
            }
        }

        public string GetSelectedText()
        {
            return _doc.HasSelection ? _doc.GetText(_doc.SelectionRange) : string.Empty;
        }

        public void Paste(string text)
        {
            TypeText(text);
        }

        public void Cut()
        {
            if (!_doc.HasSelection) return;
            // Caller should grab GetSelectedText() before calling Cut()
            _history.Execute(new DeleteTextCommand(_doc.SelectionRange), _doc);
        }

        public void SetText(string text)
        {
            _doc.SetText(text);
            _history.Clear();
        }

        public string GetText() => _doc.GetText();

        private TextPosition MoveLeft(TextPosition pos)
        {
            if (pos.Column > 0)
                return new TextPosition(pos.Line, pos.Column - 1);
            if (pos.Line > 0)
                return new TextPosition(pos.Line - 1, _doc.GetLineLength(pos.Line - 1));
            return pos;
        }

        private TextPosition MoveRight(TextPosition pos)
        {
            if (pos.Column < _doc.GetLineLength(pos.Line))
                return new TextPosition(pos.Line, pos.Column + 1);
            if (pos.Line < _doc.LineCount - 1)
                return new TextPosition(pos.Line + 1, 0);
            return pos;
        }

        private TextPosition MoveWordLeft(TextPosition pos)
        {
            if (pos.Column == 0 && pos.Line == 0)
                return pos;

            // Move to previous line end if at line start
            if (pos.Column == 0)
                return new TextPosition(pos.Line - 1, _doc.GetLineLength(pos.Line - 1));

            string line = _doc.GetLine(pos.Line);
            int col = pos.Column - 1;

            // Skip whitespace
            while (col > 0 && char.IsWhiteSpace(line[col]))
                col--;

            // Skip word characters
            if (col >= 0 && IsWordChar(line[col]))
            {
                while (col > 0 && IsWordChar(line[col - 1]))
                    col--;
            }
            else if (col >= 0)
            {
                // Skip non-word, non-whitespace (punctuation)
                while (col > 0 && !IsWordChar(line[col - 1]) && !char.IsWhiteSpace(line[col - 1]))
                    col--;
            }

            return new TextPosition(pos.Line, col);
        }

        private TextPosition MoveWordRight(TextPosition pos)
        {
            int lineLen = _doc.GetLineLength(pos.Line);
            if (pos.Column >= lineLen && pos.Line >= _doc.LineCount - 1)
                return pos;

            // Move to next line start if at line end
            if (pos.Column >= lineLen)
                return new TextPosition(pos.Line + 1, 0);

            string line = _doc.GetLine(pos.Line);
            int col = pos.Column;

            // Skip current word characters
            if (IsWordChar(line[col]))
            {
                while (col < lineLen && IsWordChar(line[col]))
                    col++;
            }
            else if (!char.IsWhiteSpace(line[col]))
            {
                while (col < lineLen && !IsWordChar(line[col]) && !char.IsWhiteSpace(line[col]))
                    col++;
            }

            // Skip whitespace
            while (col < lineLen && char.IsWhiteSpace(line[col]))
                col++;

            return new TextPosition(pos.Line, col);
        }

        internal static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        public TextRange GetWordBoundsAt(TextPosition pos)
        {
            var p = TextOperations.ClampPosition(_doc, pos);
            string line = _doc.GetLine(p.Line);

            if (line.Length == 0)
                return new TextRange(p, p);

            int col = Math.Min(p.Column, line.Length - 1);
            if (col < 0) col = 0;

            char ch = line[col];
            int start = col;
            int end = col;

            if (IsWordChar(ch))
            {
                while (start > 0 && IsWordChar(line[start - 1])) start--;
                while (end < line.Length - 1 && IsWordChar(line[end + 1])) end++;
            }
            else if (char.IsWhiteSpace(ch))
            {
                while (start > 0 && char.IsWhiteSpace(line[start - 1])) start--;
                while (end < line.Length - 1 && char.IsWhiteSpace(line[end + 1])) end++;
            }
            else
            {
                while (start > 0 && !IsWordChar(line[start - 1]) && !char.IsWhiteSpace(line[start - 1])) start--;
                while (end < line.Length - 1 && !IsWordChar(line[end + 1]) && !char.IsWhiteSpace(line[end + 1])) end++;
            }

            return new TextRange(
                new TextPosition(p.Line, start),
                new TextPosition(p.Line, end + 1));
        }
    }
}
