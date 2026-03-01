using System;
using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    public readonly struct TerminalTextEditResult
    {
        public string Text { get; }
        public int CursorIndex { get; }
        public int SelectIndex { get; }

        public TerminalTextEditResult(string text, int cursorIndex, int selectIndex)
        {
            Text = text ?? string.Empty;
            CursorIndex = cursorIndex;
            SelectIndex = selectIndex;
        }
    }

    public static class GrowlTerminalTextOps
    {
        public static TerminalTextEditResult ApplyEnterAutoIndent(
            string source,
            int selectionStart,
            int selectionEnd,
            int indentSize)
        {
            string working = source ?? string.Empty;
            int start = Clamp(selectionStart, 0, working.Length);
            int end = Clamp(selectionEnd, 0, working.Length);
            if (end < start)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            if (end > start)
                working = working.Remove(start, end - start);

            int lineStart = GetLineStartInText(working, start);
            int referenceLength = Math.Max(0, start - lineStart);
            string referenceLine = referenceLength > 0
                ? working.Substring(lineStart, referenceLength)
                : string.Empty;

            int leadingSpaces = CountLeadingSpaces(working, lineStart, referenceLength);
            bool shouldIndentBlock = referenceLine.TrimEnd().EndsWith(":", StringComparison.Ordinal);
            int indentCount = leadingSpaces + (shouldIndentBlock ? Math.Max(1, indentSize) : 0);

            string insertion = "\n" + new string(' ', indentCount);
            working = working.Insert(start, insertion);

            int newCaret = start + insertion.Length;
            return new TerminalTextEditResult(working, newCaret, newCaret);
        }

        public static TerminalTextEditResult ApplyTabIndentation(
            string source,
            int cursorIndex,
            int selectIndex,
            int indentSize,
            bool outdent)
        {
            string working = source ?? string.Empty;
            int cursor = Clamp(cursorIndex, 0, working.Length);
            int select = Clamp(selectIndex, 0, working.Length);
            int spaces = Math.Max(1, indentSize);

            int selectionStart = Math.Min(cursor, select);
            int selectionEnd = Math.Max(cursor, select);
            bool reverseSelection = cursor < select;

            if (SelectionSpansMultipleLines(working, selectionStart, selectionEnd))
            {
                var lineStarts = BuildLineStartCache(working);
                int firstLine = GetLineIndexAtPosition(lineStarts, working.Length, selectionStart);
                int lastLine = GetLineIndexAtPosition(lineStarts, working.Length, Math.Max(selectionStart, selectionEnd - 1));

                var targetLineStarts = new List<int>(Math.Max(0, lastLine - firstLine + 1));
                for (int i = firstLine; i <= lastLine; i++)
                    targetLineStarts.Add(lineStarts[i]);

                int offset = 0;
                int start = selectionStart;
                int end = selectionEnd;

                for (int i = 0; i < targetLineStarts.Count; i++)
                {
                    int currentStart = targetLineStarts[i] + offset;

                    if (!outdent)
                    {
                        working = working.Insert(currentStart, new string(' ', spaces));
                        offset += spaces;

                        if (currentStart <= start)
                            start += spaces;
                        if (currentStart < end)
                            end += spaces;
                    }
                    else
                    {
                        int removable = CountLeadingSpaces(working, currentStart, spaces);
                        if (removable <= 0)
                            continue;

                        working = working.Remove(currentStart, removable);
                        offset -= removable;

                        if (currentStart < start)
                            start = Math.Max(currentStart, start - removable);
                        if (currentStart < end)
                            end = Math.Max(currentStart, end - removable);
                    }
                }

                if (reverseSelection)
                    return new TerminalTextEditResult(working, start, end);
                return new TerminalTextEditResult(working, end, start);
            }

            int lineStartSingle = GetLineStartInText(working, cursor);
            if (!outdent)
            {
                string indent = new string(' ', spaces);
                working = working.Insert(lineStartSingle, indent);

                if (cursor >= lineStartSingle)
                    cursor += indent.Length;
                if (select >= lineStartSingle)
                    select += indent.Length;
            }
            else
            {
                int removable = CountLeadingSpaces(working, lineStartSingle, spaces);
                if (removable > 0)
                {
                    working = working.Remove(lineStartSingle, removable);

                    if (cursor > lineStartSingle)
                        cursor = Math.Max(lineStartSingle, cursor - removable);
                    if (select > lineStartSingle)
                        select = Math.Max(lineStartSingle, select - removable);
                }
            }

            return new TerminalTextEditResult(working, cursor, cursor);
        }

        public static List<int> BuildLineStartCache(string source)
        {
            var lineStarts = new List<int> { 0 };
            string text = source ?? string.Empty;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n' && i + 1 <= text.Length)
                    lineStarts.Add(i + 1);
            }

            if (lineStarts.Count == 0)
                lineStarts.Add(0);

            return lineStarts;
        }

        public static int GetLineIndexAtPosition(IReadOnlyList<int> lineStarts, int textLength, int position)
        {
            if (lineStarts == null || lineStarts.Count == 0)
                return 0;

            int clamped = Clamp(position, 0, Math.Max(0, textLength));

            int lo = 0;
            int hi = lineStarts.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                int start = lineStarts[mid];
                if (start == clamped)
                    return mid;
                if (start < clamped)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return Math.Max(0, lo - 1);
        }

        private static bool SelectionSpansMultipleLines(string source, int selectionStart, int selectionEnd)
        {
            if (selectionStart == selectionEnd)
                return false;

            var lineStarts = BuildLineStartCache(source);
            int endProbe = Math.Max(selectionStart, selectionEnd - 1);
            return GetLineIndexAtPosition(lineStarts, (source ?? string.Empty).Length, selectionStart) !=
                   GetLineIndexAtPosition(lineStarts, (source ?? string.Empty).Length, endProbe);
        }

        private static int GetLineStartInText(string source, int index)
        {
            string safe = source ?? string.Empty;
            if (safe.Length == 0)
                return 0;

            int clamped = Clamp(index, 0, safe.Length);
            int probe = Math.Max(0, clamped - 1);
            int previousNewline = safe.LastIndexOf('\n', probe);
            return previousNewline < 0 ? 0 : previousNewline + 1;
        }

        private static int CountLeadingSpaces(string source, int lineStart, int maxSpaces)
        {
            string safe = source ?? string.Empty;
            int limit = Math.Max(0, maxSpaces);
            int removable = 0;
            while (removable < limit &&
                   lineStart + removable < safe.Length &&
                   safe[lineStart + removable] == ' ')
            {
                removable++;
            }
            return removable;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
