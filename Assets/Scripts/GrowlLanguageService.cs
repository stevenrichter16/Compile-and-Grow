using System.Collections.Generic;
using CodeEditor.Language;

/// <summary>
/// Lightweight per-line tokenizer for Growl syntax highlighting.
/// Implements ILanguageService for the CodeEditor package.
/// Does not need INDENT/DEDENT tracking — only produces highlight tokens.
/// </summary>
public sealed class GrowlLanguageService : ILanguageService
{
    // ── Line state for multiline constructs ─────────────────────────────

    public sealed class LineState
    {
        public enum Mode : byte { Normal, InBlockComment, InMultilineString }
        public readonly Mode CurrentMode;
        public readonly char StringQuote;

        public static readonly LineState Normal_ = new LineState(Mode.Normal, '\0');

        public LineState(Mode mode, char quote)
        {
            CurrentMode = mode;
            StringQuote = quote;
        }

        public override bool Equals(object obj)
        {
            if (obj is LineState other)
                return CurrentMode == other.CurrentMode && StringQuote == other.StringQuote;
            return false;
        }

        public override int GetHashCode()
        {
            return ((int)CurrentMode * 397) ^ StringQuote;
        }
    }

    // ── Keyword dictionaries ────────────────────────────────────────────

    private static readonly HashSet<string> s_keywords = new HashSet<string>
    {
        // Control flow
        "if", "elif", "else", "for", "in", "while", "loop",
        "break", "continue", "return", "yield",
        // Declarations
        "fn", "class", "struct", "enum", "trait", "mixin",
        "abstract", "static", "const", "type",
        "module", "import", "from", "as",
        // Pattern matching & error handling
        "match", "case", "try", "recover", "always",
        // Logical / OOP
        "and", "or", "not", "is",
        "self", "super", "cls",
        // Literal keywords
        "true", "false", "none",
    };

    private static readonly HashSet<string> s_biologicalKeywords = new HashSet<string>
    {
        "phase", "when", "then",
        "respond", "to",
        "adapt", "toward", "rate", "otherwise",
        "cycle", "at", "period",
        "ticker", "every",
        "wait", "defer", "until",
        "mutate", "by",
    };

    // ── Cached end state from last TokenizeLine call ────────────────────

    private LineState _lastEndState;
    private string _lastLineText;

    // ── ILanguageService implementation ─────────────────────────────────

    public bool ShouldIndentAfterLine(string lineText)
    {
        if (string.IsNullOrEmpty(lineText)) return false;
        string trimmed = lineText.TrimEnd();
        return trimmed.Length > 0 && trimmed[trimmed.Length - 1] == ':';
    }

    public IReadOnlyList<HighlightToken> TokenizeLine(string lineText, object lineState)
    {
        var state = (lineState as LineState) ?? LineState.Normal_;
        var tokens = ScanLine(lineText, state, out LineState endState);
        _lastEndState = endState;
        _lastLineText = lineText;
        return tokens;
    }

    public object GetLineEndState(string lineText, object lineStartState)
    {
        // If TokenizeLine was just called for this exact line, return cached result
        if (_lastEndState != null && ReferenceEquals(lineText, _lastLineText))
        {
            var result = _lastEndState;
            _lastEndState = null;
            _lastLineText = null;
            return result;
        }

        var state = (lineStartState as LineState) ?? LineState.Normal_;
        ScanLine(lineText, state, out LineState endState);
        return endState;
    }

    // ── Per-line scanner ────────────────────────────────────────────────

    private static List<HighlightToken> ScanLine(string line, LineState startState, out LineState endState)
    {
        var tokens = new List<HighlightToken>();
        int len = line.Length;
        int pos = 0;
        endState = LineState.Normal_;

        // ── Continue multiline block comment ────────────────────────────
        if (startState.CurrentMode == LineState.Mode.InBlockComment)
        {
            int closeIdx = FindBlockCommentClose(line, 0);
            if (closeIdx < 0)
            {
                // Entire line is still in block comment
                if (len > 0)
                    tokens.Add(new HighlightToken(0, len, TokenCategory.Comment));
                endState = startState;
                return tokens;
            }
            // Comment ends at closeIdx + 3
            int end = closeIdx + 3;
            tokens.Add(new HighlightToken(0, end, TokenCategory.Comment));
            pos = end;
            endState = LineState.Normal_;
        }

        // ── Continue multiline string ───────────────────────────────────
        if (startState.CurrentMode == LineState.Mode.InMultilineString)
        {
            char q = startState.StringQuote;
            int closeIdx = FindTripleQuoteClose(line, 0, q);
            if (closeIdx < 0)
            {
                if (len > 0)
                    tokens.Add(new HighlightToken(0, len, TokenCategory.String));
                endState = startState;
                return tokens;
            }
            int end = closeIdx + 3;
            tokens.Add(new HighlightToken(0, end, TokenCategory.String));
            pos = end;
            endState = LineState.Normal_;
        }

        // ── Scan remaining characters ───────────────────────────────────
        while (pos < len)
        {
            char c = line[pos];

            // Skip whitespace
            if (c == ' ' || c == '\t')
            {
                pos++;
                continue;
            }

            // ── # : comment or color literal ────────────────────────────
            if (c == '#')
            {
                // Color literal: # followed by exactly 6 hex digits
                if (IsColorLiteral(line, pos))
                {
                    tokens.Add(new HighlightToken(pos, 7, TokenCategory.Number));
                    pos += 7;
                    continue;
                }

                // Block comment: ###
                if (pos + 2 < len && line[pos + 1] == '#' && line[pos + 2] == '#')
                {
                    // Check if block comment closes on this line
                    int closeIdx = FindBlockCommentClose(line, pos + 3);
                    if (closeIdx < 0)
                    {
                        tokens.Add(new HighlightToken(pos, len - pos, TokenCategory.Comment));
                        endState = new LineState(LineState.Mode.InBlockComment, '\0');
                        return tokens;
                    }
                    int end = closeIdx + 3;
                    tokens.Add(new HighlightToken(pos, end - pos, TokenCategory.Comment));
                    pos = end;
                    continue;
                }

                // Doc comment (##) or warn comment (#!) or regular comment (#)
                // All run to end of line
                tokens.Add(new HighlightToken(pos, len - pos, TokenCategory.Comment));
                return tokens;
            }

            // ── String literals ─────────────────────────────────────────
            if (c == '"' || c == '\'')
            {
                // Check for raw string: r" or r'
                // (handled below when we see 'r' as identifier start)

                // Triple-quoted multiline string
                if (pos + 2 < len && line[pos + 1] == c && line[pos + 2] == c)
                {
                    int closeIdx = FindTripleQuoteClose(line, pos + 3, c);
                    if (closeIdx < 0)
                    {
                        tokens.Add(new HighlightToken(pos, len - pos, TokenCategory.String));
                        endState = new LineState(LineState.Mode.InMultilineString, c);
                        return tokens;
                    }
                    int end = closeIdx + 3;
                    tokens.Add(new HighlightToken(pos, end - pos, TokenCategory.String));
                    pos = end;
                    continue;
                }

                // Single-line string
                int strEnd = ScanSingleLineString(line, pos, c);
                tokens.Add(new HighlightToken(pos, strEnd - pos, TokenCategory.String));
                pos = strEnd;
                continue;
            }

            // ── Numbers ─────────────────────────────────────────────────
            if (IsDigit(c) || (c == '.' && pos + 1 < len && IsDigit(line[pos + 1])))
            {
                int numEnd = ScanNumber(line, pos);
                tokens.Add(new HighlightToken(pos, numEnd - pos, TokenCategory.Number));
                pos = numEnd;
                continue;
            }

            // ── Identifiers and keywords ────────────────────────────────
            if (IsIdentStart(c))
            {
                int idEnd = pos + 1;
                while (idEnd < len && IsIdentChar(line[idEnd]))
                    idEnd++;

                string word = line.Substring(pos, idEnd - pos);

                // Check for raw string: r"..." or r'...'
                if (word == "r" && idEnd < len && (line[idEnd] == '"' || line[idEnd] == '\''))
                {
                    char q = line[idEnd];
                    int strEnd = ScanSingleLineString(line, idEnd, q);
                    tokens.Add(new HighlightToken(pos, strEnd - pos, TokenCategory.String));
                    pos = strEnd;
                    continue;
                }

                // Classify the identifier
                TokenCategory cat;
                if (s_biologicalKeywords.Contains(word))
                    cat = TokenCategory.BiologicalKeyword;
                else if (s_keywords.Contains(word))
                    cat = TokenCategory.Keyword;
                else
                {
                    // Peek past whitespace — if '(' follows, it's a function call
                    int peek = idEnd;
                    while (peek < len && line[peek] == ' ')
                        peek++;
                    cat = (peek < len && line[peek] == '(')
                        ? TokenCategory.Function
                        : TokenCategory.Variable;
                }

                tokens.Add(new HighlightToken(pos, idEnd - pos, cat));

                pos = idEnd;
                continue;
            }

            // ── Decorator (@identifier) ─────────────────────────────────
            if (c == '@')
            {
                int start = pos;
                pos++; // skip @
                if (pos < len && IsIdentStart(line[pos]))
                {
                    while (pos < len && IsIdentChar(line[pos]))
                        pos++;
                }
                tokens.Add(new HighlightToken(start, pos - start, TokenCategory.Decorator));
                continue;
            }

            // ── Operators ───────────────────────────────────────────────
            if (IsOperatorChar(c))
            {
                int start = pos;
                // Consume multi-character operators
                pos++;
                while (pos < len && IsOperatorChar(line[pos]))
                    pos++;
                tokens.Add(new HighlightToken(start, pos - start, TokenCategory.Operator));
                continue;
            }

            // ── Everything else (brackets, commas, colons, dots) → skip
            pos++;
        }

        return tokens;
    }

    // ── Helper methods ──────────────────────────────────────────────────

    private static bool IsColorLiteral(string line, int hashPos)
    {
        // # followed by exactly 6 hex digits, then NOT another hex digit
        if (hashPos + 6 >= line.Length) return false;
        for (int i = 1; i <= 6; i++)
        {
            if (!IsHexDigit(line[hashPos + i])) return false;
        }
        if (hashPos + 7 < line.Length && IsHexDigit(line[hashPos + 7]))
            return false;
        return true;
    }

    private static int FindBlockCommentClose(string line, int startPos)
    {
        for (int i = startPos; i + 2 < line.Length; i++)
        {
            if (line[i] == '#' && line[i + 1] == '#' && line[i + 2] == '#')
                return i;
        }
        return -1;
    }

    private static int FindTripleQuoteClose(string line, int startPos, char quote)
    {
        for (int i = startPos; i + 2 < line.Length; i++)
        {
            if (line[i] == quote && line[i + 1] == quote && line[i + 2] == quote)
                return i;
        }
        return -1;
    }

    private static int ScanSingleLineString(string line, int pos, char quote)
    {
        int i = pos + 1; // skip opening quote
        while (i < line.Length)
        {
            if (line[i] == '\\')
            {
                i += 2; // skip escape
                continue;
            }
            if (line[i] == quote)
            {
                i++; // include closing quote
                return i;
            }
            i++;
        }
        return i; // unterminated string — color to end of line
    }

    private static int ScanNumber(string line, int pos)
    {
        int i = pos;
        int len = line.Length;

        // Hex: 0x...
        if (i + 1 < len && line[i] == '0' && (line[i + 1] == 'x' || line[i + 1] == 'X'))
        {
            i += 2;
            while (i < len && (IsHexDigit(line[i]) || line[i] == '_'))
                i++;
            return ScanUnitSuffix(line, i);
        }

        // Binary: 0b...
        if (i + 1 < len && line[i] == '0' && (line[i + 1] == 'b' || line[i + 1] == 'B'))
        {
            i += 2;
            while (i < len && (line[i] == '0' || line[i] == '1' || line[i] == '_'))
                i++;
            return ScanUnitSuffix(line, i);
        }

        // Decimal integer / float
        while (i < len && (IsDigit(line[i]) || line[i] == '_'))
            i++;

        // Decimal point
        if (i < len && line[i] == '.' && i + 1 < len && IsDigit(line[i + 1]))
        {
            i++; // skip .
            while (i < len && (IsDigit(line[i]) || line[i] == '_'))
                i++;
        }

        // Exponent
        if (i < len && (line[i] == 'e' || line[i] == 'E'))
        {
            i++;
            if (i < len && (line[i] == '+' || line[i] == '-'))
                i++;
            while (i < len && (IsDigit(line[i]) || line[i] == '_'))
                i++;
        }

        return ScanUnitSuffix(line, i);
    }

    private static int ScanUnitSuffix(string line, int pos)
    {
        // Unit suffixes: cm, kg, %, C, kW, s, etc.
        if (pos < line.Length && line[pos] == '%')
            return pos + 1;
        int i = pos;
        while (i < line.Length && IsIdentChar(line[i]) && !IsDigit(line[i]))
            i++;
        return i;
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsHexDigit(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    private static bool IsIdentStart(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    private static bool IsIdentChar(char c) => IsIdentStart(c) || IsDigit(c);

    private static bool IsOperatorChar(char c)
    {
        switch (c)
        {
            case '+': case '-': case '*': case '/': case '%':
            case '=': case '!': case '<': case '>':
            case '|': case '&': case '^': case '~': case '?':
                return true;
            default:
                return false;
        }
    }
}
