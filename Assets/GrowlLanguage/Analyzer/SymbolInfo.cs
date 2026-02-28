using GrowlLanguage.AST;

namespace GrowlLanguage.Analyzer
{
    /// <summary>
    /// Symbol-table entry.
    /// </summary>
    public sealed class SymbolInfo
    {
        public string Name { get; }
        public SymbolKind Kind { get; }
        public int DeclLine { get; }
        public int DeclColumn { get; }
        public GrowlNode Declaration { get; }
        public TypeSymbol Type { get; set; }

        public SymbolInfo(
            string name,
            SymbolKind kind,
            int declLine,
            int declColumn,
            GrowlNode declaration = null,
            TypeSymbol type = null)
        {
            Name = name;
            Kind = kind;
            DeclLine = declLine;
            DeclColumn = declColumn;
            Declaration = declaration;
            Type = type ?? TypeSymbol.Unknown;
        }
    }
}
