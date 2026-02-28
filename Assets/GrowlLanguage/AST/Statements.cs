using System.Collections.Generic;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Statement and control-flow node types.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Assignment ────────────────────────────────────────────────────────────

    public sealed class AssignStmt : GrowlNode
    {
        public GrowlNode Target { get; }
        public Token     Op     { get; }
        public GrowlNode Value  { get; }

        public AssignStmt(GrowlNode target, Token op, GrowlNode value)
            : base(target.Line, target.Column)
        {
            Target = target;
            Op     = op;
            Value  = value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitAssign(this);
    }

    // ── Expression statement ─────────────────────────────────────────────────

    public sealed class ExprStmt : GrowlNode
    {
        public GrowlNode Expression { get; }

        public ExprStmt(GrowlNode expression) : base(expression.Line, expression.Column)
        {
            Expression = expression;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitExprStmt(this);
    }

    // ── Jump statements ───────────────────────────────────────────────────────

    public sealed class ReturnStmt : GrowlNode
    {
        public GrowlNode Value { get; }

        public ReturnStmt(Token keyword, GrowlNode value) : base(keyword.Line, keyword.Column)
        {
            Value = value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitReturn(this);
    }

    public sealed class BreakStmt : GrowlNode
    {
        public BreakStmt(Token keyword) : base(keyword.Line, keyword.Column) { }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitBreak(this);
    }

    public sealed class ContinueStmt : GrowlNode
    {
        public ContinueStmt(Token keyword) : base(keyword.Line, keyword.Column) { }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitContinue(this);
    }

    public sealed class YieldStmt : GrowlNode
    {
        public GrowlNode Value { get; }

        public YieldStmt(Token keyword, GrowlNode value) : base(keyword.Line, keyword.Column)
        {
            Value = value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitYield(this);
    }

    // ── Timing statements ─────────────────────────────────────────────────────

    public sealed class WaitStmt : GrowlNode
    {
        public GrowlNode Ticks { get; }

        public WaitStmt(Token keyword, GrowlNode ticks) : base(keyword.Line, keyword.Column)
        {
            Ticks = ticks;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitWait(this);
    }

    public sealed class DeferStmt : GrowlNode
    {
        public GrowlNode       Duration { get; }
        public bool            IsUntil  { get; }
        public List<GrowlNode> Body     { get; }

        public DeferStmt(GrowlNode duration, bool isUntil, List<GrowlNode> body, int line, int col)
            : base(line, col)
        {
            Duration = duration;
            IsUntil  = isUntil;
            Body     = body;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitDefer(this);
    }

    // ── Mutation statement ────────────────────────────────────────────────────

    public sealed class MutateStmt : GrowlNode
    {
        public GrowlNode Target   { get; }
        public GrowlNode Value    { get; }
        public GrowlNode Interval { get; }

        public MutateStmt(GrowlNode target, GrowlNode value, GrowlNode interval, int line, int col)
            : base(line, col)
        {
            Target   = target;
            Value    = value;
            Interval = interval;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitMutate(this);
    }

    // ── Control flow ─────────────────────────────────────────────────────────

    public sealed class IfStmt : GrowlNode
    {
        public GrowlNode        Condition   { get; }
        public List<GrowlNode>  ThenBody    { get; }
        public List<ElifClause> ElifClauses { get; }
        public List<GrowlNode>  ElseBody    { get; }

        public IfStmt(GrowlNode condition, List<GrowlNode> thenBody,
                      List<ElifClause> elifClauses, List<GrowlNode> elseBody,
                      int line, int col)
            : base(line, col)
        {
            Condition   = condition;
            ThenBody    = thenBody;
            ElifClauses = elifClauses;
            ElseBody    = elseBody;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitIf(this);
    }

    public sealed class ForStmt : GrowlNode
    {
        public List<string>    Targets  { get; }
        public GrowlNode       Iterable { get; }
        public List<GrowlNode> Body     { get; }
        public List<GrowlNode> ElseBody { get; }

        public ForStmt(List<string> targets, GrowlNode iterable,
                       List<GrowlNode> body, List<GrowlNode> elseBody,
                       int line, int col)
            : base(line, col)
        {
            Targets  = targets;
            Iterable = iterable;
            Body     = body;
            ElseBody = elseBody;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitFor(this);
    }

    public sealed class WhileStmt : GrowlNode
    {
        public GrowlNode       Condition { get; }
        public List<GrowlNode> Body      { get; }

        public WhileStmt(GrowlNode condition, List<GrowlNode> body, int line, int col)
            : base(line, col)
        {
            Condition = condition;
            Body      = body;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitWhile(this);
    }

    public sealed class LoopStmt : GrowlNode
    {
        public List<GrowlNode> Body { get; }

        public LoopStmt(List<GrowlNode> body, int line, int col) : base(line, col)
        {
            Body = body;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitLoop(this);
    }

    public sealed class MatchStmt : GrowlNode
    {
        public GrowlNode       Subject { get; }
        public List<CaseClause> Cases  { get; }

        public MatchStmt(GrowlNode subject, List<CaseClause> cases, int line, int col)
            : base(line, col)
        {
            Subject = subject;
            Cases   = cases;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitMatch(this);
    }

    public sealed class TryStmt : GrowlNode
    {
        public List<GrowlNode> TryBody     { get; }
        public string          ErrorName   { get; }
        public List<GrowlNode> RecoverBody { get; }
        public List<GrowlNode> AlwaysBody  { get; }

        public TryStmt(List<GrowlNode> tryBody, Token errorName,
                       List<GrowlNode> recoverBody, List<GrowlNode> alwaysBody,
                       int line, int col)
            : base(line, col)
        {
            TryBody     = tryBody;
            ErrorName   = errorName.Value;
            RecoverBody = recoverBody;
            AlwaysBody  = alwaysBody;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitTry(this);
    }

    // ── Import / Module ───────────────────────────────────────────────────────

    public sealed class ImportStmt : GrowlNode
    {
        public List<string>  ModulePath { get; }
        public string        Alias      { get; }
        public List<string>  Names      { get; }

        public ImportStmt(List<string> modulePath, string alias, List<string> names,
                          int line, int col)
            : base(line, col)
        {
            ModulePath = modulePath;
            Alias      = alias;
            Names      = names;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitImport(this);
    }

    public sealed class ModuleDecl : GrowlNode
    {
        public string Name { get; }

        public ModuleDecl(Token name, int line, int col) : base(line, col)
        {
            Name = name.Value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitModule(this);
    }
}
