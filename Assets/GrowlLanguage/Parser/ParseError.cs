namespace GrowlLanguage.Parser
{
    /// <summary>
    /// A non-fatal parser diagnostic. The parser attempts to recover and keep
    /// building as much AST as possible.
    /// </summary>
    public readonly struct ParseError
    {
        public readonly string Message;
        public readonly int Line;
        public readonly int Column;

        public ParseError(string message, int line, int column)
        {
            Message = message;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"[{Line}:{Column}] ParseError: {Message}";
    }
}
