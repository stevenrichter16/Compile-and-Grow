using CodeEditor.Core;

namespace CodeEditor.Completion
{
    public interface ICompletionProvider
    {
        CompletionResult GetCompletions(DocumentModel doc, TextPosition cursor);
    }
}
