using NUnit.Framework;
using CodeEditor.Core;
using CodeEditor.Commands;

namespace CodeEditor.Tests
{
    [TestFixture]
    public class CommandHistoryTests
    {
        private DocumentModel _doc;
        private CommandHistory _history;

        [SetUp]
        public void SetUp()
        {
            _doc = new DocumentModel();
            _history = new CommandHistory();
        }

        [Test]
        public void Execute_InsertsText()
        {
            _history.Execute(new InsertTextCommand(TextPosition.Zero, "hello"), _doc);
            Assert.AreEqual("hello", _doc.GetText());
        }

        [Test]
        public void Undo_RevertsInsert()
        {
            _history.Execute(new InsertTextCommand(TextPosition.Zero, "hello"), _doc);
            _history.Undo(_doc);
            Assert.AreEqual(string.Empty, _doc.GetText());
        }

        [Test]
        public void Redo_ReappliesInsert()
        {
            _history.Execute(new InsertTextCommand(TextPosition.Zero, "hello"), _doc);
            _history.Undo(_doc);
            _history.Redo(_doc);
            Assert.AreEqual("hello", _doc.GetText());
        }

        [Test]
        public void Undo_ThenNewEdit_ClearsRedo()
        {
            _history.Execute(new InsertTextCommand(TextPosition.Zero, "a"), _doc);
            _history.Undo(_doc);
            Assert.IsTrue(_history.CanRedo);
            _history.Execute(new InsertTextCommand(TextPosition.Zero, "b"), _doc);
            Assert.IsFalse(_history.CanRedo);
        }

        [Test]
        public void ConsecutiveSingleCharInserts_MergeIntoBatch()
        {
            _history.Execute(new InsertTextCommand(new TextPosition(0, 0), "h"), _doc);
            _history.Execute(new InsertTextCommand(new TextPosition(0, 1), "e"), _doc);
            _history.Execute(new InsertTextCommand(new TextPosition(0, 2), "l"), _doc);
            Assert.AreEqual("hel", _doc.GetText());

            // Single undo should remove all merged characters
            _history.Undo(_doc);
            Assert.AreEqual(string.Empty, _doc.GetText());
        }

        [Test]
        public void WhitespaceAfterNonWhitespace_BreaksBatch()
        {
            _history.Execute(new InsertTextCommand(new TextPosition(0, 0), "a"), _doc);
            _history.Execute(new InsertTextCommand(new TextPosition(0, 1), "b"), _doc);
            _history.Execute(new InsertTextCommand(new TextPosition(0, 2), " "), _doc);
            Assert.AreEqual("ab ", _doc.GetText());

            // Undo removes only the space (batch was broken)
            _history.Undo(_doc);
            Assert.AreEqual("ab", _doc.GetText());

            // Second undo removes "ab"
            _history.Undo(_doc);
            Assert.AreEqual(string.Empty, _doc.GetText());
        }

        [Test]
        public void BreakBatch_ForcesNewUndoGroup()
        {
            _history.Execute(new InsertTextCommand(new TextPosition(0, 0), "a"), _doc);
            _history.Execute(new InsertTextCommand(new TextPosition(0, 1), "b"), _doc);
            _history.BreakBatch();
            _history.Execute(new InsertTextCommand(new TextPosition(0, 2), "c"), _doc);

            _history.Undo(_doc);
            Assert.AreEqual("ab", _doc.GetText());
        }

        [Test]
        public void DeleteCommand_UndoRestoresText()
        {
            _doc.SetText("hello");
            _history.Execute(new DeleteTextCommand(new TextRange(new TextPosition(0, 1), new TextPosition(0, 4))), _doc);
            Assert.AreEqual("ho", _doc.GetText());

            _history.Undo(_doc);
            Assert.AreEqual("hello", _doc.GetText());
        }

        [Test]
        public void IndentCommand_IncreasesIndent()
        {
            _doc.SetText("hello");
            _history.Execute(new IndentCommand(0, 0, 4, outdent: false), _doc);
            Assert.AreEqual("    hello", _doc.GetText());
        }

        [Test]
        public void IndentCommand_Undo_RestoresOriginal()
        {
            _doc.SetText("hello");
            _history.Execute(new IndentCommand(0, 0, 4, outdent: false), _doc);
            _history.Undo(_doc);
            Assert.AreEqual("hello", _doc.GetText());
        }

        [Test]
        public void IndentCommand_Outdent()
        {
            _doc.SetText("    hello");
            _history.Execute(new IndentCommand(0, 0, 4, outdent: true), _doc);
            Assert.AreEqual("hello", _doc.GetText());
        }

        [Test]
        public void InsertWithSelection_ReplacesSelectedText()
        {
            _doc.SetText("hello world");
            _doc.SetSelection(new TextPosition(0, 5), new TextPosition(0, 11));
            var sel = _doc.SelectionRange;
            _history.Execute(new InsertTextCommand(sel.Start, "!", sel), _doc);
            Assert.AreEqual("hello!", _doc.GetText());
        }

        [Test]
        public void InsertWithSelection_Undo_RestoresSelection()
        {
            _doc.SetText("hello world");
            _doc.SetSelection(new TextPosition(0, 5), new TextPosition(0, 11));
            var sel = _doc.SelectionRange;
            _history.Execute(new InsertTextCommand(sel.Start, "!", sel), _doc);
            _history.Undo(_doc);
            Assert.AreEqual("hello world", _doc.GetText());
        }

        [Test]
        public void MultipleUndoRedo_Consistency()
        {
            _history.Execute(new InsertTextCommand(new TextPosition(0, 0), "aaa"), _doc);
            _history.Execute(new InsertTextCommand(new TextPosition(0, 3), "\nbbb"), _doc);
            Assert.AreEqual("aaa\nbbb", _doc.GetText());

            _history.Undo(_doc);
            Assert.AreEqual("aaa", _doc.GetText());

            _history.Undo(_doc);
            Assert.AreEqual(string.Empty, _doc.GetText());

            _history.Redo(_doc);
            Assert.AreEqual("aaa", _doc.GetText());

            _history.Redo(_doc);
            Assert.AreEqual("aaa\nbbb", _doc.GetText());
        }

        [Test]
        public void CanUndo_CanRedo_Correct()
        {
            Assert.IsFalse(_history.CanUndo);
            Assert.IsFalse(_history.CanRedo);

            _history.Execute(new InsertTextCommand(TextPosition.Zero, "x"), _doc);
            Assert.IsTrue(_history.CanUndo);
            Assert.IsFalse(_history.CanRedo);

            _history.Undo(_doc);
            Assert.IsFalse(_history.CanUndo);
            Assert.IsTrue(_history.CanRedo);
        }

        [Test]
        public void Clear_ResetsHistory()
        {
            _history.Execute(new InsertTextCommand(TextPosition.Zero, "x"), _doc);
            _history.Clear();
            Assert.IsFalse(_history.CanUndo);
            Assert.IsFalse(_history.CanRedo);
        }
    }
}
