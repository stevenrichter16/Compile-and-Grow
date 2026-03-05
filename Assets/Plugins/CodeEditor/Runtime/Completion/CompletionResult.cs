using System.Collections.Generic;
using CodeEditor.Core;

namespace CodeEditor.Completion
{
    public sealed class CompletionResult
    {
        public static readonly CompletionResult Empty = new CompletionResult(
            new List<CompletionItem>(), new TextRange(TextPosition.Zero, TextPosition.Zero));

        public readonly IReadOnlyList<CompletionItem> Items;
        public readonly TextRange ReplacementRange;

        public CompletionResult(IReadOnlyList<CompletionItem> items, TextRange replacementRange)
        {
            Items = items;
            ReplacementRange = replacementRange;
        }
    }
}
