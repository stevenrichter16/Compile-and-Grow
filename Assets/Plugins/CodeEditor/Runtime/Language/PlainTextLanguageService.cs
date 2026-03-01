using System.Collections.Generic;

namespace CodeEditor.Language
{
    public sealed class PlainTextLanguageService : ILanguageService
    {
        private static readonly IReadOnlyList<HighlightToken> EmptyTokens = new HighlightToken[0];

        public bool ShouldIndentAfterLine(string lineText) => false;
        public IReadOnlyList<HighlightToken> TokenizeLine(string lineText, object lineState) => EmptyTokens;
        public object GetLineEndState(string lineText, object lineStartState) => null;
    }
}
