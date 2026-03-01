using System;
using CodeEditor.Language;

namespace CodeEditor.Core
{
    public static class TextOperations
    {
        public static int GetIndentLevel(string line)
        {
            if (line == null) return 0;
            int count = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ' || line[i] == '\t') count++;
                else break;
            }
            return count;
        }

        public static string ComputeAutoIndent(string currentLineText, int indentSize, ILanguageService languageService = null)
        {
            int currentIndent = GetIndentLevel(currentLineText);
            bool shouldIncrease = languageService != null && languageService.ShouldIndentAfterLine(currentLineText);
            int newIndent = shouldIncrease ? currentIndent + indentSize : currentIndent;
            return new string(' ', newIndent);
        }

        public static void IndentLines(DocumentModel doc, int firstLine, int lastLine, int indentSize, bool outdent)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            firstLine = Math.Max(0, firstLine);
            lastLine = Math.Min(lastLine, doc.LineCount - 1);
            string indentStr = new string(' ', indentSize);

            for (int i = firstLine; i <= lastLine; i++)
            {
                string line = doc.GetLine(i);
                if (outdent)
                {
                    int spaces = Math.Min(indentSize, GetIndentLevel(line));
                    if (spaces > 0)
                        doc.Delete(new TextRange(new TextPosition(i, 0), new TextPosition(i, spaces)));
                }
                else
                {
                    doc.Insert(new TextPosition(i, 0), indentStr);
                }
            }
        }

        public static TextPosition ClampPosition(DocumentModel doc, TextPosition pos)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            int line = Math.Max(0, Math.Min(pos.Line, doc.LineCount - 1));
            int col = Math.Max(0, Math.Min(pos.Column, doc.GetLineLength(line)));
            return new TextPosition(line, col);
        }
    }
}
