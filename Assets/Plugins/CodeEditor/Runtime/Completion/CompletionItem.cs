namespace CodeEditor.Completion
{
    public enum CompletionKind
    {
        Keyword,
        Function,
        Variable,
        Constant,
        Method,
        Property,
        Snippet,
    }

    public readonly struct CompletionItem
    {
        public readonly string Label;
        public readonly string InsertText;
        public readonly string Detail;
        public readonly CompletionKind Kind;

        public CompletionItem(string label, CompletionKind kind,
                              string detail = null, string insertText = null)
        {
            Label = label;
            Kind = kind;
            Detail = detail;
            InsertText = insertText ?? label;
        }
    }
}
