using System.Collections.Generic;

namespace GrowlLanguage.Analyzer
{
    /// <summary>
    /// Lexical scope for semantic analysis.
    /// </summary>
    public sealed class Scope
    {
        private readonly Dictionary<string, SymbolInfo> _symbols =
            new Dictionary<string, SymbolInfo>();

        public Scope Parent { get; }
        public int Depth { get; }
        public string Name { get; }

        public IReadOnlyDictionary<string, SymbolInfo> Symbols => _symbols;

        public Scope(Scope parent, string name = null)
        {
            Parent = parent;
            Name = name ?? "scope";
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        public bool TryDeclare(SymbolInfo symbol)
        {
            if (symbol == null || string.IsNullOrEmpty(symbol.Name))
                return false;

            if (_symbols.ContainsKey(symbol.Name))
                return false;

            _symbols[symbol.Name] = symbol;
            return true;
        }

        public bool TryResolve(string name, out SymbolInfo symbol)
        {
            symbol = null;
            if (string.IsNullOrEmpty(name))
                return false;

            for (Scope s = this; s != null; s = s.Parent)
            {
                if (s._symbols.TryGetValue(name, out symbol))
                    return true;
            }

            return false;
        }

        public Scope CreateChild(string name = null) => new Scope(this, name);
    }
}
