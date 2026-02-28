using System.Collections.Generic;

namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Root node of every parsed Growl source file.
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class ProgramNode : GrowlNode
    {
        public List<GrowlNode> Statements { get; }

        public ProgramNode(List<GrowlNode> statements) : base(1, 1)
        {
            Statements = statements;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitProgram(this);
    }
}
