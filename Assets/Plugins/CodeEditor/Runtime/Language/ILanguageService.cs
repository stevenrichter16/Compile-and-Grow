using System.Collections.Generic;

namespace CodeEditor.Language
{
    public enum TokenCategory
    {
        Default,
        Keyword,
        String,
        Number,
        Comment,
        Operator,
        Type,
        Function,
        Variable,
        Error,
        Decorator,
    }

    public readonly struct HighlightToken
    {
        public readonly int StartColumn;
        public readonly int Length;
        public readonly TokenCategory Category;

        public HighlightToken(int startColumn, int length, TokenCategory category)
        {
            StartColumn = startColumn;
            Length = length;
            Category = category;
        }
    }

    public interface ILanguageService
    {
        bool ShouldIndentAfterLine(string lineText);
        IReadOnlyList<HighlightToken> TokenizeLine(string lineText, object lineState);
        object GetLineEndState(string lineText, object lineStartState);
    }
}
