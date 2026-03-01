using NUnit.Framework;
using GrowlLanguage.Runtime;

namespace GrowlLanguage.Tests.Runtime
{
    [TestFixture]
    public class GrowlTerminalTextOpsTests
    {
        [Test]
        public void EnterAfterColon_InsertsSingleNewlineAndIndent()
        {
            string source = "if ready:\n    run()";
            int caret = "if ready:".Length;

            TerminalTextEditResult result = GrowlTerminalTextOps.ApplyEnterAutoIndent(
                source,
                caret,
                caret,
                indentSize: 4);

            Assert.That(result.Text, Is.EqualTo("if ready:\n    \n    run()"));
            Assert.That(result.CursorIndex, Is.EqualTo("if ready:\n    ".Length));
            Assert.That(result.SelectIndex, Is.EqualTo(result.CursorIndex));
        }

        [Test]
        public void EnterAfterIndentedLine_PreservesBaseIndent()
        {
            string source = "fn main():\n    energy = 1";
            int caret = "fn main():\n    energy = 1".Length;

            TerminalTextEditResult result = GrowlTerminalTextOps.ApplyEnterAutoIndent(
                source,
                caret,
                caret,
                indentSize: 4);

            Assert.That(result.Text, Is.EqualTo("fn main():\n    energy = 1\n    "));
            Assert.That(result.CursorIndex, Is.EqualTo(result.Text.Length));
        }

        [Test]
        public void TabOnSingleLine_InsertsIndentAtLineStart()
        {
            string source = "value = 10";
            int caret = source.Length;

            TerminalTextEditResult result = GrowlTerminalTextOps.ApplyTabIndentation(
                source,
                caret,
                caret,
                indentSize: 4,
                outdent: false);

            Assert.That(result.Text, Is.EqualTo("    value = 10"));
            Assert.That(result.CursorIndex, Is.EqualTo(caret + 4));
            Assert.That(result.SelectIndex, Is.EqualTo(result.CursorIndex));
        }

        [Test]
        public void ShiftTabOnSingleLine_RemovesUpToIndentSizeSpaces()
        {
            string source = "    value = 10";
            int caret = source.Length;

            TerminalTextEditResult result = GrowlTerminalTextOps.ApplyTabIndentation(
                source,
                caret,
                caret,
                indentSize: 4,
                outdent: true);

            Assert.That(result.Text, Is.EqualTo("value = 10"));
            Assert.That(result.SelectIndex, Is.EqualTo(result.CursorIndex));
        }

        [Test]
        public void TabOnMultiLineSelection_IndentsAllSelectedLines()
        {
            string source = "a = 1\nb = 2\nc = 3";
            int selectionStart = 0;
            int selectionEnd = "a = 1\nb = 2".Length;

            TerminalTextEditResult result = GrowlTerminalTextOps.ApplyTabIndentation(
                source,
                selectionEnd,
                selectionStart,
                indentSize: 4,
                outdent: false);

            Assert.That(result.Text, Is.EqualTo("    a = 1\n    b = 2\nc = 3"));
        }

        [Test]
        public void ShiftTabOnMultiLineSelection_OutdentsAllSelectedLines()
        {
            string source = "    a = 1\n    b = 2\nc = 3";
            int selectionStart = 0;
            int selectionEnd = "    a = 1\n    b = 2".Length;

            TerminalTextEditResult result = GrowlTerminalTextOps.ApplyTabIndentation(
                source,
                selectionEnd,
                selectionStart,
                indentSize: 4,
                outdent: true);

            Assert.That(result.Text, Is.EqualTo("a = 1\nb = 2\nc = 3"));
        }

        [Test]
        public void LineIndexMapping_ResolvesExpectedLine()
        {
            string source = "one\ntwo\nthree";
            var starts = GrowlTerminalTextOps.BuildLineStartCache(source);

            Assert.That(GrowlTerminalTextOps.GetLineIndexAtPosition(starts, source.Length, 0), Is.EqualTo(0));
            Assert.That(GrowlTerminalTextOps.GetLineIndexAtPosition(starts, source.Length, 5), Is.EqualTo(1));
            Assert.That(GrowlTerminalTextOps.GetLineIndexAtPosition(starts, source.Length, source.Length), Is.EqualTo(2));
        }
    }
}
