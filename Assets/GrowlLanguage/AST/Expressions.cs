using System;
using System.Collections.Generic;
using System.Globalization;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // All expression node types.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Literals ──────────────────────────────────────────────────────────────

    public sealed class IntegerLiteralExpr : GrowlNode
    {
        public long Value { get; }

        public IntegerLiteralExpr(Token token) : base(token.Line, token.Column)
        {
            string raw = token.Value.Replace("_", "");
            if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                Value = Convert.ToInt64(raw.Substring(2), 16);
            else if (raw.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                Value = Convert.ToInt64(raw.Substring(2), 2);
            else
                Value = long.Parse(raw);
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitIntegerLiteral(this);
    }

    public sealed class FloatLiteralExpr : GrowlNode
    {
        public double  Value { get; }
        public string  Unit  { get; }

        public FloatLiteralExpr(Token token) : base(token.Line, token.Column)
        {
            Value = double.Parse(token.Value.Replace("_", ""), CultureInfo.InvariantCulture);
            Unit  = token.Unit;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitFloatLiteral(this);
    }

    public sealed class StringLiteralExpr : GrowlNode
    {
        public string Value { get; }

        public StringLiteralExpr(Token token) : base(token.Line, token.Column)
        {
            Value = token.Value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitStringLiteral(this);
    }

    public sealed class BoolLiteralExpr : GrowlNode
    {
        public bool Value { get; }

        public BoolLiteralExpr(Token token) : base(token.Line, token.Column)
        {
            Value = token.Type == TokenType.True;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitBoolLiteral(this);
    }

    public sealed class NoneLiteralExpr : GrowlNode
    {
        public NoneLiteralExpr(Token token) : base(token.Line, token.Column) { }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitNoneLiteral(this);
    }

    public sealed class ColorLiteralExpr : GrowlNode
    {
        public string Hex { get; }

        public ColorLiteralExpr(Token token) : base(token.Line, token.Column)
        {
            Hex = token.Value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitColorLiteral(this);
    }

    public sealed class InterpolatedStringExpr : GrowlNode
    {
        public List<GrowlNode> Segments { get; }

        public InterpolatedStringExpr(List<GrowlNode> segments, int line, int col)
            : base(line, col)
        {
            Segments = segments;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitInterpolatedStr(this);
    }

    // ── Core Expressions ─────────────────────────────────────────────────────

    public sealed class NameExpr : GrowlNode
    {
        public string Name { get; }

        public NameExpr(Token token) : base(token.Line, token.Column)
        {
            Name = token.Value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitName(this);
    }

    public sealed class BinaryExpr : GrowlNode
    {
        public GrowlNode Left  { get; }
        public Token     Op    { get; }
        public GrowlNode Right { get; }

        public BinaryExpr(GrowlNode left, Token op, GrowlNode right)
            : base(left.Line, left.Column)
        {
            Left  = left;
            Op    = op;
            Right = right;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitBinary(this);
    }

    public sealed class UnaryExpr : GrowlNode
    {
        public Token     Op      { get; }
        public GrowlNode Operand { get; }

        public UnaryExpr(Token op, GrowlNode operand) : base(op.Line, op.Column)
        {
            Op      = op;
            Operand = operand;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitUnary(this);
    }

    public sealed class TernaryExpr : GrowlNode
    {
        public GrowlNode ThenExpr  { get; }
        public GrowlNode Condition { get; }
        public GrowlNode ElseExpr  { get; }

        public TernaryExpr(GrowlNode thenExpr, GrowlNode condition, GrowlNode elseExpr)
            : base(thenExpr.Line, thenExpr.Column)
        {
            ThenExpr  = thenExpr;
            Condition = condition;
            ElseExpr  = elseExpr;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitTernary(this);
    }

    public sealed class CallExpr : GrowlNode
    {
        public GrowlNode      Callee { get; }
        public List<Argument> Args   { get; }

        public CallExpr(GrowlNode callee, List<Argument> args, int line, int col)
            : base(line, col)
        {
            Callee = callee;
            Args   = args;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitCall(this);
    }

    public sealed class AttributeExpr : GrowlNode
    {
        public GrowlNode Object    { get; }
        public string    FieldName { get; }

        public AttributeExpr(GrowlNode obj, Token field)
            : base(obj.Line, obj.Column)
        {
            Object    = obj;
            FieldName = field.Value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitAttribute(this);
    }

    public sealed class SubscriptExpr : GrowlNode
    {
        public GrowlNode Object { get; }
        public GrowlNode Key    { get; }

        public SubscriptExpr(GrowlNode obj, GrowlNode key)
            : base(obj.Line, obj.Column)
        {
            Object = obj;
            Key    = key;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitSubscript(this);
    }

    public sealed class LambdaExpr : GrowlNode
    {
        public List<Param> Params { get; }
        public GrowlNode   Body   { get; }

        public LambdaExpr(List<Param> @params, GrowlNode body, int line, int col)
            : base(line, col)
        {
            Params = @params;
            Body   = body;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitLambda(this);
    }

    // ── Collection Expressions ────────────────────────────────────────────────

    public sealed class ListExpr : GrowlNode
    {
        public List<GrowlNode> Elements { get; }

        public ListExpr(List<GrowlNode> elements, int line, int col)
            : base(line, col)
        {
            Elements = elements;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitList(this);
    }

    public sealed class ListComprehensionExpr : GrowlNode
    {
        public GrowlNode                  Element { get; }
        public List<ComprehensionClause>  Clauses { get; }

        public ListComprehensionExpr(GrowlNode element,
                                     List<ComprehensionClause> clauses,
                                     int line, int col)
            : base(line, col)
        {
            Element = element;
            Clauses = clauses;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitListComprehension(this);
    }

    public sealed class DictExpr : GrowlNode
    {
        public List<DictEntry> Entries { get; }

        public DictExpr(List<DictEntry> entries, int line, int col) : base(line, col)
        {
            Entries = entries;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitDict(this);
    }

    public sealed class DictComprehensionExpr : GrowlNode
    {
        public GrowlNode                 Key     { get; }
        public GrowlNode                 Value   { get; }
        public List<ComprehensionClause> Clauses { get; }

        public DictComprehensionExpr(GrowlNode key, GrowlNode value,
                                     List<ComprehensionClause> clauses,
                                     int line, int col)
            : base(line, col)
        {
            Key     = key;
            Value   = value;
            Clauses = clauses;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitDictComprehension(this);
    }

    public sealed class SetExpr : GrowlNode
    {
        public List<GrowlNode> Elements { get; }

        public SetExpr(List<GrowlNode> elements, int line, int col) : base(line, col)
        {
            Elements = elements;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitSet(this);
    }

    public sealed class TupleExpr : GrowlNode
    {
        public List<GrowlNode> Elements { get; }

        public TupleExpr(List<GrowlNode> elements, int line, int col) : base(line, col)
        {
            Elements = elements;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitTuple(this);
    }

    public sealed class RangeExpr : GrowlNode
    {
        public GrowlNode Start     { get; }
        public GrowlNode End       { get; }
        public bool      Inclusive { get; }
        public GrowlNode Step      { get; }

        public RangeExpr(GrowlNode start, GrowlNode end, bool inclusive,
                         GrowlNode step, int line, int col)
            : base(line, col)
        {
            Start     = start;
            End       = end;
            Inclusive = inclusive;
            Step      = step;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitRange(this);
    }

    public sealed class VectorExpr : GrowlNode
    {
        public GrowlNode X { get; }
        public GrowlNode Y { get; }
        public GrowlNode Z { get; }

        public VectorExpr(GrowlNode x, GrowlNode y, GrowlNode z, int line, int col)
            : base(line, col)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitVector(this);
    }

    public sealed class SpreadExpr : GrowlNode
    {
        public GrowlNode Operand { get; }

        public SpreadExpr(GrowlNode operand, int line, int col) : base(line, col)
        {
            Operand = operand;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitSpread(this);
    }
}
