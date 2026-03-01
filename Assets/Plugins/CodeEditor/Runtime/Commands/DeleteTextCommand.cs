using CodeEditor.Core;

namespace CodeEditor.Commands
{
    public sealed class DeleteTextCommand : IEditCommand
    {
        private readonly TextRange _range;
        private string _deletedText;
        private TextPosition _cursorBefore;
        private TextPosition _anchorBefore;

        public DeleteTextCommand(TextRange range)
        {
            _range = range;
        }

        public string Description => "Delete text";

        public void Execute(DocumentModel doc)
        {
            _cursorBefore = doc.Cursor;
            _anchorBefore = doc.SelectionAnchor;
            _deletedText = doc.GetText(_range);
            doc.Delete(_range);
            doc.SetCursor(_range.Start);
        }

        public void Undo(DocumentModel doc)
        {
            doc.Insert(_range.Start, _deletedText);
            doc.SetSelection(_anchorBefore, _cursorBefore);
        }

        public bool TryMerge(IEditCommand next)
        {
            // Delete commands don't merge — each backspace/delete is its own undo step
            // (matching VSCode behavior for individual deletes)
            return false;
        }
    }
}
