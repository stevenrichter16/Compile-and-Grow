using System.Collections.Generic;
using CodeEditor.Core;

namespace CodeEditor.Commands
{
    public sealed class CommandHistory
    {
        private readonly List<IEditCommand> _undoStack = new List<IEditCommand>();
        private readonly List<IEditCommand> _redoStack = new List<IEditCommand>();
        private bool _batchBroken;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Execute(IEditCommand command, DocumentModel doc)
        {
            command.Execute(doc);
            _redoStack.Clear();

            if (!_batchBroken && _undoStack.Count > 0)
            {
                var top = _undoStack[_undoStack.Count - 1];
                if (top.TryMerge(command))
                    return;
            }

            _undoStack.Add(command);
            _batchBroken = false;
        }

        public void Undo(DocumentModel doc)
        {
            if (_undoStack.Count == 0) return;
            int last = _undoStack.Count - 1;
            var cmd = _undoStack[last];
            _undoStack.RemoveAt(last);
            cmd.Undo(doc);
            _redoStack.Add(cmd);
            _batchBroken = true;
        }

        public void Redo(DocumentModel doc)
        {
            if (_redoStack.Count == 0) return;
            int last = _redoStack.Count - 1;
            var cmd = _redoStack[last];
            _redoStack.RemoveAt(last);
            cmd.Execute(doc);
            _undoStack.Add(cmd);
        }

        public void BreakBatch()
        {
            _batchBroken = true;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _batchBroken = false;
        }
    }
}
