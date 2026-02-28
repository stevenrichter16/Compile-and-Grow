namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Abstract base for every node in a Growl AST.
    //
    // Every concrete node implements Accept<T>(IGrowlVisitor<T>) which calls
    // exactly one method on the visitor, enabling type-safe double dispatch
    // without any runtime casting.
    // ─────────────────────────────────────────────────────────────────────────
    public abstract class GrowlNode
    {
        /// <summary>1-based source line where this node begins.</summary>
        public int Line   { get; }

        /// <summary>1-based source column where this node begins.</summary>
        public int Column { get; }

        protected GrowlNode(int line, int column)
        {
            Line   = line;
            Column = column;
        }

        /// <summary>
        /// Double-dispatch entry point.  Each concrete node calls the single
        /// corresponding Visit method on <paramref name="visitor"/>.
        /// </summary>
        public abstract T Accept<T>(IGrowlVisitor<T> visitor);
    }
}
