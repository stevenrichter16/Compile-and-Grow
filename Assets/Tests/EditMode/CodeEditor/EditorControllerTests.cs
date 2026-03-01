using NUnit.Framework;
using CodeEditor.Core;
using CodeEditor.Commands;
using CodeEditor.Editor;
using CodeEditor.Language;

namespace CodeEditor.Tests
{
    [TestFixture]
    public class EditorControllerTests
    {
        private DocumentModel _doc;
        private EditorController _ctrl;

        [SetUp]
        public void SetUp()
        {
            _doc = new DocumentModel();
            _ctrl = new EditorController(_doc, new EditorConfig { IndentSize = 4 });
        }

        [Test]
        public void TypeCharacter_InsertsAtCursor()
        {
            _ctrl.TypeCharacter('a');
            Assert.AreEqual("a", _doc.GetText());
            Assert.AreEqual(new TextPosition(0, 1), _doc.Cursor);
        }

        [Test]
        public void TypeCharacter_Sequence()
        {
            _ctrl.TypeCharacter('h');
            _ctrl.TypeCharacter('i');
            Assert.AreEqual("hi", _doc.GetText());
            Assert.AreEqual(new TextPosition(0, 2), _doc.Cursor);
        }

        [Test]
        public void TypeCharacter_ReplacesSelection()
        {
            _ctrl.SetText("hello");
            _doc.SetSelection(new TextPosition(0, 1), new TextPosition(0, 4));
            _ctrl.TypeCharacter('X');
            Assert.AreEqual("hXo", _doc.GetText());
        }

        [Test]
        public void Enter_InsertsNewline()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 5));
            _ctrl.Enter();
            Assert.AreEqual(2, _doc.LineCount);
            Assert.AreEqual("hello", _doc.GetLine(0));
            Assert.AreEqual(string.Empty, _doc.GetLine(1));
        }

        [Test]
        public void Enter_AutoIndent_KeepsCurrentLevel()
        {
            _ctrl.SetText("    hello");
            _doc.SetCursor(new TextPosition(0, 9));
            _ctrl.Enter();
            Assert.AreEqual("    hello", _doc.GetLine(0));
            Assert.AreEqual("    ", _doc.GetLine(1));
        }

        [Test]
        public void Enter_AutoIndent_WithLanguageService_IncreasesIndent()
        {
            var svc = new MockLanguageService(shouldIndent: true);
            _ctrl.SetLanguageService(svc);
            _ctrl.SetText("    if x:");
            _doc.SetCursor(new TextPosition(0, 9));
            _ctrl.Enter();
            Assert.AreEqual("        ", _doc.GetLine(1));
        }

        [Test]
        public void Backspace_DeletesCharBeforeCursor()
        {
            _ctrl.SetText("abc");
            _doc.SetCursor(new TextPosition(0, 3));
            _ctrl.Backspace();
            Assert.AreEqual("ab", _doc.GetText());
        }

        [Test]
        public void Backspace_AtLineStart_JoinsWithPrevious()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(1, 0));
            _ctrl.Backspace();
            Assert.AreEqual(1, _doc.LineCount);
            Assert.AreEqual("abcdef", _doc.GetLine(0));
        }

        [Test]
        public void Backspace_AtDocStart_NoOp()
        {
            _ctrl.SetText("abc");
            _doc.SetCursor(TextPosition.Zero);
            _ctrl.Backspace();
            Assert.AreEqual("abc", _doc.GetText());
        }

        [Test]
        public void Backspace_WithSelection_DeletesSelection()
        {
            _ctrl.SetText("hello");
            _doc.SetSelection(new TextPosition(0, 1), new TextPosition(0, 4));
            _ctrl.Backspace();
            Assert.AreEqual("ho", _doc.GetText());
        }

        [Test]
        public void Delete_DeletesCharAfterCursor()
        {
            _ctrl.SetText("abc");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.Delete();
            Assert.AreEqual("ac", _doc.GetText());
        }

        [Test]
        public void Delete_AtLineEnd_JoinsWithNext()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(0, 3));
            _ctrl.Delete();
            Assert.AreEqual(1, _doc.LineCount);
            Assert.AreEqual("abcdef", _doc.GetLine(0));
        }

        [Test]
        public void Delete_AtDocEnd_NoOp()
        {
            _ctrl.SetText("abc");
            _doc.SetCursor(new TextPosition(0, 3));
            _ctrl.Delete();
            Assert.AreEqual("abc", _doc.GetText());
        }

        [Test]
        public void Tab_InsertsSpaces()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 0));
            _ctrl.Tab(shift: false);
            Assert.AreEqual("    hello", _doc.GetText());
        }

        [Test]
        public void Tab_AtColumn2_InsertsTwoSpaces()
        {
            _ctrl.SetText("ab");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.Tab(shift: false);
            // 4 - (2 % 4) = 2 spaces to next tab stop
            Assert.AreEqual("ab  ", _doc.GetText());
        }

        [Test]
        public void ShiftTab_SingleLine_Outdents()
        {
            _ctrl.SetText("    hello");
            _doc.SetCursor(new TextPosition(0, 5));
            _ctrl.Tab(shift: true);
            Assert.AreEqual("hello", _doc.GetText());
        }

        [Test]
        public void Tab_MultiLineSelection_IndentsAll()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetSelection(new TextPosition(0, 0), new TextPosition(2, 3));
            _ctrl.Tab(shift: false);
            Assert.AreEqual("    aaa", _doc.GetLine(0));
            Assert.AreEqual("    bbb", _doc.GetLine(1));
            Assert.AreEqual("    ccc", _doc.GetLine(2));
        }

        [Test]
        public void Undo_RevertsLastAction()
        {
            _ctrl.TypeCharacter('a');
            _ctrl.TypeCharacter('b');
            _ctrl.Undo();
            // "ab" was batched, so undo removes both
            Assert.AreEqual(string.Empty, _doc.GetText());
        }

        [Test]
        public void Redo_ReappliesUndoneAction()
        {
            _ctrl.TypeCharacter('x');
            _ctrl.Undo();
            _ctrl.Redo();
            Assert.AreEqual("x", _doc.GetText());
        }

        [Test]
        public void SelectAll_SelectsEntireDocument()
        {
            _ctrl.SetText("abc\ndef");
            _ctrl.SelectAll();
            Assert.AreEqual(TextPosition.Zero, _doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(1, 3), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Left()
        {
            _ctrl.SetText("abc");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.MoveCursor(MoveDirection.Left, shift: false);
            Assert.AreEqual(new TextPosition(0, 1), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Right()
        {
            _ctrl.SetText("abc");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.MoveCursor(MoveDirection.Right, shift: false);
            Assert.AreEqual(new TextPosition(0, 2), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Up()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(1, 2));
            _ctrl.MoveCursor(MoveDirection.Up, shift: false);
            Assert.AreEqual(new TextPosition(0, 2), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Down()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.MoveCursor(MoveDirection.Down, shift: false);
            Assert.AreEqual(new TextPosition(1, 2), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Home()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 3));
            _ctrl.MoveCursor(MoveDirection.Home, shift: false);
            Assert.AreEqual(new TextPosition(0, 0), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_End()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 0));
            _ctrl.MoveCursor(MoveDirection.End, shift: false);
            Assert.AreEqual(new TextPosition(0, 5), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Left_WithSelection_CollapsesToStart()
        {
            _ctrl.SetText("hello");
            _doc.SetSelection(new TextPosition(0, 1), new TextPosition(0, 4));
            _ctrl.MoveCursor(MoveDirection.Left, shift: false);
            Assert.AreEqual(new TextPosition(0, 1), _doc.Cursor);
            Assert.IsFalse(_doc.HasSelection);
        }

        [Test]
        public void MoveCursor_Right_WithSelection_CollapsesToEnd()
        {
            _ctrl.SetText("hello");
            _doc.SetSelection(new TextPosition(0, 1), new TextPosition(0, 4));
            _ctrl.MoveCursor(MoveDirection.Right, shift: false);
            Assert.AreEqual(new TextPosition(0, 4), _doc.Cursor);
            Assert.IsFalse(_doc.HasSelection);
        }

        [Test]
        public void MoveCursor_Shift_ExtendsSelection()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.MoveCursor(MoveDirection.Right, shift: true);
            _ctrl.MoveCursor(MoveDirection.Right, shift: true);
            Assert.IsTrue(_doc.HasSelection);
            Assert.AreEqual(new TextPosition(0, 2), _doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(0, 4), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_WordRight()
        {
            _ctrl.SetText("hello world foo");
            _doc.SetCursor(new TextPosition(0, 0));
            _ctrl.MoveCursor(MoveDirection.Right, shift: false, word: true);
            Assert.AreEqual(new TextPosition(0, 6), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_WordLeft()
        {
            _ctrl.SetText("hello world");
            _doc.SetCursor(new TextPosition(0, 11));
            _ctrl.MoveCursor(MoveDirection.Left, shift: false, word: true);
            Assert.AreEqual(new TextPosition(0, 6), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Left_AtLineStart_GoesToPreviousLine()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(1, 0));
            _ctrl.MoveCursor(MoveDirection.Left, shift: false);
            Assert.AreEqual(new TextPosition(0, 3), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_Right_AtLineEnd_GoesToNextLine()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(0, 3));
            _ctrl.MoveCursor(MoveDirection.Right, shift: false);
            Assert.AreEqual(new TextPosition(1, 0), _doc.Cursor);
        }

        [Test]
        public void MoveCursor_DocumentStart()
        {
            _ctrl.SetText("abc\ndef\nghi");
            _doc.SetCursor(new TextPosition(2, 2));
            _ctrl.MoveCursor(MoveDirection.DocumentStart, shift: false);
            Assert.AreEqual(TextPosition.Zero, _doc.Cursor);
        }

        [Test]
        public void MoveCursor_DocumentEnd()
        {
            _ctrl.SetText("abc\ndef\nghi");
            _doc.SetCursor(TextPosition.Zero);
            _ctrl.MoveCursor(MoveDirection.DocumentEnd, shift: false);
            Assert.AreEqual(new TextPosition(2, 3), _doc.Cursor);
        }

        [Test]
        public void Cut_RemovesSelection()
        {
            _ctrl.SetText("hello world");
            _doc.SetSelection(new TextPosition(0, 5), new TextPosition(0, 11));
            string cut = _ctrl.GetSelectedText();
            Assert.AreEqual(" world", cut);
            _ctrl.Cut();
            Assert.AreEqual("hello", _doc.GetText());
        }

        [Test]
        public void Paste_InsertsText()
        {
            _ctrl.SetText("ab");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.Paste("XY");
            Assert.AreEqual("aXYb", _doc.GetText());
        }

        [Test]
        public void Paste_ReplacesSelection()
        {
            _ctrl.SetText("hello");
            _doc.SetSelection(new TextPosition(0, 1), new TextPosition(0, 4));
            _ctrl.Paste("XY");
            Assert.AreEqual("hXYo", _doc.GetText());
        }

        [Test]
        public void TypeText_MultiLine_Paste()
        {
            _ctrl.SetText("ab");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.TypeText("X\nY\nZ");
            Assert.AreEqual(3, _doc.LineCount);
            Assert.AreEqual("aX", _doc.GetLine(0));
            Assert.AreEqual("Y", _doc.GetLine(1));
            Assert.AreEqual("Zb", _doc.GetLine(2));
        }

        [Test]
        public void MoveCursor_Down_ClampsColumn()
        {
            _ctrl.SetText("longline\nhi");
            _doc.SetCursor(new TextPosition(0, 7));
            _ctrl.MoveCursor(MoveDirection.Down, shift: false);
            // "hi" is only 2 chars, so column clamps to 2
            Assert.AreEqual(new TextPosition(1, 2), _doc.Cursor);
        }

        [Test]
        public void SetText_ClearsHistory()
        {
            _ctrl.TypeCharacter('a');
            _ctrl.SetText("fresh");
            Assert.IsFalse(_ctrl.History.CanUndo);
        }

        // === Acceptance criteria: cursor position after every operation ===

        [Test]
        public void AC1_ArrowUp_MovesCursorPosition()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetCursor(new TextPosition(2, 1));
            _ctrl.MoveCursor(MoveDirection.Up, shift: false);
            Assert.AreEqual(new TextPosition(1, 1), _doc.Cursor);
            _ctrl.MoveCursor(MoveDirection.Up, shift: false);
            Assert.AreEqual(new TextPosition(0, 1), _doc.Cursor);
        }

        [Test]
        public void AC1_ArrowDown_MovesCursorPosition()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.MoveCursor(MoveDirection.Down, shift: false);
            Assert.AreEqual(new TextPosition(1, 1), _doc.Cursor);
            _ctrl.MoveCursor(MoveDirection.Down, shift: false);
            Assert.AreEqual(new TextPosition(2, 1), _doc.Cursor);
        }

        [Test]
        public void AC1_ArrowUp_AtFirstLine_GoesToLineStart()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.MoveCursor(MoveDirection.Up, shift: false);
            Assert.AreEqual(new TextPosition(0, 0), _doc.Cursor);
        }

        [Test]
        public void AC1_ArrowDown_AtLastLine_GoesToLineEnd()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(1, 1));
            _ctrl.MoveCursor(MoveDirection.Down, shift: false);
            Assert.AreEqual(new TextPosition(1, 3), _doc.Cursor);
        }

        [Test]
        public void AC5_PageUp_MovesUpByPageScrollLines()
        {
            var lines = new string[50];
            for (int i = 0; i < 50; i++) lines[i] = "line" + i;
            _ctrl.SetText(string.Join("\n", lines));
            _doc.SetCursor(new TextPosition(30, 2));
            _ctrl.MoveCursor(MoveDirection.PageUp, shift: false);
            Assert.AreEqual(new TextPosition(10, 2), _doc.Cursor); // 30 - 20 = 10
        }

        [Test]
        public void AC5_PageDown_MovesDownByPageScrollLines()
        {
            var lines = new string[50];
            for (int i = 0; i < 50; i++) lines[i] = "line" + i;
            _ctrl.SetText(string.Join("\n", lines));
            _doc.SetCursor(new TextPosition(10, 2));
            _ctrl.MoveCursor(MoveDirection.PageDown, shift: false);
            Assert.AreEqual(new TextPosition(30, 2), _doc.Cursor); // 10 + 20 = 30
        }

        [Test]
        public void AC5_PageUp_ClampsToFirstLine()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetCursor(new TextPosition(1, 0));
            _ctrl.MoveCursor(MoveDirection.PageUp, shift: false);
            Assert.AreEqual(0, _doc.Cursor.Line);
        }

        [Test]
        public void AC5_PageDown_ClampsToLastLine()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetCursor(new TextPosition(1, 0));
            _ctrl.MoveCursor(MoveDirection.PageDown, shift: false);
            Assert.AreEqual(2, _doc.Cursor.Line);
        }

        [Test]
        public void AC7_ShiftDown_CreatesMultiLineSelection()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.MoveCursor(MoveDirection.Down, shift: true);
            Assert.IsTrue(_doc.HasSelection);
            Assert.AreEqual(new TextPosition(0, 1), _doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(1, 1), _doc.Cursor);
            Assert.IsTrue(_doc.SelectionRange.SpansMultipleLines);
        }

        [Test]
        public void AC8_ShiftHome_SelectsToLineStart()
        {
            _ctrl.SetText("hello world");
            _doc.SetCursor(new TextPosition(0, 5));
            _ctrl.MoveCursor(MoveDirection.Home, shift: true);
            Assert.IsTrue(_doc.HasSelection);
            Assert.AreEqual(new TextPosition(0, 5), _doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(0, 0), _doc.Cursor);
        }

        [Test]
        public void AC8_ShiftEnd_SelectsToLineEnd()
        {
            _ctrl.SetText("hello world");
            _doc.SetCursor(new TextPosition(0, 5));
            _ctrl.MoveCursor(MoveDirection.End, shift: true);
            Assert.IsTrue(_doc.HasSelection);
            Assert.AreEqual(new TextPosition(0, 5), _doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(0, 11), _doc.Cursor);
        }

        [Test]
        public void AC9_ShiftDown_MultiLine_ExtendsSelection()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.MoveCursor(MoveDirection.Down, shift: true);
            _ctrl.MoveCursor(MoveDirection.Down, shift: true);
            Assert.AreEqual(new TextPosition(0, 2), _doc.SelectionAnchor);
            Assert.AreEqual(new TextPosition(2, 2), _doc.Cursor);
        }

        [Test]
        public void AC11_ArrowClearsSelection()
        {
            _ctrl.SetText("hello");
            _doc.SetSelection(new TextPosition(0, 1), new TextPosition(0, 4));
            Assert.IsTrue(_doc.HasSelection);
            _ctrl.MoveCursor(MoveDirection.Right, shift: false);
            Assert.IsFalse(_doc.HasSelection);
        }

        [Test]
        public void AC12_TypeCharacter_MovesCursorForward()
        {
            _ctrl.SetText("ab");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.TypeCharacter('X');
            Assert.AreEqual(new TextPosition(0, 2), _doc.Cursor);
        }

        [Test]
        public void AC13_Backspace_MovesCursorBack()
        {
            _ctrl.SetText("abc");
            _doc.SetCursor(new TextPosition(0, 3));
            _ctrl.Backspace();
            Assert.AreEqual(new TextPosition(0, 2), _doc.Cursor);
        }

        [Test]
        public void AC13_Backspace_AtLineStart_MovesCursorToEndOfPreviousLine()
        {
            _ctrl.SetText("abc\ndef");
            _doc.SetCursor(new TextPosition(1, 0));
            _ctrl.Backspace();
            Assert.AreEqual(new TextPosition(0, 3), _doc.Cursor);
        }

        [Test]
        public void AC14_Enter_MovesCursorToIndentedNewLine()
        {
            _ctrl.SetText("    hello");
            _doc.SetCursor(new TextPosition(0, 9));
            _ctrl.Enter();
            Assert.AreEqual(new TextPosition(1, 4), _doc.Cursor); // 4 spaces of indent
        }

        [Test]
        public void AC15_Tab_MovesCursorToNextTabStop()
        {
            _ctrl.SetText("ab");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.Tab(shift: false);
            // 4 - (2 % 4) = 2 spaces inserted, cursor at column 4
            Assert.AreEqual(new TextPosition(0, 4), _doc.Cursor);
        }

        [Test]
        public void AC16_Paste_MovesCursorToEndOfPastedText()
        {
            _ctrl.SetText("ab");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.Paste("XYZ");
            Assert.AreEqual(new TextPosition(0, 4), _doc.Cursor); // after "aXYZb", cursor at 4
        }

        [Test]
        public void AC16_Paste_MultiLine_MovesCursorToEndOfPastedText()
        {
            _ctrl.SetText("ab");
            _doc.SetCursor(new TextPosition(0, 1));
            _ctrl.Paste("X\nY");
            Assert.AreEqual(new TextPosition(1, 1), _doc.Cursor);
        }

        [Test]
        public void AC17_Undo_RestoresCursorPosition()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 5));
            _ctrl.TypeCharacter('!');
            Assert.AreEqual(new TextPosition(0, 6), _doc.Cursor);
            _ctrl.Undo();
            Assert.AreEqual(new TextPosition(0, 5), _doc.Cursor);
        }

        [Test]
        public void AC17_Redo_RestoresCursorPosition()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 5));
            _ctrl.TypeCharacter('!');
            _ctrl.Undo();
            _ctrl.Redo();
            Assert.AreEqual(new TextPosition(0, 6), _doc.Cursor);
        }

        [Test]
        public void AC6_CursorLine_TracksAfterMovement()
        {
            _ctrl.SetText("aaa\nbbb\nccc");
            _doc.SetCursor(new TextPosition(0, 0));
            Assert.AreEqual(0, _doc.Cursor.Line);
            _ctrl.MoveCursor(MoveDirection.Down, shift: false);
            Assert.AreEqual(1, _doc.Cursor.Line);
            _ctrl.MoveCursor(MoveDirection.Down, shift: false);
            Assert.AreEqual(2, _doc.Cursor.Line);
            _ctrl.MoveCursor(MoveDirection.Up, shift: false);
            Assert.AreEqual(1, _doc.Cursor.Line);
        }

        // ── Shift+Tab (Outdent) comprehensive tests ──

        [Test]
        public void ShiftTab_AfterTab_RestoresOriginalText()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 0));
            _ctrl.Tab(shift: false);
            Assert.AreEqual("    hello", _doc.GetLine(0), "Tab should indent");

            _ctrl.Tab(shift: true);
            Assert.AreEqual("hello", _doc.GetLine(0), "Shift+Tab should restore original text");
        }

        [Test]
        public void ShiftTab_AfterTab_RestoresCursorToStartOfText()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 0));
            _ctrl.Tab(shift: false);
            Assert.AreEqual(new TextPosition(0, 4), _doc.Cursor, "Tab should move cursor to col 4");

            _ctrl.Tab(shift: true);
            Assert.AreEqual(new TextPosition(0, 0), _doc.Cursor, "Shift+Tab should move cursor to col 0");
        }

        [Test]
        public void ShiftTab_CursorInMiddleOfIndent_MovesToZero()
        {
            _ctrl.SetText("    hello");
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.Tab(shift: true);
            Assert.AreEqual("hello", _doc.GetLine(0));
            Assert.AreEqual(new TextPosition(0, 0), _doc.Cursor);
        }

        [Test]
        public void ShiftTab_CursorAfterIndent_AdjustsColumnByIndentSize()
        {
            _ctrl.SetText("    hello");
            _doc.SetCursor(new TextPosition(0, 7)); // on 'l'
            _ctrl.Tab(shift: true);
            Assert.AreEqual("hello", _doc.GetLine(0));
            Assert.AreEqual(new TextPosition(0, 3), _doc.Cursor); // 7 - 4 = 3
        }

        [Test]
        public void ShiftTab_NoIndent_TextUnchanged()
        {
            _ctrl.SetText("hello");
            _doc.SetCursor(new TextPosition(0, 3));
            int versionBefore = _doc.Version;
            _ctrl.Tab(shift: true);
            Assert.AreEqual("hello", _doc.GetLine(0));
            // Version should not change when there's nothing to outdent
            Assert.AreEqual(versionBefore, _doc.Version, "Version should not change when nothing to outdent");
        }

        [Test]
        public void ShiftTab_PartialIndent_RemovesOnlyAvailableSpaces()
        {
            _ctrl.SetText("  hello"); // only 2 spaces
            _doc.SetCursor(new TextPosition(0, 2));
            _ctrl.Tab(shift: true);
            Assert.AreEqual("hello", _doc.GetLine(0));
            Assert.AreEqual(new TextPosition(0, 0), _doc.Cursor);
        }

        [Test]
        public void ShiftTab_MultiLine_OutdentsAllLines()
        {
            _ctrl.SetText("    aaa\n    bbb\n    ccc");
            _doc.SetSelection(new TextPosition(0, 0), new TextPosition(2, 7));
            _ctrl.Tab(shift: true);
            Assert.AreEqual("aaa", _doc.GetLine(0));
            Assert.AreEqual("bbb", _doc.GetLine(1));
            Assert.AreEqual("ccc", _doc.GetLine(2));
        }

        [Test]
        public void ShiftTab_VersionIncrements_WhenTextChanges()
        {
            _ctrl.SetText("    hello");
            _doc.SetCursor(new TextPosition(0, 4));
            int versionBefore = _doc.Version;
            _ctrl.Tab(shift: true);
            Assert.Greater(_doc.Version, versionBefore, "Version must increment after outdent");
        }

        [Test]
        public void ShiftTab_ThenUndo_RestoresIndent()
        {
            _ctrl.SetText("    hello");
            _doc.SetCursor(new TextPosition(0, 4));
            _ctrl.Tab(shift: true);
            Assert.AreEqual("hello", _doc.GetLine(0));
            _ctrl.Undo();
            Assert.AreEqual("    hello", _doc.GetLine(0));
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
