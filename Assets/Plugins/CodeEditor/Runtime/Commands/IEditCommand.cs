using CodeEditor.Core;

namespace CodeEditor.Commands
{
    public interface IEditCommand
    {
        void Execute(DocumentModel doc);
        void Undo(DocumentModel doc);
        string Description { get; }
        bool TryMerge(IEditCommand next);
    }
}
