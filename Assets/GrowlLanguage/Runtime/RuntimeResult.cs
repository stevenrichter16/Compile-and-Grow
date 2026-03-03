using System.Collections.Generic;
using GrowlLanguage.Analyzer;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Runtime
{
    public enum RuntimeMessageKind
    {
        ParseError,
        AnalyzeError,
        RuntimeError,
    }

    public readonly struct RuntimeMessage
    {
        public RuntimeMessageKind Kind { get; }
        public string Code { get; }
        public string Message { get; }
        public int Line { get; }
        public int Column { get; }

        public RuntimeMessage(RuntimeMessageKind kind, string code, string message, int line, int column)
        {
            Kind = kind;
            Code = string.IsNullOrEmpty(code) ? kind.ToString() : code;
            Message = message ?? string.Empty;
            Line = line;
            Column = column;
        }

        public override string ToString() => $"[{Line}:{Column}] {Code}: {Message}";
    }

    public sealed class RuntimeOptions
    {
        public bool AutoInvokeEntryFunction { get; set; }
        public string EntryFunctionName { get; set; } = "main";
        public int MaxLoopIterations { get; set; } = 100000;
        public IGrowlRuntimeHost Host { get; set; }
        public BiologicalContext BioContext { get; set; }
    }

    public sealed class RuntimeResult
    {
        public ParseResult ParseResult { get; }
        public AnalyzeResult AnalyzeResult { get; }
        public IReadOnlyList<RuntimeMessage> Messages { get; }
        public IReadOnlyList<string> OutputLines { get; }
        public object LastValue { get; }

        public bool Success => Messages.Count == 0;

        public RuntimeResult(
            ParseResult parseResult,
            AnalyzeResult analyzeResult,
            IReadOnlyList<RuntimeMessage> messages,
            IReadOnlyList<string> outputLines,
            object lastValue)
        {
            ParseResult = parseResult;
            AnalyzeResult = analyzeResult;
            Messages = messages ?? new List<RuntimeMessage>();
            OutputLines = outputLines ?? new List<string>();
            LastValue = lastValue;
        }
    }
}
