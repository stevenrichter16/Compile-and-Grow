using System.Collections.Generic;
using GrowlLanguage.AST;

namespace GrowlLanguage.Analyzer
{
    /// <summary>
    /// Semantic-analysis output. Scope and binding maps are intentionally
    /// exposed so tests can assert resolver behavior.
    /// </summary>
    public sealed class AnalyzeResult
    {
        public ProgramNode Program { get; }
        public List<AnalyzeError> Errors { get; }
        public Scope GlobalScope { get; }
        public Dictionary<GrowlNode, Scope> ScopeByNode { get; }
        public Dictionary<NameExpr, SymbolInfo> NameBindings { get; }
        public Dictionary<GrowlNode, TypeSymbol> ExpressionTypes { get; }

        public bool HasErrors => Errors.Count > 0;

        public AnalyzeResult(
            ProgramNode program,
            List<AnalyzeError> errors,
            Scope globalScope,
            Dictionary<GrowlNode, Scope> scopeByNode,
            Dictionary<NameExpr, SymbolInfo> nameBindings,
            Dictionary<GrowlNode, TypeSymbol> expressionTypes)
        {
            Program = program;
            Errors = errors ?? new List<AnalyzeError>();
            GlobalScope = globalScope;
            ScopeByNode = scopeByNode ?? new Dictionary<GrowlNode, Scope>();
            NameBindings = nameBindings ?? new Dictionary<NameExpr, SymbolInfo>();
            ExpressionTypes = expressionTypes ?? new Dictionary<GrowlNode, TypeSymbol>();
        }
    }
}
