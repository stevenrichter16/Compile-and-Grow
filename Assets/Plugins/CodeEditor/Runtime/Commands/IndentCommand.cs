using System;
using System.Collections.Generic;
using CodeEditor.Core;

namespace CodeEditor.Commands
{
    public sealed class IndentCommand : IEditCommand
    {
        private readonly int _firstLine;
        private readonly int _lastLine;
        private readonly int _indentSize;
        private readonly bool _outdent;
        private TextPosition _cursorBefore;
        private TextPosition _anchorBefore;
        private List<string> _linesBefore;

        public IndentCommand(int firstLine, int lastLine, int indentSize, bool outdent)
        {
            _firstLine = firstLine;
            _lastLine = lastLine;
            _indentSize = indentSize;
            _outdent = outdent;
        }

        public string Description => _outdent ? "Outdent" : "Indent";

        public void Execute(DocumentModel doc)
        {
            _cursorBefore = doc.Cursor;
            _anchorBefore = doc.SelectionAnchor;

            int first = Math.Max(0, _firstLine);
            int last = Math.Min(_lastLine, doc.LineCount - 1);

            _linesBefore = new List<string>(last - first + 1);
            for (int i = first; i <= last; i++)
                _linesBefore.Add(doc.GetLine(i));

            TextOperations.IndentLines(doc, first, last, _indentSize, _outdent);

            // Adjust cursor and anchor columns
            AdjustPosition(doc, ref _cursorBefore, first, last);
        }

        public void Undo(DocumentModel doc)
        {
            int first = Math.Max(0, _firstLine);
            int last = Math.Min(_lastLine, doc.LineCount - 1);

            for (int i = first; i <= last; i++)
            {
                string current = doc.GetLine(i);
                int idx = i - first;
                if (idx < _linesBefore.Count && current != _linesBefore[idx])
                {
                    doc.Delete(new TextRange(new TextPosition(i, 0), new TextPosition(i, current.Length)));
                    doc.Insert(new TextPosition(i, 0), _linesBefore[idx]);
                }
            }

            doc.SetSelection(_anchorBefore, _cursorBefore);
        }

        public bool TryMerge(IEditCommand next) => false;

        private void AdjustPosition(DocumentModel doc, ref TextPosition before, int first, int last)
        {
            // After indent/outdent, adjust cursor column for affected lines
            int cursorLine = before.Line;
            if (cursorLine >= first && cursorLine <= last)
            {
                int newLen = doc.GetLineLength(cursorLine);
                int col = Math.Min(Math.Max(0, before.Column + (_outdent ? -_indentSize : _indentSize)), newLen);
                doc.SetCursor(new TextPosition(cursorLine, col));
            }

            int anchorLine = _anchorBefore.Line;
            if (anchorLine >= first && anchorLine <= last)
            {
                int newLen = doc.GetLineLength(anchorLine);
                int ancCol = Math.Min(Math.Max(0, _anchorBefore.Column + (_outdent ? -_indentSize : _indentSize)), newLen);
                doc.SetSelection(new TextPosition(anchorLine, ancCol), doc.Cursor);
            }
        }
    }
}
