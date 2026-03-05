using System.Collections.Generic;
using NUnit.Framework;
using CodeEditor.Language;

[TestFixture]
public class GrowlLanguageServiceTests
{
    private GrowlLanguageService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new GrowlLanguageService();
    }

    // ── Keyword classification ──────────────────────────────────────

    [Test]
    public void ControlKeywords_AreClassifiedAsKeyword()
    {
        var tokens = Tokenize("if x > 5:");
        AssertTokenAt(tokens, 0, 0, 2, TokenCategory.Keyword); // if
    }

    [Test]
    public void DeclarationKeywords_AreClassifiedAsKeyword()
    {
        var tokens = Tokenize("fn main():");
        AssertTokenAt(tokens, 0, 0, 2, TokenCategory.Keyword); // fn
    }

    [TestCase("for")]
    [TestCase("while")]
    [TestCase("return")]
    [TestCase("class")]
    [TestCase("import")]
    [TestCase("true")]
    [TestCase("false")]
    [TestCase("none")]
    [TestCase("and")]
    [TestCase("or")]
    [TestCase("not")]
    [TestCase("self")]
    public void AllKeywords_AreClassified(string keyword)
    {
        var tokens = Tokenize(keyword);
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Keyword, tokens[0].Category);
    }

    // ── Biological keywords ─────────────────────────────────────────

    [TestCase("phase")]
    [TestCase("when")]
    [TestCase("then")]
    [TestCase("respond")]
    [TestCase("adapt")]
    [TestCase("cycle")]
    [TestCase("ticker")]
    [TestCase("every")]
    [TestCase("mutate")]
    [TestCase("defer")]
    public void BiologicalKeywords_AreClassified(string keyword)
    {
        var tokens = Tokenize(keyword);
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.BiologicalKeyword, tokens[0].Category);
    }

    [Test]
    public void PhaseDeclaration_HasBiologicalKeyword()
    {
        var tokens = Tokenize("phase germination:");
        AssertTokenAt(tokens, 0, 0, 5, TokenCategory.BiologicalKeyword);
    }

    // ── Identifiers ────────────────────────────────────────────────

    [Test]
    public void PlainIdentifiers_AreClassifiedAsVariable()
    {
        var tokens = Tokenize("foo bar baz");
        Assert.AreEqual(3, tokens.Count);
        Assert.AreEqual(TokenCategory.Variable, tokens[0].Category);
        Assert.AreEqual(TokenCategory.Variable, tokens[1].Category);
        Assert.AreEqual(TokenCategory.Variable, tokens[2].Category);
    }

    [Test]
    public void FunctionCall_IsClassifiedAsFunction()
    {
        var tokens = Tokenize("print(x)");
        AssertTokenAt(tokens, 0, 0, 5, TokenCategory.Function); // print
        AssertTokenAt(tokens, 1, 6, 1, TokenCategory.Variable); // x
    }

    // ── Strings ────────────────────────────────────────────────────

    [Test]
    public void SingleLineString_DoubleQuote()
    {
        var tokens = Tokenize("x = \"hello world\"");
        var strTok = FindToken(tokens, TokenCategory.String);
        Assert.IsNotNull(strTok);
    }

    [Test]
    public void SingleLineString_SingleQuote()
    {
        var tokens = Tokenize("x = 'hello'");
        var strTok = FindToken(tokens, TokenCategory.String);
        Assert.IsNotNull(strTok);
    }

    [Test]
    public void RawString_ClassifiedAsString()
    {
        var tokens = Tokenize("r\"no escapes\"");
        var strTok = FindToken(tokens, TokenCategory.String);
        Assert.IsNotNull(strTok);
        Assert.AreEqual(0, strTok.Value.StartColumn);
    }

    [Test]
    public void StringWithEscapes_IncludesEscapedQuote()
    {
        // "hello \"world\""
        var tokens = Tokenize("\"hello \\\"world\\\"\"");
        var strTok = FindToken(tokens, TokenCategory.String);
        Assert.IsNotNull(strTok);
        Assert.AreEqual(0, strTok.Value.StartColumn);
    }

    // ── Multiline strings ──────────────────────────────────────────

    [Test]
    public void MultilineStringStart_SetsEndState()
    {
        _service.TokenizeLine("x = \"\"\"hello", null);
        var endState = (GrowlLanguageService.LineState)_service.GetLineEndState("x = \"\"\"hello", null);
        Assert.AreEqual(GrowlLanguageService.LineState.Mode.InMultilineString, endState.CurrentMode);
        Assert.AreEqual('"', endState.StringQuote);
    }

    [Test]
    public void MultilineStringContinuation_EntireLineIsString()
    {
        _service.TokenizeLine("x = \"\"\"hello", null);
        var state = _service.GetLineEndState("x = \"\"\"hello", null);

        var tokens = _service.TokenizeLine("middle line", state);
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.String, tokens[0].Category);
        Assert.AreEqual(0, tokens[0].StartColumn);
        Assert.AreEqual(11, tokens[0].Length);
    }

    [Test]
    public void MultilineStringClose_ReturnsToNormal()
    {
        var state = new GrowlLanguageService.LineState(
            GrowlLanguageService.LineState.Mode.InMultilineString, '"');

        _service.TokenizeLine("end\"\"\" + x", state);
        var endState = (GrowlLanguageService.LineState)_service.GetLineEndState("end\"\"\" + x", state);
        Assert.AreEqual(GrowlLanguageService.LineState.Mode.Normal, endState.CurrentMode);
    }

    // ── Comments ───────────────────────────────────────────────────

    [Test]
    public void RegularComment_EntireLineIsComment()
    {
        var tokens = Tokenize("# this is a comment");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Comment, tokens[0].Category);
        Assert.AreEqual(0, tokens[0].StartColumn);
    }

    [Test]
    public void DocComment_IsComment()
    {
        var tokens = Tokenize("## documentation");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Comment, tokens[0].Category);
    }

    [Test]
    public void WarnComment_IsComment()
    {
        var tokens = Tokenize("#! warning message");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Comment, tokens[0].Category);
    }

    [Test]
    public void InlineComment_AfterCode()
    {
        var tokens = Tokenize("x = 5 # inline comment");
        var commentTok = FindToken(tokens, TokenCategory.Comment);
        Assert.IsNotNull(commentTok);
    }

    [Test]
    public void ColorLiteral_IsNumber()
    {
        var tokens = Tokenize("#FF0000");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Number, tokens[0].Category);
        Assert.AreEqual(7, tokens[0].Length);
    }

    [Test]
    public void ColorLiteralInExpression_NotConfusedWithComment()
    {
        var tokens = Tokenize("c = #B4FFB4");
        var numTok = FindToken(tokens, TokenCategory.Number);
        Assert.IsNotNull(numTok);
        Assert.AreEqual(4, numTok.Value.StartColumn);
        Assert.AreEqual(7, numTok.Value.Length);
    }

    // ── Block comments ─────────────────────────────────────────────

    [Test]
    public void BlockCommentStart_SetsEndState()
    {
        _service.TokenizeLine("### block comment", null);
        var endState = (GrowlLanguageService.LineState)_service.GetLineEndState("### block comment", null);
        Assert.AreEqual(GrowlLanguageService.LineState.Mode.InBlockComment, endState.CurrentMode);
    }

    [Test]
    public void BlockCommentSingleLine_ClosesOnSameLine()
    {
        var tokens = Tokenize("### inline ### x = 5");
        var commentTok = FindToken(tokens, TokenCategory.Comment);
        Assert.IsNotNull(commentTok);
        Assert.AreEqual(0, commentTok.Value.StartColumn);
        Assert.AreEqual(14, commentTok.Value.Length); // ### inline ###

        _service.TokenizeLine("### inline ### x = 5", null);
        var endState = (GrowlLanguageService.LineState)_service.GetLineEndState("### inline ### x = 5", null);
        Assert.AreEqual(GrowlLanguageService.LineState.Mode.Normal, endState.CurrentMode);
    }

    // ── Numbers ────────────────────────────────────────────────────

    [Test]
    public void IntegerLiteral()
    {
        var tokens = Tokenize("x = 42");
        var numTok = FindToken(tokens, TokenCategory.Number);
        Assert.IsNotNull(numTok);
        Assert.AreEqual(4, numTok.Value.StartColumn);
    }

    [Test]
    public void FloatLiteral()
    {
        var tokens = Tokenize("3.14");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Number, tokens[0].Category);
    }

    [Test]
    public void HexLiteral()
    {
        var tokens = Tokenize("0xFF");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Number, tokens[0].Category);
    }

    [Test]
    public void BinaryLiteral()
    {
        var tokens = Tokenize("0b1010");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Number, tokens[0].Category);
    }

    [Test]
    public void NumberWithUnitSuffix()
    {
        var tokens = Tokenize("5.0cm");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Number, tokens[0].Category);
        Assert.AreEqual(5, tokens[0].Length);
    }

    [Test]
    public void NumberWithPercentSuffix()
    {
        var tokens = Tokenize("85%");
        Assert.AreEqual(1, tokens.Count);
        Assert.AreEqual(TokenCategory.Number, tokens[0].Category);
        Assert.AreEqual(3, tokens[0].Length);
    }

    // ── Decorators ─────────────────────────────────────────────────

    [Test]
    public void Decorator_IsClassified()
    {
        var tokens = Tokenize("@role(stem)");
        var decTok = FindToken(tokens, TokenCategory.Decorator);
        Assert.IsNotNull(decTok);
        Assert.AreEqual(0, decTok.Value.StartColumn);
        Assert.AreEqual(5, decTok.Value.Length); // @role
    }

    // ── Operators ──────────────────────────────────────────────────

    [Test]
    public void Operators_AreClassified()
    {
        var tokens = Tokenize("x + y == z");
        var opTokens = FindAll(tokens, TokenCategory.Operator);
        Assert.AreEqual(2, opTokens.Count);
    }

    // ── ShouldIndentAfterLine ──────────────────────────────────────

    [Test]
    public void ShouldIndent_LineEndingWithColon()
    {
        Assert.IsTrue(_service.ShouldIndentAfterLine("if x > 5:"));
        Assert.IsTrue(_service.ShouldIndentAfterLine("fn main():"));
        Assert.IsTrue(_service.ShouldIndentAfterLine("phase germination:"));
    }

    [Test]
    public void ShouldNotIndent_NormalLine()
    {
        Assert.IsFalse(_service.ShouldIndentAfterLine("x = 5"));
        Assert.IsFalse(_service.ShouldIndentAfterLine(""));
    }

    // ── Edge cases ─────────────────────────────────────────────────

    [Test]
    public void EmptyLine_ReturnsNoTokens()
    {
        Assert.AreEqual(0, Tokenize("").Count);
    }

    [Test]
    public void WhitespaceOnlyLine_ReturnsNoTokens()
    {
        Assert.AreEqual(0, Tokenize("    ").Count);
    }

    [Test]
    public void MixedLine_AllCategoriesPresent()
    {
        var tokens = Tokenize("fn add(x, y): # adds two numbers");
        Assert.IsNotNull(FindToken(tokens, TokenCategory.Keyword));
        Assert.IsNotNull(FindToken(tokens, TokenCategory.Comment));
    }

    [Test]
    public void LineState_Equality()
    {
        var a = new GrowlLanguageService.LineState(
            GrowlLanguageService.LineState.Mode.InMultilineString, '"');
        var b = new GrowlLanguageService.LineState(
            GrowlLanguageService.LineState.Mode.InMultilineString, '"');
        var c = new GrowlLanguageService.LineState(
            GrowlLanguageService.LineState.Mode.Normal, '\0');

        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    // ── Helpers ────────────────────────────────────────────────────

    private IReadOnlyList<HighlightToken> Tokenize(string line)
    {
        return _service.TokenizeLine(line, null);
    }

    private static void AssertTokenAt(IReadOnlyList<HighlightToken> tokens, int index,
        int expectedStart, int expectedLength, TokenCategory expectedCategory)
    {
        Assert.IsTrue(index < tokens.Count, $"Expected token at index {index} but only {tokens.Count} tokens");
        Assert.AreEqual(expectedStart, tokens[index].StartColumn, "StartColumn");
        Assert.AreEqual(expectedLength, tokens[index].Length, "Length");
        Assert.AreEqual(expectedCategory, tokens[index].Category, "Category");
    }

    private static HighlightToken? FindToken(IReadOnlyList<HighlightToken> tokens, TokenCategory cat)
    {
        for (int i = 0; i < tokens.Count; i++)
            if (tokens[i].Category == cat) return tokens[i];
        return null;
    }

    private static List<HighlightToken> FindAll(IReadOnlyList<HighlightToken> tokens, TokenCategory cat)
    {
        var result = new List<HighlightToken>();
        for (int i = 0; i < tokens.Count; i++)
            if (tokens[i].Category == cat) result.Add(tokens[i]);
        return result;
    }
}
