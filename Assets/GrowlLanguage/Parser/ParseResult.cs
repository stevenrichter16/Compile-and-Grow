using System.Collections.Generic;
using GrowlLanguage.AST;

namespace GrowlLanguage.Parser
{
    /// <summary>
    /// The complete output of a parse pass: AST root plus parser diagnostics.
    /// </summary>
    public sealed class ParseResult
    {
        public readonly ProgramNode Program;
        public readonly List<ParseError> Errors;

        public bool HasErrors => Errors.Count > 0;

        public ParseResult(ProgramNode program, List<ParseError> errors)
        {
            Program = program;
            Errors = errors;
        }
    }
}
