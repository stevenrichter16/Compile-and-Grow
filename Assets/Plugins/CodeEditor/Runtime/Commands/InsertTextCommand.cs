using System;
using CodeEditor.Core;

namespace CodeEditor.Commands
{
    public sealed class InsertTextCommand : IEditCommand
    {
        private TextPosition _position;
        private string _text;
        private TextRange _selectionToDelete;
        private string _deletedText;
        private TextPosition _cursorBefore;
        private TextPosition _cursorAfter;
        private TextPosition _anchorBefore;

        public InsertTextCommand(TextPosition position, string text, TextRange selectionToDelete = default)
        {
            _position = position;
            _text = text;
            _selectionToDelete = selectionToDelete;
        }

        public string Description => "Insert text";

        public void Execute(DocumentModel doc)
        {
            _cursorBefore = doc.Cursor;
            _anchorBefore = doc.SelectionAnchor;

            if (!_selectionToDelete.IsEmpty)
            {
                _deletedText = doc.GetText(_selectionToDelete);
                doc.Delete(_selectionToDelete);
                _position = _selectionToDelete.Start;
            }

            doc.Insert(_position, _text);

            // Compute cursor after insert
            string[] inserted = _text.Split('\n');
            if (inserted.Length == 1)
            {
                _cursorAfter = new TextPosition(_position.Line, _position.Column + _text.Length);
            }
            else
            {
                _cursorAfter = new TextPosition(
                    _position.Line + inserted.Length - 1,
                    inserted[inserted.Length - 1].Length);
            }

            doc.SetCursor(_cursorAfter);
        }

        public void Undo(DocumentModel doc)
        {
            // Remove inserted text
            string[] inserted = _text.Split('\n');
            TextPosition insertEnd;
            if (inserted.Length == 1)
            {
                insertEnd = new TextPosition(_position.Line, _position.Column + _text.Length);
            }
            else
            {
                insertEnd = new TextPosition(
                    _position.Line + inserted.Length - 1,
                    inserted[inserted.Length - 1].Length);
            }
            doc.Delete(new TextRange(_position, insertEnd));

            // Restore deleted selection text
            if (_deletedText != null)
                doc.Insert(_selectionToDelete.Start, _deletedText);

            doc.SetSelection(_anchorBefore, _cursorBefore);
        }

        public bool TryMerge(IEditCommand next)
        {
            if (!(next is InsertTextCommand other)) return false;
            if (other._text.Length != 1) return false;
            if (!other._selectionToDelete.IsEmpty) return false;

            // Only merge consecutive single-char inserts at the expected position
            if (!other._position.Equals(_cursorAfter)) return false;

            // Break batch on whitespace after non-whitespace
            bool thisEndsWithWhitespace = _text.Length > 0 && char.IsWhiteSpace(_text[_text.Length - 1]);
            bool nextIsWhitespace = char.IsWhiteSpace(other._text[0]);
            if (!thisEndsWithWhitespace && nextIsWhitespace) return false;

            _text += other._text;
            _cursorAfter = new TextPosition(_cursorAfter.Line, _cursorAfter.Column + 1);
            if (other._text[0] == '\n')
                _cursorAfter = new TextPosition(_cursorAfter.Line + 1, 0);

            return true;
        }
    }
}
