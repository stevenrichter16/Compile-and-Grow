using System.Collections.Generic;

namespace GrowlLanguage.Lexer
{
    /// <summary>
    /// The complete output of a lex pass: a token stream and any diagnostics.
    /// Tokens always includes a terminal Eof even when errors are present.
    /// </summary>
    public sealed class LexResult
    {
        public readonly List<Token>    Tokens;
        public readonly List<LexError> Errors;

        public bool HasErrors => Errors.Count > 0;

        public LexResult(List<Token> tokens, List<LexError> errors)
        {
            Tokens = tokens;
            Errors = errors;
        }
    }
}
