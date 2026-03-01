using NUnit.Framework;
using CodeEditor.Core;

namespace CodeEditor.Tests
{
    [TestFixture]
    public class DocumentModelTests
    {
        [Test]
        public void NewDocument_HasOneLine()
        {
            var doc = new DocumentModel();
            Assert.AreEqual(1, doc.LineCount);
            Assert.AreEqual(string.Empty, doc.GetLine(0));
        }

        [Test]
        public void SetText_SplitsIntoLines()
        {
            var doc = new DocumentModel();
            doc.SetText("hello\nworld\nfoo");
            Assert.AreEqual(3, doc.LineCount);
            Assert.AreEqual("hello", doc.GetLine(0));
            Assert.AreEqual("world", doc.GetLine(1));
            Assert.AreEqual("foo", doc.GetLine(2));
        }

        [Test]
        public void SetText_Null_ResetsToEmptyLine()
        {
            var doc = new DocumentModel();
            doc.SetText("abc");
            doc.SetText(null);
            Assert.AreEqual(1, doc.LineCount);
            Assert.AreEqual(string.Empty, doc.GetLine(0));
        }

        [Test]
        public void GetText_RoundTrips()
        {
            var doc = new DocumentModel();
            string text = "line1\nline2\nline3";
            doc.SetText(text);
            Assert.AreEqual(text, doc.GetText());
        }

        [Test]
        public void Insert_SingleChar_IntoEmptyDoc()
        {
            var doc = new DocumentModel();
            doc.Insert(TextPosition.Zero, "a");
            Assert.AreEqual("a", doc.GetLine(0));
        }

        [Test]
        public void Insert_SingleLine_AtMiddle()
        {
            var doc = new DocumentModel();
            doc.SetText("helo");
            doc.Insert(new TextPosition(0, 2), "l");
            Assert.AreEqual("hello", doc.GetLine(0));
        }

        [Test]
        public void Insert_MultiLine()
        {
            var doc = new DocumentModel();
            doc.SetText("ab");
            doc.Insert(new TextPosition(0, 1), "x\ny\nz");
            Assert.AreEqual(3, doc.LineCount);
            Assert.AreEqual("ax", doc.GetLine(0));
            Assert.AreEqual("y", doc.GetLine(1));
            Assert.AreEqual("zb", doc.GetLine(2));
        }

        [Test]
        public void Insert_AtEndOfLine()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            doc.Insert(new TextPosition(0, 5), " world");
            Assert.AreEqual("hello world", doc.GetLine(0));
        }

        [Test]
        public void Insert_Newline_SplitsLine()
        {
            var doc = new DocumentModel();
            doc.SetText("helloworld");
            doc.Insert(new TextPosition(0, 5), "\n");
            Assert.AreEqual(2, doc.LineCount);
            Assert.AreEqual("hello", doc.GetLine(0));
            Assert.AreEqual("world", doc.GetLine(1));
        }

        [Test]
        public void Delete_WithinSingleLine()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            doc.Delete(new TextRange(new TextPosition(0, 1), new TextPosition(0, 4)));
            Assert.AreEqual("ho", doc.GetLine(0));
        }

        [Test]
        public void Delete_AcrossLines()
        {
            var doc = new DocumentModel();
            doc.SetText("hello\nworld\nfoo");
            doc.Delete(new TextRange(new TextPosition(0, 3), new TextPosition(2, 1)));
            Assert.AreEqual(1, doc.LineCount);
            Assert.AreEqual("heloo", doc.GetLine(0));
        }

        [Test]
        public void Delete_EntireLine()
        {
            var doc = new DocumentModel();
            doc.SetText("aaa\nbbb\nccc");
            doc.Delete(new TextRange(new TextPosition(0, 3), new TextPosition(1, 3)));
            Assert.AreEqual(2, doc.LineCount);
            Assert.AreEqual("aaa", doc.GetLine(0));
            Assert.AreEqual("ccc", doc.GetLine(1));
        }

        [Test]
        public void Delete_EmptyRange_NoOp()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            doc.Delete(new TextRange(new TextPosition(0, 2), new TextPosition(0, 2)));
            Assert.AreEqual("hello", doc.GetLine(0));
        }

        [Test]
        public void Replace_Selection()
        {
            var doc = new DocumentModel();
            doc.SetText("hello world");
            doc.Replace(new TextRange(new TextPosition(0, 5), new TextPosition(0, 11)), "!");
            Assert.AreEqual("hello!", doc.GetLine(0));
        }

        [Test]
        public void GetText_Range_SingleLine()
        {
            var doc = new DocumentModel();
            doc.SetText("hello world");
            string text = doc.GetText(new TextRange(new TextPosition(0, 6), new TextPosition(0, 11)));
            Assert.AreEqual("world", text);
        }

        [Test]
        public void GetText_Range_MultiLine()
        {
            var doc = new DocumentModel();
            doc.SetText("aaa\nbbb\nccc");
            string text = doc.GetText(new TextRange(new TextPosition(0, 1), new TextPosition(2, 2)));
            Assert.AreEqual("aa\nbbb\ncc", text);
        }

        [Test]
        public void GetCharOffset_And_GetPosition_RoundTrip()
        {
            var doc = new DocumentModel();
            doc.SetText("abc\ndef\nghi");

            var pos = new TextPosition(1, 2);
            int offset = doc.GetCharOffset(pos);
            Assert.AreEqual(6, offset); // "abc\n" = 4, "de" = 2

            var back = doc.GetPosition(offset);
            Assert.AreEqual(pos, back);
        }

        [Test]
        public void GetPosition_AtStart()
        {
            var doc = new DocumentModel();
            doc.SetText("abc\ndef");
            Assert.AreEqual(TextPosition.Zero, doc.GetPosition(0));
        }

        [Test]
        public void GetPosition_BeyondEnd_ClampsToEnd()
        {
            var doc = new DocumentModel();
            doc.SetText("ab\ncd");
            var pos = doc.GetPosition(999);
            Assert.AreEqual(new TextPosition(1, 2), pos);
        }

        [Test]
        public void SetCursor_Clamps()
        {
            var doc = new DocumentModel();
            doc.SetText("hi");
            doc.SetCursor(new TextPosition(5, 10));
            Assert.AreEqual(new TextPosition(0, 2), doc.Cursor);
        }

        [Test]
        public void SetCursor_ExtendSelection()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            doc.SetCursor(new TextPosition(0, 1));
            doc.SetCursor(new TextPosition(0, 4), extendSelection: true);
            Assert.IsTrue(doc.HasSelection);
            Assert.AreEqual(new TextPosition(0, 1), doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(0, 4), doc.Cursor);
        }

        [Test]
        public void SelectAll()
        {
            var doc = new DocumentModel();
            doc.SetText("abc\ndef");
            doc.SelectAll();
            Assert.AreEqual(TextPosition.Zero, doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(1, 3), doc.Cursor);
        }



        [Test]
        public void Changed_Event_Fires_OnInsert()
        {
            var doc = new DocumentModel();
            DocumentChangeEventArgs? args = null;
            doc.Changed += e => args = e;
            doc.Insert(TextPosition.Zero, "hi");
            Assert.IsTrue(args.HasValue);
            Assert.AreEqual("hi", args.Value.InsertedText);
            Assert.AreEqual(1, args.Value.NewVersion);
        }

        [Test]
        public void Changed_Event_Fires_OnDelete()
        {
            var doc = new DocumentModel();
            doc.SetText("hello");
            DocumentChangeEventArgs? args = null;
            doc.Changed += e => args = e;
            doc.Delete(new TextRange(new TextPosition(0, 0), new TextPosition(0, 2)));
            Assert.IsTrue(args.HasValue);
            Assert.AreEqual(string.Empty, args.Value.InsertedText);
        }

        [Test]
        public void CursorMoved_Event_Fires()
        {
            var doc = new DocumentModel();
            doc.SetText("abc");
            TextPosition? moved = null;
            doc.CursorMoved += p => moved = p;
            doc.SetCursor(new TextPosition(0, 2));
            Assert.IsTrue(moved.HasValue);
            Assert.AreEqual(new TextPosition(0, 2), moved.Value);
        }

        [Test]
        public void Version_IncreasesOnMutation()
        {
            var doc = new DocumentModel();
            int v0 = doc.Version;
            doc.Insert(TextPosition.Zero, "a");
            Assert.Greater(doc.Version, v0);
        }
    }
}
