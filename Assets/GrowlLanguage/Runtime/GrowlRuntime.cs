using System.Collections.Generic;
using GrowlLanguage.Analyzer;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Runtime
{
    public static class GrowlRuntime
    {
        public static RuntimeResult Execute(string source, RuntimeOptions options = null)
        {
            options = options ?? new RuntimeOptions();
            var messages = new List<RuntimeMessage>();
            var outputLines = new List<string>();

            ParseResult parse = Parser.Parser.Parse(source ?? string.Empty);
            if (parse.HasErrors)
            {
                foreach (var err in parse.Errors)
                {
                    messages.Add(new RuntimeMessage(
                        RuntimeMessageKind.ParseError,
                        nameof(RuntimeMessageKind.ParseError),
                        err.Message,
                        err.Line,
                        err.Column));
                }

                return new RuntimeResult(parse, analyzeResult: null, messages, outputLines, lastValue: null);
            }

            AnalyzeResult analyze = SemanticAnalyzer.Analyze(parse.Program);
            if (analyze.HasErrors)
            {
                foreach (var err in analyze.Errors)
                {
                    messages.Add(new RuntimeMessage(
                        RuntimeMessageKind.AnalyzeError,
                        err.Code.ToString(),
                        err.Message,
                        err.Line,
                        err.Column));
                }

                return new RuntimeResult(parse, analyze, messages, outputLines, lastValue: null);
            }

            object lastValue = null;
            try
            {
                var interpreter = new Interpreter(options, outputLines);
                interpreter.SetSource(source);
                lastValue = interpreter.Execute(parse.Program);
            }
            catch (RuntimeExecutionException ex)
            {
                messages.Add(new RuntimeMessage(
                    RuntimeMessageKind.RuntimeError,
                    nameof(RuntimeMessageKind.RuntimeError),
                    ex.Message,
                    ex.Line,
                    ex.Column));
            }

            return new RuntimeResult(parse, analyze, messages, outputLines, lastValue);
        }
    }
}
