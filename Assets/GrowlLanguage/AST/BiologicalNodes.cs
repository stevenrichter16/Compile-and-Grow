using System.Collections.Generic;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Biological construct node types unique to Growl.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A <c>gene</c> declaration — wraps a function that fulfils a named role.
    /// </summary>
    public sealed class GeneDecl : GrowlNode
    {
        public string  RoleName { get; }
        public bool    IsRole   { get; }
        public FnDecl  Fn       { get; }

        public GeneDecl(string roleName, bool isRole, FnDecl fn, int line, int col)
            : base(line, col)
        {
            RoleName = roleName;
            IsRole   = isRole;
            Fn       = fn;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitGene(this);
    }

    /// <summary>
    /// A <c>phase</c> block — body executes while the organism is in the
    /// named life-stage (optionally bounded by age range and a condition).
    /// </summary>
    public sealed class PhaseBlock : GrowlNode
    {
        public string          PhaseName { get; }
        public GrowlNode       MinAge    { get; }
        public GrowlNode       MaxAge    { get; }
        public GrowlNode       Condition { get; }
        public List<GrowlNode> Body      { get; }

        public PhaseBlock(string phaseName, GrowlNode minAge, GrowlNode maxAge,
                          GrowlNode condition, List<GrowlNode> body,
                          int line, int col)
            : base(line, col)
        {
            PhaseName = phaseName;
            MinAge    = minAge;
            MaxAge    = maxAge;
            Condition = condition;
            Body      = body;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitPhase(this);
    }

    /// <summary>
    /// A <c>when … then …</c> block — reactive rule triggered by a condition.
    /// </summary>
    public sealed class WhenBlock : GrowlNode
    {
        public GrowlNode       Condition  { get; }
        public List<GrowlNode> Body       { get; }
        public List<GrowlNode> ThenBlock  { get; }

        public WhenBlock(GrowlNode condition, List<GrowlNode> body,
                         List<GrowlNode> thenBlock, int line, int col)
            : base(line, col)
        {
            Condition = condition;
            Body      = body;
            ThenBlock = thenBlock;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitWhen(this);
    }

    /// <summary>
    /// A <c>respond to event [as binding]</c> block — event handler.
    /// </summary>
    public sealed class RespondBlock : GrowlNode
    {
        public string  EventName { get; }
        public string  Binding   { get; }
        public List<GrowlNode> Body { get; }

        public RespondBlock(string eventName, Token? binding, List<GrowlNode> body,
                            int line, int col)
            : base(line, col)
        {
            EventName = eventName;
            Binding   = binding?.Value;
            Body      = body;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitRespond(this);
    }

    /// <summary>
    /// An <c>adapt</c> block — resource-driven adaptive behaviour.
    /// </summary>
    public sealed class AdaptBlock : GrowlNode
    {
        public GrowlNode       Subject { get; }
        public List<AdaptRule> Rules   { get; }
        public GrowlNode       Budget  { get; }

        public AdaptBlock(GrowlNode subject, List<AdaptRule> rules, GrowlNode budget,
                          int line, int col)
            : base(line, col)
        {
            Subject = subject;
            Rules   = rules;
            Budget  = budget;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitAdapt(this);
    }

    /// <summary>
    /// A <c>cycle</c> block — periodic multi-phase behaviour over N ticks.
    /// </summary>
    public sealed class CycleBlock : GrowlNode
    {
        public string           Name   { get; }
        public GrowlNode        Period { get; }
        public List<CyclePoint> Points { get; }

        public CycleBlock(string name, GrowlNode period, List<CyclePoint> points,
                          int line, int col)
            : base(line, col)
        {
            Name   = name;
            Period = period;
            Points = points;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitCycle(this);
    }

    /// <summary>
    /// A <c>ticker</c> declaration — named timer that fires every N ticks.
    /// </summary>
    public sealed class TickerDecl : GrowlNode
    {
        public string          Name     { get; }
        public GrowlNode       Interval { get; }
        public List<GrowlNode> Body     { get; }

        public TickerDecl(string name, GrowlNode interval, List<GrowlNode> body,
                          int line, int col)
            : base(line, col)
        {
            Name     = name;
            Interval = interval;
            Body     = body;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitTicker(this);
    }
}
