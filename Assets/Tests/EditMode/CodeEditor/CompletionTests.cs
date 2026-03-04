using NUnit.Framework;
using CodeEditor.Completion;
using CodeEditor.Core;
using CodeEditor.Editor;

namespace CodeEditor.Tests
{
    [TestFixture]
    public class CompletionTests
    {
        [Test]
        public void CompletionItem_StoresFields()
        {
            var item = new CompletionItem("print", CompletionKind.Function, "print(value)");
            Assert.AreEqual("print", item.Label);
            Assert.AreEqual("print", item.InsertText);
            Assert.AreEqual("print(value)", item.Detail);
            Assert.AreEqual(CompletionKind.Function, item.Kind);
        }

        [Test]
        public void CompletionItem_InsertTextOverride()
        {
            var item = new CompletionItem("for loop", CompletionKind.Snippet, "snippet", "for x in list:");
            Assert.AreEqual("for loop", item.Label);
            Assert.AreEqual("for x in list:", item.InsertText);
        }

        [Test]
        public void CompletionResult_Empty_HasNoItems()
        {
            Assert.AreEqual(0, CompletionResult.Empty.Items.Count);
            Assert.IsTrue(CompletionResult.Empty.ReplacementRange.IsEmpty);
        }

        [Test]
        public void GetWordPrefixAtCursor_EmptyDocument_ReturnsEmptyPrefix()
        {
            var doc = new DocumentModel();
            var ctrl = new EditorController(doc, new EditorConfig());
            var (prefix, range) = ctrl.GetWordPrefixAtCursor();
            Assert.AreEqual("", prefix);
            Assert.IsTrue(range.IsEmpty);
        }

        [Test]
        public void GetWordPrefixAtCursor_MidWord_ReturnsPrefix()
        {
            var doc = new DocumentModel();
            doc.SetText("print");
            doc.SetCursor(new TextPosition(0, 3)); // cur after "pri"
            var ctrl = new EditorController(doc, new EditorConfig());
            var (prefix, range) = ctrl.GetWordPrefixAtCursor();
            Assert.AreEqual("pri", prefix);
            Assert.AreEqual(new TextPosition(0, 0), range.Start);
            Assert.AreEqual(new TextPosition(0, 3), range.End);
        }

        [Test]
        public void GetWordPrefixAtCursor_AfterDot_ReturnsEmpty()
        {
            var doc = new DocumentModel();
            doc.SetText("math.");
            doc.SetCursor(new TextPosition(0, 5)); // right after dot
            var ctrl = new EditorController(doc, new EditorConfig());
            var (prefix, range) = ctrl.GetWordPrefixAtCursor();
            // Dot is not a word char, so prefix should be empty
            Assert.AreEqual("", prefix);
        }

        [Test]
        public void GetWordPrefixAtCursor_AfterDotWithPartialMember_ReturnsPartial()
        {
            var doc = new DocumentModel();
            doc.SetText("math.sq");
            doc.SetCursor(new TextPosition(0, 7)); // after "sq"
            var ctrl = new EditorController(doc, new EditorConfig());
            var (prefix, range) = ctrl.GetWordPrefixAtCursor();
            Assert.AreEqual("sq", prefix);
        }

        [Test]
        public void GetDotContext_AfterDot_ReturnsDotInfo()
        {
            var doc = new DocumentModel();
            doc.SetText("math.sq");
            doc.SetCursor(new TextPosition(0, 7));
            var ctrl = new EditorController(doc, new EditorConfig());
            var (isDot, receiver, memberPrefix, memberRange) = ctrl.GetDotContext();
            Assert.IsTrue(isDot);
            Assert.AreEqual("math", receiver);
            Assert.AreEqual("sq", memberPrefix);
            Assert.AreEqual(new TextPosition(0, 5), memberRange.Start);
            Assert.AreEqual(new TextPosition(0, 7), memberRange.End);
        }

        [Test]
        public void GetDotContext_NoDot_ReturnsFalse()
        {
            var doc = new DocumentModel();
            doc.SetText("print");
            doc.SetCursor(new TextPosition(0, 5));
            var ctrl = new EditorController(doc, new EditorConfig());
            var (isDot, _, _, _) = ctrl.GetDotContext();
            Assert.IsFalse(isDot);
        }

        [Test]
        public void GetDotContext_DotAtStart_ReturnsFalse()
        {
            var doc = new DocumentModel();
            doc.SetText(".foo");
            doc.SetCursor(new TextPosition(0, 4));
            var ctrl = new EditorController(doc, new EditorConfig());
            var (isDot, _, _, _) = ctrl.GetDotContext();
            // No receiver before dot → should return false
            Assert.IsFalse(isDot);
        }

        [Test]
        public void SimpleProvider_ReturnsMatchingItems()
        {
            var doc = new DocumentModel();
            doc.SetText("pri");
            doc.SetCursor(new TextPosition(0, 3));

            var provider = new TestCompletionProvider(new[]
            {
                new CompletionItem("print", CompletionKind.Function, "print(value)"),
                new CompletionItem("private", CompletionKind.Keyword, "keyword"),
                new CompletionItem("abs", CompletionKind.Function, "abs(n)"),
            });

            var result = provider.GetCompletions(doc, doc.Cursor);
            // "pri" matches "print" and "private" but not "abs"
            Assert.AreEqual(2, result.Items.Count);
        }

        /// <summary>Simple provider for testing that filters by prefix.</summary>
        private class TestCompletionProvider : ICompletionProvider
        {
            private readonly CompletionItem[] _items;

            public TestCompletionProvider(CompletionItem[] items) => _items = items;

            public CompletionResult GetCompletions(DocumentModel doc, TextPosition cursor)
            {
                string line = doc.GetLine(cursor.Line);
                int col = cursor.Column;
                int start = col;
                while (start > 0 && EditorController.IsWordChar(line[start - 1]))
                    start--;
                string prefix = line.Substring(start, col - start);
                var range = new TextRange(new TextPosition(cursor.Line, start), new TextPosition(cursor.Line, col));

                var results = new System.Collections.Generic.List<CompletionItem>();
                foreach (var item in _items)
                {
                    if (item.Label.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                        results.Add(item);
                }
                return new CompletionResult(results, range);
            }
        }
    }
}
