namespace GrowlLanguage.Lexer
{
    /// <summary>
    /// A single lexical token produced by the Growl lexer.
    /// Immutable value type — safe to copy freely.
    /// </summary>
    public readonly struct Token
    {
        /// <summary>The kind of token.</summary>
        public readonly TokenType Type;

        /// <summary>
        /// The processed text value of the token.
        /// For strings: escape sequences are NOT resolved here (raw source text between quotes).
        /// For numbers: digit-separator underscores stripped, prefix (0x / 0b) retained.
        /// For identifiers and keywords: the literal source text.
        /// For structural tokens (Indent, Dedent, Newline, Eof): empty string.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Unit suffix attached to a numeric literal, or null.
        /// Examples: "cm", "m", "kg", "g", "s", "C", "kW", "%", "h"
        /// The semantic conversion (e.g. 85% → 0.85) is handled by the parser/runtime.
        /// </summary>
        public readonly string Unit;

        /// <summary>1-based source line where this token starts.</summary>
        public readonly int Line;

        /// <summary>1-based source column where this token starts.</summary>
        public readonly int Column;

        public Token(TokenType type, string value, int line, int column, string unit = null)
        {
            Type   = type;
            Value  = value ?? string.Empty;
            Unit   = unit;
            Line   = line;
            Column = column;
        }

        // ── Convenience queries ──────────────────────────────────────────────────

        public bool Is(TokenType t)        => Type == t;
        public bool IsAny(TokenType a, TokenType b) => Type == a || Type == b;

        public bool IsLiteral =>
            Type == TokenType.Integer   || Type == TokenType.Float  ||
            Type == TokenType.String    || Type == TokenType.Color  ||
            Type == TokenType.RawString || Type == TokenType.MultilineString ||
            Type == TokenType.True      || Type == TokenType.False  ||
            Type == TokenType.None;

        public bool IsStructural =>
            Type == TokenType.Newline || Type == TokenType.Indent ||
            Type == TokenType.Dedent  || Type == TokenType.Eof;

        public bool IsAssignmentOp =>
            Type == TokenType.Equal             || Type == TokenType.PlusEqual     ||
            Type == TokenType.MinusEqual        || Type == TokenType.StarEqual     ||
            Type == TokenType.SlashEqual        || Type == TokenType.SlashSlashEqual ||
            Type == TokenType.PercentEqual      || Type == TokenType.StarStarEqual;

        // ── Sentinel values ──────────────────────────────────────────────────────

        public static readonly Token Eof  = new Token(TokenType.Eof,   string.Empty, 0, 0);
        public static readonly Token None = new Token(TokenType.Error, string.Empty, 0, 0);

        // ── Diagnostics ──────────────────────────────────────────────────────────

        public override string ToString()
        {
            string unitSuffix = Unit != null ? Unit : string.Empty;
            string display    = Value.Length > 40 ? Value.Substring(0, 37) + "..." : Value;
            return $"[{Type} '{display}{unitSuffix}' {Line}:{Column}]";
        }
    }
}
