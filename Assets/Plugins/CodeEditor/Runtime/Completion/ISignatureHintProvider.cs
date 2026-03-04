using CodeEditor.Core;

namespace CodeEditor.Completion
{
    public interface ISignatureHintProvider
    {
        /// <summary>
        /// Returns the signature hint for the function call surrounding the cursor,
        /// or null if the cursor is not inside a function call.
        /// </summary>
        SignatureHint GetSignatureHint(DocumentModel doc, TextPosition cursor, out int activeParameter);
    }
}
