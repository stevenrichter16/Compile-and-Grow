using System.Collections.Generic;
using System.Text;

namespace GrowlLanguage.Lexer
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Growl Lexer
    //
    // Converts Growl source text into a flat List<Token>.
    //
    // Key behaviours:
    //   • Python-style INDENT / DEDENT from leading-space indentation (tabs → error).
    //   • NEWLINE tokens emitted at logical line ends; suppressed inside ( ) [ ] { }.
    //   • String interpolation:  "text ${expr} more"
    //       → InterpStrStart, InterpStrText, InterpStart, <expr tokens>,
    //         InterpEnd, InterpStrText, InterpStrEnd
    //   • Color literals:  #RRGGBB  (exactly 6 hex digits immediately after #).
    //   • Unit-suffixed numbers: 5.0cm  85%  22.5C  12kW  (stored in Token.Unit).
    //   • Three comment styles:
    //       #  regular   — consumed, no token
    //       ## doc       — emitted as DocComment
    //       #! warning   — emitted as WarnComment
    //       ### block ### — consumed, no token
    //   • Raw strings:  r"..."  r'...'  (no escapes, no interpolation).
    //   • Multi-line strings:  """..."""  (interpolation supported).
    // ─────────────────────────────────────────────────────────────────────────────
    public sealed class Lexer
    {
        // ── Source ───────────────────────────────────────────────────────────────
        private readonly string _src;       // normalised (only \n line endings)
        private          int    _pos;       // byte cursor into _src
        private          int    _line;      // 1-based current line
        private          int    _col;       // 1-based current column
        private          int    _lineStart; // _pos value at the start of _line

        // ── Output ───────────────────────────────────────────────────────────────
        private readonly List<Token>    _tokens = new List<Token>(256);
        private readonly List<LexError> _errors = new List<LexError>();

        // ── INDENT / DEDENT state ────────────────────────────────────────────────
        // Stack of indentation levels (column counts).  Bottom is always 0.
        private readonly Stack<int> _indentStack = new Stack<int>();
        private          bool       _atLineStart  = true;

        // ── Bracket depth (suppresses structural tokens inside brackets) ─────────
        private int _bracketDepth;

        // ── Interpolation mode (child lexer for ${ ... } content) ────────────────
        // When true: no INDENT/DEDENT emitted, first EOF terminates the run.
        private readonly bool _interpMode;

        // ─────────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Lex <paramref name="source"/> and return a token stream plus any diagnostics.</summary>
        public static LexResult Lex(string source) => new Lexer(source, interpMode: false).Run();

        // ─────────────────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────────────────

        private Lexer(string source, bool interpMode)
        {
            // Normalise line endings once up-front.
            _src = (source ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace("\r",   "\n");

            _pos       = 0;
            _line      = 1;
            _col       = 1;
            _lineStart = 0;
            _interpMode = interpMode;
            _indentStack.Push(0);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Main scan loop
        // ─────────────────────────────────────────────────────────────────────────

        private LexResult Run()
        {
            while (!AtEnd())
            {
                // ── Handle indentation at the start of a logical line ─────────
                if (_atLineStart && !_interpMode && _bracketDepth == 0)
                {
                    ProcessIndentation();
                    // ProcessIndentation may have skipped a blank / comment line
                    // and kept _atLineStart = true — loop again.
                    if (_atLineStart) continue;
                }

                // ── Skip horizontal whitespace ────────────────────────────────
                SkipSpaces();
                if (AtEnd()) break;

                char c = Current();

                // ── Newline ───────────────────────────────────────────────────
                if (c == '\n')
                {
                    Advance();
                    if (!_interpMode && _bracketDepth == 0)
                    {
                        // Coalesce: don't emit two consecutive Newlines.
                        if (_tokens.Count == 0 || _tokens[_tokens.Count - 1].Type != TokenType.Newline)
                            Emit(TokenType.Newline, "\n");
                    }
                    _atLineStart = true;
                    continue;
                }

                // ── Comment or color literal ──────────────────────────────────
                if (c == '#')
                {
                    if (IsColorLiteralAhead())
                        ScanColorLiteral();
                    else
                        ScanComment();
                    continue;
                }

                // ── Ordinary token ────────────────────────────────────────────
                ScanToken();
            }

            // ── Close any still-open indent blocks ────────────────────────────
            if (!_interpMode)
            {
                while (_indentStack.Count > 1)
                {
                    _indentStack.Pop();
                    Emit(TokenType.Dedent, "");
                }
                // Ensure token stream ends with Newline before Eof.
                if (_tokens.Count > 0 && _tokens[_tokens.Count - 1].Type != TokenType.Newline)
                    Emit(TokenType.Newline, "\n");
            }

            Emit(TokenType.Eof, "");
            return new LexResult(_tokens, _errors);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Indentation
        // ─────────────────────────────────────────────────────────────────────────

        private void ProcessIndentation()
        {
            int savedLine = _line;
            int savedCol  = _col;
            int indent    = 0;

            // Count leading spaces, reject tabs.
            while (!AtEnd() && Current() == ' ')
            {
                indent++;
                Advance();
            }

            if (!AtEnd() && Current() == '\t')
            {
                AddError("Tabs are not allowed — use 4 spaces per indent level", savedLine, savedCol);
                // Recovery: treat each tab as 4 spaces.
                while (!AtEnd() && (Current() == '\t' || Current() == ' '))
                {
                    indent += Current() == '\t' ? 4 : 1;
                    Advance();
                }
            }

            // Blank line: only whitespace, then newline or EOF.
            if (AtEnd() || Current() == '\n')
            {
                if (!AtEnd()) Advance(); // consume '\n' so Run() doesn't loop forever
                return; // keep _atLineStart = true
            }

            // Comment-only line — don't emit INDENT/DEDENT, but leave the '#'
            // unconsumed so Run() can route it to ScanComment(), which properly
            // distinguishes ## DocComment, #! WarnComment, ### block, and # regular.
            if (Current() == '#' && !IsColorLiteralAhead())
            {
                _atLineStart = false;
                return;
            }

            // Real content — resolve INDENT / DEDENT.
            _atLineStart = false;
            int top = _indentStack.Peek();

            if (indent > top)
            {
                _indentStack.Push(indent);
                Emit(TokenType.Indent, "", savedLine, savedCol);
            }
            else if (indent < top)
            {
                while (_indentStack.Count > 1 && _indentStack.Peek() > indent)
                {
                    _indentStack.Pop();
                    Emit(TokenType.Dedent, "", savedLine, savedCol);
                }
                if (_indentStack.Peek() != indent)
                    AddError($"Inconsistent indentation: {indent} spaces does not match any open block",
                             savedLine, savedCol);
            }
            // Equal indent: no structural token needed.
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Comments
        // ─────────────────────────────────────────────────────────────────────────

        private void ScanComment()
        {
            int startLine = _line, startCol = _col;
            Advance(); // consume first '#'

            // Block comment: ### ... ###
            if (!AtEnd() && Current() == '#' &&
                _pos + 1 < _src.Length && _src[_pos + 1] == '#')
            {
                Advance(); Advance(); // consume ##  (total: consumed ###)
                while (!AtEnd())
                {
                    if (Current() == '#' &&
                        _pos + 1 < _src.Length && _src[_pos + 1] == '#' &&
                        _pos + 2 < _src.Length && _src[_pos + 2] == '#')
                    {
                        Advance(); Advance(); Advance(); // closing ###
                        return;
                    }
                    Advance(); // also handles '\n' via Advance()
                }
                AddError("Unterminated block comment", startLine, startCol);
                return;
            }

            // Doc comment: ## text
            if (!AtEnd() && Current() == '#')
            {
                Advance(); // consume second '#'
                SkipSpaces();
                string text = ReadToEndOfLine();
                _tokens.Add(new Token(TokenType.DocComment, text, startLine, startCol));
                return;
            }

            // Warning comment: #! text
            if (!AtEnd() && Current() == '!')
            {
                Advance(); // consume '!'
                SkipSpaces();
                string text = ReadToEndOfLine();
                _tokens.Add(new Token(TokenType.WarnComment, text, startLine, startCol));
                return;
            }

            // Regular comment: # text — consume silently.
            SkipToEndOfLine();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Color literals
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the '#' at Current() is followed by exactly 6 hex digits.
        /// This distinguishes  #FF0000  (color) from  # comment  (comment).
        /// </summary>
        private bool IsColorLiteralAhead()
        {
            // _src[_pos] == '#'  (already checked by caller)
            if (_pos + 6 >= _src.Length) return false;
            for (int i = 1; i <= 6; i++)
            {
                if (!IsHexDigit(_src[_pos + i])) return false;
            }
            // The 7th character must not be a hex digit (no 7+ digit colors).
            char after = _pos + 7 < _src.Length ? _src[_pos + 7] : '\0';
            return !IsHexDigit(after);
        }

        private void ScanColorLiteral()
        {
            int startLine = _line, startCol = _col;
            Advance(); // consume '#'

            var sb = new StringBuilder(6);
            for (int i = 0; i < 6; i++)
                sb.Append(Advance());

            _tokens.Add(new Token(TokenType.Color, sb.ToString().ToUpperInvariant(),
                                  startLine, startCol));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Main token dispatcher
        // ─────────────────────────────────────────────────────────────────────────

        private void ScanToken()
        {
            int startLine = _line, startCol = _col;
            char c = Advance();

            switch (c)
            {
                // ── Brackets ──────────────────────────────────────────────────
                case '(':
                    _bracketDepth++;
                    Emit(TokenType.LeftParen, "(", startLine, startCol); break;
                case ')':
                    _bracketDepth = _bracketDepth > 0 ? _bracketDepth - 1 : 0;
                    Emit(TokenType.RightParen, ")", startLine, startCol); break;
                case '[':
                    _bracketDepth++;
                    Emit(TokenType.LeftBracket, "[", startLine, startCol); break;
                case ']':
                    _bracketDepth = _bracketDepth > 0 ? _bracketDepth - 1 : 0;
                    Emit(TokenType.RightBracket, "]", startLine, startCol); break;
                case '{':
                    _bracketDepth++;
                    Emit(TokenType.LeftBrace, "{", startLine, startCol); break;
                case '}':
                    _bracketDepth = _bracketDepth > 0 ? _bracketDepth - 1 : 0;
                    Emit(TokenType.RightBrace, "}", startLine, startCol); break;

                // ── Single-character punctuation ───────────────────────────────
                case ',': Emit(TokenType.Comma, ",", startLine, startCol); break;
                case ':': Emit(TokenType.Colon, ":", startLine, startCol); break;
                case '@': Emit(TokenType.At,    "@", startLine, startCol); break;
                case '~': Emit(TokenType.Tilde, "~", startLine, startCol); break;

                // ── Dot / range / spread ──────────────────────────────────────
                case '.':
                    if (Match('.'))
                    {
                        if (Match('='))      Emit(TokenType.DotDotEqual, "..=", startLine, startCol);
                        else if (Match('.')) Emit(TokenType.DotDotDot,   "...", startLine, startCol);
                        else                 Emit(TokenType.DotDot,      "..",  startLine, startCol);
                    }
                    else
                    {
                        Emit(TokenType.Dot, ".", startLine, startCol);
                    }
                    break;

                // ── Arithmetic ────────────────────────────────────────────────
                case '+':
                    if (Match('=')) Emit(TokenType.PlusEqual, "+=", startLine, startCol);
                    else             Emit(TokenType.Plus,      "+",  startLine, startCol);
                    break;

                case '-':
                    if      (Match('=')) Emit(TokenType.MinusEqual, "-=", startLine, startCol);
                    else if (Match('>')) Emit(TokenType.Arrow,       "->", startLine, startCol);
                    else                 Emit(TokenType.Minus,        "-",  startLine, startCol);
                    break;

                case '*':
                    if (Match('*'))
                    {
                        if (Match('=')) Emit(TokenType.StarStarEqual, "**=", startLine, startCol);
                        else             Emit(TokenType.StarStar,      "**",  startLine, startCol);
                    }
                    else if (Match('=')) Emit(TokenType.StarEqual, "*=", startLine, startCol);
                    else                  Emit(TokenType.Star,      "*",  startLine, startCol);
                    break;

                case '/':
                    if (Match('/'))
                    {
                        if (Match('=')) Emit(TokenType.SlashSlashEqual, "//=", startLine, startCol);
                        else             Emit(TokenType.SlashSlash,      "//",  startLine, startCol);
                    }
                    else if (Match('=')) Emit(TokenType.SlashEqual, "/=", startLine, startCol);
                    else                  Emit(TokenType.Slash,      "/",  startLine, startCol);
                    break;

                case '%':
                    if (Match('=')) Emit(TokenType.PercentEqual, "%=", startLine, startCol);
                    else             Emit(TokenType.Percent,      "%",  startLine, startCol);
                    break;

                // ── Comparison & assignment ────────────────────────────────────
                case '=':
                    if (Match('=')) Emit(TokenType.EqualEqual, "==", startLine, startCol);
                    else             Emit(TokenType.Equal,       "=",  startLine, startCol);
                    break;

                case '!':
                    if (Match('=')) Emit(TokenType.BangEqual, "!=", startLine, startCol);
                    else AddError("Unexpected character '!' (did you mean '!='?)", startLine, startCol);
                    break;

                case '<':
                    if      (Match('<')) Emit(TokenType.LessLess,    "<<", startLine, startCol);
                    else if (Match('=')) Emit(TokenType.LessEqual,   "<=", startLine, startCol);
                    else                  Emit(TokenType.Less,         "<",  startLine, startCol);
                    break;

                case '>':
                    if      (Match('>')) Emit(TokenType.GreaterGreater, ">>", startLine, startCol);
                    else if (Match('=')) Emit(TokenType.GreaterEqual,   ">=", startLine, startCol);
                    else                  Emit(TokenType.Greater,         ">",  startLine, startCol);
                    break;

                // ── Bitwise / special ──────────────────────────────────────────
                case '&': Emit(TokenType.Ampersand, "&", startLine, startCol); break;

                case '|':
                    if (Match('>')) Emit(TokenType.PipeGreater, "|>", startLine, startCol);
                    else             Emit(TokenType.Pipe,         "|",  startLine, startCol);
                    break;

                case '^':
                    if (Match('^')) Emit(TokenType.CaretCaret, "^^", startLine, startCol);
                    else             Emit(TokenType.Caret,       "^",  startLine, startCol);
                    break;

                case '?':
                    if (Match('?')) Emit(TokenType.QuestionQuestion, "??", startLine, startCol);
                    else AddError("Unexpected character '?' (did you mean '??'?)", startLine, startCol);
                    break;

                // ── Strings ───────────────────────────────────────────────────
                case '"':
                case '\'':
                    ScanString(c, startLine, startCol);
                    break;

                // ── Numeric literal ────────────────────────────────────────────
                default:
                    if (IsDigit(c))
                    {
                        ScanNumber(c, startLine, startCol);
                    }
                    else if (IsLetter(c) || c == '_')
                    {
                        ScanIdentifierOrKeyword(c, startLine, startCol);
                    }
                    else
                    {
                        AddError($"Unexpected character '{c}' (U+{(int)c:X4})", startLine, startCol);
                    }
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Numbers
        // ─────────────────────────────────────────────────────────────────────────

        private void ScanNumber(char first, int startLine, int startCol)
        {
            var sb = new StringBuilder();
            sb.Append(first);

            // Hex literal: 0x...
            if (first == '0' && !AtEnd() && (Current() == 'x' || Current() == 'X'))
            {
                sb.Append(Advance()); // x / X
                while (!AtEnd() && (IsHexDigit(Current()) || Current() == '_'))
                    sb.Append(Advance());

                string hexVal = sb.ToString();
                _tokens.Add(new Token(TokenType.Integer, hexVal, startLine, startCol));
                return;
            }

            // Binary literal: 0b...
            if (first == '0' && !AtEnd() && (Current() == 'b' || Current() == 'B'))
            {
                sb.Append(Advance()); // b / B
                while (!AtEnd() && (Current() == '0' || Current() == '1' || Current() == '_'))
                    sb.Append(Advance());

                string binVal = sb.ToString();
                _tokens.Add(new Token(TokenType.Integer, binVal, startLine, startCol));
                return;
            }

            // Decimal integer digits (with underscores as visual separators)
            while (!AtEnd() && (IsDigit(Current()) || Current() == '_'))
                sb.Append(Advance());

            bool isFloat = false;

            // Decimal point — but not the start of a range (..)
            if (!AtEnd() && Current() == '.' &&
                !(_pos + 1 < _src.Length && _src[_pos + 1] == '.'))
            {
                isFloat = true;
                sb.Append(Advance()); // '.'
                while (!AtEnd() && (IsDigit(Current()) || Current() == '_'))
                    sb.Append(Advance());
            }

            // Exponent: e / E  [+/-]  digits
            if (!AtEnd() && (Current() == 'e' || Current() == 'E'))
            {
                isFloat = true;
                sb.Append(Advance()); // e / E
                if (!AtEnd() && (Current() == '+' || Current() == '-'))
                    sb.Append(Advance());
                while (!AtEnd() && IsDigit(Current()))
                    sb.Append(Advance());
            }

            // Strip underscores from the stored value.
            string numValue = sb.ToString().Replace("_", "");

            // Optional unit suffix.
            string unit = TryScanUnitSuffix();

            TokenType numType = isFloat ? TokenType.Float : TokenType.Integer;
            _tokens.Add(new Token(numType, numValue, startLine, startCol, unit));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Unit suffixes
        // ─────────────────────────────────────────────────────────────────────────

        // Ordered so that two-character suffixes are tried before one-character.
        private static readonly string[] TwoCharUnits = { "cm", "kg", "kW" };
        private static readonly string[] OneCharUnits = { "m", "g", "s", "C", "h" };

        private string TryScanUnitSuffix()
        {
            // Percent — single character.
            if (!AtEnd() && Current() == '%')
            {
                Advance();
                return "%";
            }

            // Two-character suffixes.
            if (_pos + 1 < _src.Length)
            {
                char a = _src[_pos], b = _src[_pos + 1];
                foreach (string u in TwoCharUnits)
                {
                    if (u[0] == a && u[1] == b)
                    {
                        char after = _pos + 2 < _src.Length ? _src[_pos + 2] : '\0';
                        if (!IsLetterOrDigit(after) && after != '_')
                        {
                            Advance(); Advance();
                            return u;
                        }
                    }
                }
            }

            // One-character suffixes.
            if (!AtEnd())
            {
                char a = Current();
                foreach (string u in OneCharUnits)
                {
                    if (u[0] == a)
                    {
                        char after = _pos + 1 < _src.Length ? _src[_pos + 1] : '\0';
                        if (!IsLetterOrDigit(after) && after != '_')
                        {
                            Advance();
                            return u;
                        }
                    }
                }
            }

            return null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Identifiers and keywords
        // ─────────────────────────────────────────────────────────────────────────

        private void ScanIdentifierOrKeyword(char first, int startLine, int startCol)
        {
            var sb = new StringBuilder();
            sb.Append(first);

            while (!AtEnd() && (IsLetterOrDigit(Current()) || Current() == '_'))
                sb.Append(Advance());

            string text = sb.ToString();

            // Raw-string prefix: r followed immediately by a quote.
            if (text == "r" && !AtEnd() && (Current() == '"' || Current() == '\''))
            {
                char quote = Advance();
                ScanRawString(quote, startLine, startCol);
                return;
            }

            // Keyword lookup.
            if (s_keywords.TryGetValue(text, out TokenType kwType))
            {
                _tokens.Add(new Token(kwType, text, startLine, startCol));
            }
            else
            {
                _tokens.Add(new Token(TokenType.Identifier, text, startLine, startCol));
            }
        }

        private static readonly Dictionary<string, TokenType> s_keywords =
            new Dictionary<string, TokenType>
        {
            // Literals
            { "true",       TokenType.True       },
            { "false",      TokenType.False      },
            { "none",       TokenType.None       },

            // Control flow
            { "if",         TokenType.If         },
            { "elif",       TokenType.Elif       },
            { "else",       TokenType.Else       },
            { "for",        TokenType.For        },
            { "in",         TokenType.In         },
            { "while",      TokenType.While      },
            { "loop",       TokenType.Loop       },
            { "break",      TokenType.Break      },
            { "continue",   TokenType.Continue   },
            { "return",     TokenType.Return     },
            { "yield",      TokenType.Yield      },

            // Declarations
            { "fn",         TokenType.Fn         },
            { "class",      TokenType.Class      },
            { "struct",     TokenType.Struct     },
            { "enum",       TokenType.Enum       },
            { "trait",      TokenType.Trait      },
            { "mixin",      TokenType.Mixin      },
            { "abstract",   TokenType.Abstract   },
            { "static",     TokenType.Static     },
            { "const",      TokenType.Const      },
            { "type",       TokenType.Type       },
            { "module",     TokenType.Module     },
            { "import",     TokenType.Import     },
            { "from",       TokenType.From       },
            { "as",         TokenType.As         },

            // Pattern matching & error handling
            { "match",      TokenType.Match      },
            { "case",       TokenType.Case       },
            { "try",        TokenType.Try        },
            { "recover",    TokenType.Recover    },
            { "always",     TokenType.Always     },

            // Logical / OOP
            { "and",        TokenType.And        },
            { "or",         TokenType.Or         },
            { "not",        TokenType.Not        },
            { "is",         TokenType.Is         },
            { "self",       TokenType.Self       },
            { "super",      TokenType.Super      },
            { "cls",        TokenType.Cls        },

            // Biological
            { "phase",      TokenType.Phase      },
            { "when",       TokenType.When       },
            { "then",       TokenType.Then       },
            { "respond",    TokenType.Respond    },
            { "to",         TokenType.To         },
            { "adapt",      TokenType.Adapt      },
            { "toward",     TokenType.Toward     },
            { "rate",       TokenType.Rate       },
            { "otherwise",  TokenType.Otherwise  },
            { "cycle",      TokenType.Cycle      },
            { "at",         TokenType.CycleAt     },
            { "period",     TokenType.Period     },
            { "ticker",     TokenType.Ticker     },
            { "every",      TokenType.Every      },
            { "wait",       TokenType.Wait       },
            { "defer",      TokenType.Defer      },
            { "until",      TokenType.Until      },
            { "mutate",     TokenType.Mutate     },
            { "by",         TokenType.By         },
        };

        // ─────────────────────────────────────────────────────────────────────────
        // Strings
        // ─────────────────────────────────────────────────────────────────────────

        private void ScanString(char quote, int startLine, int startCol)
        {
            // Check for triple-quoted (multi-line) string.
            if (!AtEnd() && Current() == quote &&
                _pos + 1 < _src.Length && _src[_pos + 1] == quote)
            {
                Advance(); Advance(); // consume the remaining two opening quotes
                ScanStringContent(quote, isMultiline: true, startLine, startCol);
            }
            else
            {
                ScanStringContent(quote, isMultiline: false, startLine, startCol);
            }
        }

        private void ScanStringContent(char quote, bool isMultiline,
                                        int startLine, int startCol)
        {
            var  textBuf   = new StringBuilder();
            bool hasInterp = false;

            while (!AtEnd())
            {
                // ── Closing quote(s) ─────────────────────────────────────────
                if (isMultiline)
                {
                    if (Current() == quote &&
                        _pos + 1 < _src.Length && _src[_pos + 1] == quote &&
                        _pos + 2 < _src.Length && _src[_pos + 2] == quote)
                    {
                        Advance(); Advance(); Advance();
                        FinaliseString(textBuf, hasInterp, startLine, startCol, isMultiline);
                        return;
                    }
                }
                else
                {
                    if (Current() == quote)
                    {
                        Advance();
                        FinaliseString(textBuf, hasInterp, startLine, startCol, isMultiline);
                        return;
                    }
                    // Unterminated single-line string.
                    if (Current() == '\n')
                    {
                        AddError("Unterminated string literal", startLine, startCol);
                        return;
                    }
                }

                // ── Escape sequence ──────────────────────────────────────────
                if (Current() == '\\')
                {
                    Advance(); // consume backslash
                    if (AtEnd())
                    {
                        AddError("Unexpected end of file in escape sequence", _line, _col);
                        return;
                    }
                    char esc = Advance();
                    textBuf.Append(ProcessEscape(esc, startLine, startCol));
                    continue;
                }

                // ── Interpolation: ${ ────────────────────────────────────────
                if (Current() == '$' && _pos + 1 < _src.Length && _src[_pos + 1] == '{')
                {
                    if (!hasInterp)
                    {
                        hasInterp = true;
                        // Retroactively replace with InterpStrStart.
                        // Insert at position in _tokens list.
                        _tokens.Add(new Token(TokenType.InterpStrStart, "", startLine, startCol));
                    }

                    // Flush accumulated text.
                    if (textBuf.Length > 0)
                    {
                        _tokens.Add(new Token(TokenType.InterpStrText,
                                              textBuf.ToString(), _line, _col));
                        textBuf.Clear();
                    }

                    int interpStartCol = _col;
                    Advance(); Advance(); // consume  $  {
                    _tokens.Add(new Token(TokenType.InterpStart, "${", _line, interpStartCol));

                    LexInterpolation();

                    _tokens.Add(new Token(TokenType.InterpEnd, "}", _line, _col));
                    Advance(); // consume closing  }
                    continue;
                }

                // ── Ordinary character ────────────────────────────────────────
                char ch = Advance(); // also tracks line/col via Advance()
                textBuf.Append(ch);
            }

            AddError("Unterminated string literal", startLine, startCol);
        }

        /// <summary>
        /// Emit the final token(s) for a completed string scan.
        /// </summary>
        private void FinaliseString(StringBuilder textBuf, bool hasInterp,
                                     int startLine, int startCol, bool isMultiline)
        {
            if (!hasInterp)
            {
                // Plain string — emit single token.
                TokenType t = isMultiline ? TokenType.MultilineString : TokenType.String;
                _tokens.Add(new Token(t, textBuf.ToString(), startLine, startCol));
            }
            else
            {
                // Flush remaining text, then close.
                if (textBuf.Length > 0)
                    _tokens.Add(new Token(TokenType.InterpStrText,
                                          textBuf.ToString(), _line, _col));
                _tokens.Add(new Token(TokenType.InterpStrEnd, "", _line, _col));
            }
        }

        /// <summary>
        /// Lex the expression inside ${ … }.
        /// _pos is just after the opening {.
        /// When this returns, _pos points at the closing }.
        /// </summary>
        private void LexInterpolation()
        {
            // Find the matching } by scanning forward with brace-depth tracking.
            // Simple strings inside the expression are skipped (no nested interp).
            int start = _pos;
            int depth = 1;
            int scan  = _pos;

            while (scan < _src.Length && depth > 0)
            {
                char c = _src[scan];
                if      (c == '{')                   depth++;
                else if (c == '}')                   depth--;
                else if (c == '"' || c == '\'')
                {
                    char q = c; scan++;
                    while (scan < _src.Length && _src[scan] != q)
                    {
                        if (_src[scan] == '\\') scan++;
                        scan++;
                    }
                    // scan now points at closing quote; loop will increment past it.
                }
                else if (c == '\n')
                {
                    AddError("Newlines are not allowed inside string interpolation", _line, _col);
                }

                if (depth > 0) scan++;
            }

            // 'scan' now points at the closing }.  Extract expression source.
            int exprLen  = scan - start;
            string exprSrc = exprLen > 0 ? _src.Substring(start, exprLen) : string.Empty;

            // Advance the main cursor to the closing } (do not consume it here).
            for (int i = start; i < scan; i++)
            {
                char ch = _src[i];
                if (ch == '\n') { _line++; _col = 1; }
                else             { _col++;             }
            }
            _pos = scan;

            // Lex the expression using a child lexer that suppresses structural tokens.
            var childResult = new Lexer(exprSrc, interpMode: true).Run();

            // Copy child errors with current line annotation.
            foreach (var e in childResult.Errors)
                _errors.Add(new LexError(e.Message, _line, e.Column));

            // Copy child tokens, stripping structural and Eof.
            foreach (var t in childResult.Tokens)
            {
                if (t.Type == TokenType.Eof     || t.Type == TokenType.Newline ||
                    t.Type == TokenType.Indent   || t.Type == TokenType.Dedent)
                    continue;
                _tokens.Add(t);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Raw strings
        // ─────────────────────────────────────────────────────────────────────────

        private void ScanRawString(char quote, int startLine, int startCol)
        {
            var sb = new StringBuilder();

            while (!AtEnd())
            {
                if (Current() == quote) { Advance(); break; }
                if (Current() == '\n' && quote != '"')
                {
                    AddError("Unterminated raw string", startLine, startCol);
                    return;
                }
                sb.Append(Advance());
            }

            _tokens.Add(new Token(TokenType.RawString, sb.ToString(), startLine, startCol));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Escape processing
        // ─────────────────────────────────────────────────────────────────────────

        private char ProcessEscape(char esc, int line, int col)
        {
            switch (esc)
            {
                case 'n':  return '\n';
                case 't':  return '\t';
                case 'r':  return '\r';
                case '\\': return '\\';
                case '"':  return '"';
                case '\'': return '\'';
                case '$':  return '$';
                case '0':  return '\0';
                default:
                    // Unknown escape — keep literal character.
                    AddError($"Unknown escape sequence '\\{esc}'", line, col);
                    return esc;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Cursor helpers
        // ─────────────────────────────────────────────────────────────────────────

        private bool AtEnd() => _pos >= _src.Length;

        /// <summary>Return current character without advancing.</summary>
        private char Current() => _pos < _src.Length ? _src[_pos] : '\0';

        /// <summary>Return character at offset <paramref name="n"/> ahead of current.</summary>
        private char Peek(int n)
        {
            int i = _pos + n;
            return i < _src.Length ? _src[i] : '\0';
        }

        /// <summary>Consume and return the current character, updating line/col.</summary>
        private char Advance()
        {
            char c = _src[_pos++];
            if (c == '\n')
            {
                _line++;
                _col       = 1;
                _lineStart = _pos;
            }
            else
            {
                _col++;
            }
            return c;
        }

        /// <summary>
        /// If the current character equals <paramref name="expected"/>, consume it
        /// and return true.  Otherwise return false without advancing.
        /// </summary>
        private bool Match(char expected)
        {
            if (AtEnd() || Current() != expected) return false;
            Advance();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Whitespace helpers
        // ─────────────────────────────────────────────────────────────────────────

        private void SkipSpaces()
        {
            while (!AtEnd() && Current() == ' ') Advance();
        }

        private void SkipToEndOfLine()
        {
            while (!AtEnd() && Current() != '\n') Advance();
        }

        private string ReadToEndOfLine()
        {
            var sb = new StringBuilder();
            while (!AtEnd() && Current() != '\n')
                sb.Append(Advance());
            return sb.ToString().TrimEnd();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Token emission
        // ─────────────────────────────────────────────────────────────────────────

        private void Emit(TokenType type, string value)
            => _tokens.Add(new Token(type, value, _line, _col));

        private void Emit(TokenType type, string value, int line, int col)
            => _tokens.Add(new Token(type, value, line, col));

        private void AddError(string message)
            => _errors.Add(new LexError(message, _line, _col));

        private void AddError(string message, int line, int col)
            => _errors.Add(new LexError(message, line, col));

        // ─────────────────────────────────────────────────────────────────────────
        // Character classification
        // ─────────────────────────────────────────────────────────────────────────

        private static bool IsDigit(char c)          => c >= '0' && c <= '9';
        private static bool IsHexDigit(char c)        => IsDigit(c)
                                                      || (c >= 'a' && c <= 'f')
                                                      || (c >= 'A' && c <= 'F');
        private static bool IsLetter(char c)          => (c >= 'a' && c <= 'z')
                                                      || (c >= 'A' && c <= 'Z')
                                                      || c == '_';
        private static bool IsLetterOrDigit(char c)   => IsLetter(c) || IsDigit(c);
    }
}
