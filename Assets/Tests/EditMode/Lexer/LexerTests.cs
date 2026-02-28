using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.Tests.Lexer
{
    // ─────────────────────────────────────────────────────────────────────────
    // Lexer tests — written BEFORE the lexer was corrected; they document the
    // exact contract the rest of the pipeline depends on.
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class LexerTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static List<Token> Lex(string src)
            => GrowlLanguage.Lexer.Lexer.Lex(src).Tokens;

        private static Token First(string src, TokenType skip = TokenType.Newline)
            => Lex(src).First(t => t.Type != skip && t.Type != TokenType.Eof);

        private static IEnumerable<Token> Content(string src)
            => Lex(src).Where(t => t.Type != TokenType.Newline &&
                                   t.Type != TokenType.Indent  &&
                                   t.Type != TokenType.Dedent  &&
                                   t.Type != TokenType.Eof);

        private static List<TokenType> Types(string src)
            => Content(src).Select(t => t.Type).ToList();

        // ── Integer literals ──────────────────────────────────────────────────

        [Test] public void Integer_Decimal()
        {
            var tok = First("42");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Integer));
            Assert.That(tok.Value, Is.EqualTo("42"));
        }

        [Test] public void Integer_WithUnderscores()
        {
            var tok = First("1_000_000");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Integer));
            Assert.That(tok.Value, Is.EqualTo("1000000"));
        }

        [Test] public void Integer_Hex()
        {
            var tok = First("0xFF");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Integer));
            Assert.That(tok.Value, Is.EqualTo("0xFF"));
        }

        [Test] public void Integer_Binary()
        {
            var tok = First("0b1010");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Integer));
            Assert.That(tok.Value, Is.EqualTo("0b1010"));
        }

        // ── Float literals ────────────────────────────────────────────────────

        [Test] public void Float_Basic()
        {
            var tok = First("3.14");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Float));
            Assert.That(tok.Value, Is.EqualTo("3.14"));
        }

        [Test] public void Float_Scientific()
        {
            var tok = First("6.022e23");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Float));
            Assert.That(tok.Value, Is.EqualTo("6.022e23"));
        }

        [Test] public void Float_NegativeExponent()
        {
            var tok = First("1.5e-4");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Float));
            Assert.That(tok.Value, Is.EqualTo("1.5e-4"));
        }

        // ── Unit suffixes ─────────────────────────────────────────────────────

        [Test] public void UnitSuffix_Centimetres()
        {
            var tok = First("5.0cm");
            Assert.That(tok.Type, Is.EqualTo(TokenType.Float));
            Assert.That(tok.Unit, Is.EqualTo("cm"));
            Assert.That(tok.Value, Is.EqualTo("5.0"));
        }

        [Test] public void UnitSuffix_Percent()
        {
            var tok = First("85%");
            Assert.That(tok.Unit, Is.EqualTo("%"));
        }

        [Test] public void UnitSuffix_Kilowatt()
        {
            var tok = First("12kW");
            Assert.That(tok.Unit, Is.EqualTo("kW"));
        }

        [Test] public void UnitSuffix_DoesNotConsumeWordBoundary()
        {
            // "sm" should not be consumed as unit "m" — it's an identifier
            var toks = Types("sm");
            Assert.That(toks, Is.EqualTo(new[] { TokenType.Identifier }));
        }

        // ── Color literals ────────────────────────────────────────────────────

        [Test] public void Color_SixHexDigits()
        {
            var tok = First("#FF0000");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Color));
            Assert.That(tok.Value, Is.EqualTo("FF0000"));
        }

        [Test] public void Color_LowercaseNormalisedToUpper()
        {
            var tok = First("#b4ffb4");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.Color));
            Assert.That(tok.Value, Is.EqualTo("B4FFB4"));
        }

        [Test] public void Hash_WithSpace_IsComment_NotColor()
        {
            var toks = Types("# FF0000");
            Assert.That(toks, Is.Empty); // comment consumed
        }

        // ── Strings ──────────────────────────────────────────────────────────

        [Test] public void String_DoubleQuoted()
        {
            var tok = First(@"""hello""");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.String));
            Assert.That(tok.Value, Is.EqualTo("hello"));
        }

        [Test] public void String_SingleQuoted()
        {
            var tok = First("'world'");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.String));
        }

        [Test] public void String_EscapeNewline()
        {
            var tok = First(@"""line1\nline2""");
            Assert.That(tok.Value, Does.Contain("\n"));
        }

        [Test] public void RawString_NoEscapeProcessing()
        {
            var tok = First(@"r""\n not a newline""");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.RawString));
            Assert.That(tok.Value, Is.EqualTo(@"\n not a newline"));
        }

        // ── String interpolation ─────────────────────────────────────────────

        [Test] public void Interpolation_EmitsCorrectTokenSequence()
        {
            var toks = Content(@"""hello ${name}""").ToList();
            // InterpStrStart  InterpStrText("hello ")  InterpStart
            // Identifier(name)  InterpEnd  InterpStrEnd
            Assert.That(toks[0].Type, Is.EqualTo(TokenType.InterpStrStart));
            Assert.That(toks[1].Type, Is.EqualTo(TokenType.InterpStrText));
            Assert.That(toks[1].Value, Is.EqualTo("hello "));
            Assert.That(toks[2].Type, Is.EqualTo(TokenType.InterpStart));
            Assert.That(toks[3].Type, Is.EqualTo(TokenType.Identifier));
            Assert.That(toks[3].Value, Is.EqualTo("name"));
            Assert.That(toks[4].Type, Is.EqualTo(TokenType.InterpEnd));
            Assert.That(toks[5].Type, Is.EqualTo(TokenType.InterpStrEnd));
        }

        [Test] public void Interpolation_ExpressionWithOperator()
        {
            var toks = Content(@"""${x + 1}""").Select(t => t.Type).ToList();
            Assert.That(toks, Contains.Item(TokenType.InterpStart));
            Assert.That(toks, Contains.Item(TokenType.Plus));
            Assert.That(toks, Contains.Item(TokenType.InterpEnd));
        }

        // ── Comments ─────────────────────────────────────────────────────────

        [Test] public void RegularComment_Consumed()
        {
            Assert.That(Types("# just a comment"), Is.Empty);
        }

        [Test] public void DocComment_EmittedAsToken()
        {
            var tok = First("## doc text");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.DocComment));
            Assert.That(tok.Value, Is.EqualTo("doc text"));
        }

        [Test] public void WarnComment_EmittedAsToken()
        {
            var tok = First("#! warning text");
            Assert.That(tok.Type,  Is.EqualTo(TokenType.WarnComment));
            Assert.That(tok.Value, Is.EqualTo("warning text"));
        }

        [Test] public void BlockComment_Consumed()
        {
            Assert.That(Types("### block\ncontent\n###"), Is.Empty);
        }

        // ── Keywords ─────────────────────────────────────────────────────────

        [Test] public void Keywords_AreRecognised()
        {
            Assert.That(First("if").Type,       Is.EqualTo(TokenType.If));
            Assert.That(First("fn").Type,       Is.EqualTo(TokenType.Fn));
            Assert.That(First("class").Type,    Is.EqualTo(TokenType.Class));
            Assert.That(First("phase").Type,    Is.EqualTo(TokenType.Phase));
            Assert.That(First("when").Type,     Is.EqualTo(TokenType.When));
            Assert.That(First("adapt").Type,    Is.EqualTo(TokenType.Adapt));
            Assert.That(First("respond").Type,  Is.EqualTo(TokenType.Respond));
            Assert.That(First("cycle").Type,    Is.EqualTo(TokenType.Cycle));
            Assert.That(First("ticker").Type,   Is.EqualTo(TokenType.Ticker));
            Assert.That(First("mutate").Type,   Is.EqualTo(TokenType.Mutate));
        }

        [Test] public void At_Keyword_IsCycleAt()
        {
            // "at" as a word is the biological CycleAt keyword.
            Assert.That(First("at").Type, Is.EqualTo(TokenType.CycleAt));
        }

        [Test] public void AtSign_IsDecoratorAt()
        {
            Assert.That(First("@").Type, Is.EqualTo(TokenType.At));
        }

        // ── Operators ─────────────────────────────────────────────────────────

        [Test] public void Operators_MultiChar()
        {
            Assert.That(First("|>").Type,  Is.EqualTo(TokenType.PipeGreater));
            Assert.That(First("??").Type,  Is.EqualTo(TokenType.QuestionQuestion));
            Assert.That(First("->").Type,  Is.EqualTo(TokenType.Arrow));
            Assert.That(First("..=").Type, Is.EqualTo(TokenType.DotDotEqual));
            Assert.That(First("...").Type, Is.EqualTo(TokenType.DotDotDot));
            Assert.That(First("^^").Type,  Is.EqualTo(TokenType.CaretCaret));
            Assert.That(First("**=").Type, Is.EqualTo(TokenType.StarStarEqual));
            Assert.That(First("//=").Type, Is.EqualTo(TokenType.SlashSlashEqual));
        }

        [Test] public void PlusEqual_IsSingleToken()
        {
            var toks = Types("x += 1");
            Assert.That(toks, Does.Contain(TokenType.PlusEqual));
            Assert.That(toks, Has.No.Member(TokenType.Plus));
        }

        // ── INDENT / DEDENT ───────────────────────────────────────────────────

        [Test] public void Indentation_SimpleBlock_EmitsIndentDedent()
        {
            string src = "if x:\n    y\n";
            var types = Lex(src).Select(t => t.Type).ToList();
            Assert.That(types, Contains.Item(TokenType.Indent));
            Assert.That(types, Contains.Item(TokenType.Dedent));
        }

        [Test] public void Indentation_BlankLinesIgnored()
        {
            string src = "if x:\n\n    y\n";
            var types = Lex(src).Select(t => t.Type).ToList();
            // Only one INDENT, not two
            Assert.That(types.Count(t => t == TokenType.Indent), Is.EqualTo(1));
        }

        [Test] public void Indentation_Tab_ProducesError()
        {
            var result = GrowlLanguage.Lexer.Lexer.Lex("if x:\n\ty\n");
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Errors[0].Message, Does.Contain("Tab"));
        }

        [Test] public void Indentation_BracketsSuppress_NewlineAndIndent()
        {
            // Inside ( ), newlines and indentation don't generate structural tokens.
            // The lexer always appends a trailing Newline before Eof, so exactly
            // one Newline is expected — the post-bracket one, not from inside.
            string src = "(\n    x\n)";
            var types = Lex(src).Select(t => t.Type).ToList();
            Assert.That(types, Has.No.Member(TokenType.Indent));
            Assert.That(types.Count(t => t == TokenType.Newline), Is.EqualTo(1));
        }

        // ── Position tracking ─────────────────────────────────────────────────

        [Test] public void Token_Line_IsOneBasedAndCorrect()
        {
            var toks = Content("x\ny\nz").ToList();
            Assert.That(toks[0].Line, Is.EqualTo(1));
            Assert.That(toks[1].Line, Is.EqualTo(2));
            Assert.That(toks[2].Line, Is.EqualTo(3));
        }

        [Test] public void Token_Column_IsOneBasedAndCorrect()
        {
            var toks = Content("  x").ToList();
            Assert.That(toks[0].Column, Is.EqualTo(3)); // after two spaces
        }
    }
}
