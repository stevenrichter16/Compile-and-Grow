using NUnit.Framework;
using CodeEditor.Core;
using CodeEditor.Language;

namespace CodeEditor.Tests
{
    [TestFixture]
    public class TextOperationsTests
    {
        [Test]
        public void GetIndentLevel_NoSpaces() => Assert.AreEqual(0, TextOperations.GetIndentLevel("hello"));

        [Test]
        public void GetIndentLevel_FourSpaces() => Assert.AreEqual(4, TextOperations.GetIndentLevel("    hello"));

        [Test]
        public void GetIndentLevel_EmptyString() => Assert.AreEqual(0, TextOperations.GetIndentLevel(""));

        [Test]
        public void GetIndentLevel_AllSpaces() => Assert.AreEqual(8, TextOperations.GetIndentLevel("        "));

        [Test]
        public void GetIndentLevel_Null() => Assert.AreEqual(0, TextOperations.GetIndentLevel(null));

        [Test]
        public void ComputeAutoIndent_NoLanguageService_KeepsCurrentIndent()
        {
            string indent = TextOperations.ComputeAutoIndent("    hello", 4);
            Assert.AreEqual("    ", indent);
        }

        [Test]
        public void ComputeAutoIndent_WithLanguageService_IncreasesIndent()
        {
            var svc = new MockLanguageService(shouldIndent: true);
            string indent = TextOperations.ComputeAutoIndent("    if x:", 4, svc);
            Assert.AreEqual("        ", indent);
        }

        [Test]
        public void ComputeAutoIndent_WithLanguageService_NoIncrease()
        {
            var svc = new MockLanguageService(shouldIndent: false);
            string indent = TextOperations.ComputeAutoIndent("    x = 1", 4, svc);
            Assert.AreEqual("    ", indent);
        }

        [Test]
        public void IndentLines_SingleLine()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            TextOperations.IndentLines(doc, 0, 0, 4, outdent: false);
            Assert.AreEqual("    hello", doc.GetLine(0));
        }

        [Test]
        public void IndentLines_MultipleLines()
        {
            var doc = new DocumentModel();
            doc.SetText("aaa\nbbb\nccc");
            TextOperations.IndentLines(doc, 0, 2, 4, outdent: false);
            Assert.AreEqual("    aaa", doc.GetLine(0));
            Assert.AreEqual("    bbb", doc.GetLine(1));
            Assert.AreEqual("    ccc", doc.GetLine(2));
        }

        [Test]
        public void OutdentLines_FullIndent()
        {
            var doc = new DocumentModel();
            doc.SetText("    hello\n    world");
            TextOperations.IndentLines(doc, 0, 1, 4, outdent: true);
            Assert.AreEqual("hello", doc.GetLine(0));
            Assert.AreEqual("world", doc.GetLine(1));
        }

        [Test]
        public void OutdentLines_PartialIndent()
        {
            var doc = new DocumentModel();
            doc.SetText("  hello");
            TextOperations.IndentLines(doc, 0, 0, 4, outdent: true);
            Assert.AreEqual("hello", doc.GetLine(0));
        }

        [Test]
        public void OutdentLines_NoIndent_NoOp()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            TextOperations.IndentLines(doc, 0, 0, 4, outdent: true);
            Assert.AreEqual("hello", doc.GetLine(0));
        }

        [Test]
        public void ClampPosition_ValidPosition_Unchanged()
        {
            var doc = new DocumentModel();
            doc.SetText("hello\nworld");
            var pos = TextOperations.ClampPosition(doc, new TextPosition(1, 3));
            Assert.AreEqual(new TextPosition(1, 3), pos);
        }

        [Test]
        public void ClampPosition_BeyondEnd_Clamps()
        {
            var doc = new DocumentModel();
            doc.SetText("hi");
            var pos = TextOperations.ClampPosition(doc, new TextPosition(5, 99));
            Assert.AreEqual(new TextPosition(0, 2), pos);
        }

        [Test]
        public void ClampPosition_Negative_ClampsToZero()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            var pos = TextOperations.ClampPosition(doc, new TextPosition(-1, -5));
            Assert.AreEqual(TextPosition.Zero, pos);
        }

        private sealed class MockLanguageService : ILanguageService
        {
            private readonly bool _shouldIndent;

            public MockLanguageService(bool shouldIndent) { _shouldIndent = shouldIndent; }

            public bool ShouldIndentAfterLine(string lineText) => _shouldIndent;

            public System.Collections.Generic.IReadOnlyList<HighlightToken> TokenizeLine(string lineText, object lineState)
                => new HighlightToken[0];

            public object GetLineEndState(string lineText, object lineStartState) => null;
        }
    }
}
