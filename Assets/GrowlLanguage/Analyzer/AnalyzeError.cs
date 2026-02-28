namespace GrowlLanguage.Analyzer
{
    public enum AnalyzeErrorCode
    {
        ParseError,
        UnresolvedName,
        DuplicateSymbol,
        TypeMismatch,
        InvalidBinaryOperands,
        NonExhaustiveMatch,
        UnreachableCase,
        DuplicateMatchCase,
        ReadBeforeAssignment,
    }

    /// <summary>
    /// A semantic-analysis diagnostic.
    /// </summary>
    public readonly struct AnalyzeError
    {
        public readonly AnalyzeErrorCode Code;
        public readonly string Message;
        public readonly int Line;
        public readonly int Column;

        public AnalyzeError(AnalyzeErrorCode code, string message, int line, int column)
        {
            Code = code;
            Message = message;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"[{Line}:{Column}] {Code}: {Message}";
    }
}
