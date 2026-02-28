namespace GrowlLanguage.Lexer
{
    /// <summary>
    /// A non-fatal lexer diagnostic.  The lexer always attempts to recover and
    /// produce a complete token stream even in the presence of errors.
    /// </summary>
    public readonly struct LexError
    {
        public readonly string Message;
        public readonly int    Line;
        public readonly int    Column;

        public LexError(string message, int line, int column)
        {
            Message = message;
            Line    = line;
            Column  = column;
        }

        public override string ToString() => $"[{Line}:{Column}] LexError: {Message}";
    }
}
