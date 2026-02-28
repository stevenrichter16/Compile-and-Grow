namespace GrowlLanguage.Lexer
{
    public enum TokenType
    {
        // ── Literals ──────────────────────────────────────────────────────────────
        Integer,            // 42  0xFF  0b1010  1_000_000
        Float,              // 3.14  6.022e23  1.5e-4
        String,             // "text"  'text'  (no interpolation)
        MultilineString,    // """..."""                (no interpolation)
        RawString,          // r"..."  r'...'          (no escapes, no interpolation)
        Color,              // #RRGGBB

        // String interpolation token sequence
        // "text ${expr} more"  →  InterpStrStart, InterpStrText, InterpStart,
        //                         <expr tokens>, InterpEnd, InterpStrText, InterpStrEnd
        InterpStrStart,     // opening quote of an interpolated string
        InterpStrText,      // literal text segment within an interpolated string
        InterpStart,        // ${
        InterpEnd,          // } closing an interpolation
        InterpStrEnd,       // closing quote of an interpolated string

        True, False, None,

        // ── Identifier ────────────────────────────────────────────────────────────
        Identifier,

        // ── Keywords — Control Flow ───────────────────────────────────────────────
        If, Elif, Else,
        For, In, While, Loop,
        Break, Continue, Return, Yield,

        // ── Keywords — Declarations ───────────────────────────────────────────────
        Fn, Class, Struct, Enum, Trait, Mixin,
        Abstract, Static,
        Const, Type,
        Module, Import, From, As,

        // ── Keywords — Pattern Matching & Error Handling ──────────────────────────
        Match, Case,
        Try, Recover, Always,

        // ── Keywords — Operators / OOP ────────────────────────────────────────────
        And, Or, Not, Is,
        Self, Super, Cls,

        // ── Biological Keywords ───────────────────────────────────────────────────
        Phase, When, Then,
        Respond, To,
        Adapt, Toward, Rate, Otherwise,
        Cycle, CycleAt, Period,
        Ticker, Every,
        Wait, Defer, Until,
        Mutate, By,

        // ── Arithmetic Operators ──────────────────────────────────────────────────
        Plus,           // +
        Minus,          // -
        Star,           // *
        Slash,          // /
        SlashSlash,     // //
        Percent,        // %
        StarStar,       // **  (exponentiation; dot product when applied to vectors)

        // ── Bitwise / Vector Operators ────────────────────────────────────────────
        Ampersand,      // &    bitwise AND / set intersection
        Pipe,           // |    bitwise OR / set union / magnitude delimiter
        Caret,          // ^    bitwise XOR / set sym-diff / normalize unary
        Tilde,          // ~    bitwise NOT
        LessLess,       // <<   left shift
        GreaterGreater, // >>   right shift
        CaretCaret,     // ^^   3-D cross product

        // ── Comparison Operators ──────────────────────────────────────────────────
        EqualEqual,     // ==
        BangEqual,      // !=
        Less,           // <
        Greater,        // >
        LessEqual,      // <=
        GreaterEqual,   // >=

        // ── Assignment Operators ──────────────────────────────────────────────────
        Equal,              // =
        PlusEqual,          // +=
        MinusEqual,         // -=
        StarEqual,          // *=
        SlashEqual,         // /=
        SlashSlashEqual,    // //=
        PercentEqual,       // %=
        StarStarEqual,      // **=

        // ── Special Operators ─────────────────────────────────────────────────────
        PipeGreater,        // |>   pipeline
        QuestionQuestion,   // ??   nullish coalescing
        Arrow,              // ->   return-type annotation / lambda body

        // ── Range / Spread ────────────────────────────────────────────────────────
        DotDot,             // ..   exclusive range
        DotDotEqual,        // ..=  inclusive range
        DotDotDot,          // ...  spread

        // ── Delimiters ────────────────────────────────────────────────────────────
        LeftParen,      // (
        RightParen,     // )
        LeftBracket,    // [
        RightBracket,   // ]
        LeftBrace,      // {
        RightBrace,     // }
        Dot,            // .
        Comma,          // ,
        Colon,          // :
        At,             // @   decorator prefix

        // ── Structural Tokens ─────────────────────────────────────────────────────
        Newline,
        Indent,
        Dedent,
        Eof,

        // ── Comment Tokens (most consumed; these carry semantic meaning) ───────────
        DocComment,         // ##  text   — attached to next declaration
        WarnComment,        // #!  text   — shown in genome analysis panel

        // ── Error ─────────────────────────────────────────────────────────────────
        Error,
    }
}
