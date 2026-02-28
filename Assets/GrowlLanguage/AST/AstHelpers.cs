using System.Collections.Generic;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Non-node helper types used as fields inside AST nodes.
    // None of these extend GrowlNode — they are plain data containers.
    // ─────────────────────────────────────────────────────────────────────────

    // ── Type reference ────────────────────────────────────────────────────────

    /// <summary>A type name, optionally parameterised (e.g. List[int]).</summary>
    public sealed class TypeRef
    {
        public string         Name     { get; }
        public List<TypeRef>  TypeArgs { get; }

        public TypeRef(string name, List<TypeRef> typeArgs = null)
        {
            Name     = name;
            TypeArgs = typeArgs ?? new List<TypeRef>();
        }
    }

    // ── Function helpers ─────────────────────────────────────────────────────

    /// <summary>A single formal parameter in a function/lambda signature.</summary>
    public sealed class Param
    {
        public string    Name           { get; }
        public TypeRef   TypeAnnotation { get; }
        public GrowlNode DefaultValue  { get; }
        public bool      IsVariadic     { get; }
        public bool      IsKeyword      { get; }

        public Param(Token name,
                     TypeRef   typeAnnotation = null,
                     GrowlNode defaultValue   = null,
                     bool      isVariadic     = false,
                     bool      isKeyword      = false)
        {
            Name           = name.Value;
            TypeAnnotation = typeAnnotation;
            DefaultValue   = defaultValue;
            IsVariadic     = isVariadic;
            IsKeyword      = isKeyword;
        }
    }

    /// <summary>A single argument in a call expression (positional or named).</summary>
    public sealed class Argument
    {
        /// <summary>Keyword name for named arguments; null for positional.</summary>
        public string    Name  { get; }
        public GrowlNode Value { get; }

        public Argument(GrowlNode value, Token? name = null)
        {
            Value = value;
            Name  = name?.Value;
        }
    }

    /// <summary>A decorator applied to a function or class.</summary>
    public sealed class Decorator
    {
        public string          Name { get; }
        public List<GrowlNode> Args { get; }

        public Decorator(Token name, List<GrowlNode> args)
        {
            Name = name.Value;
            Args = args;
        }
    }

    // ── Comprehension helpers ─────────────────────────────────────────────────

    /// <summary>A single <c>for target in iterable [if filter]</c> clause.</summary>
    public sealed class ComprehensionClause
    {
        public List<string> Targets  { get; }
        public GrowlNode    Iterable { get; }
        public GrowlNode    Filter   { get; }

        public ComprehensionClause(List<string> targets, GrowlNode iterable, GrowlNode filter)
        {
            Targets  = targets;
            Iterable = iterable;
            Filter   = filter;
        }
    }

    // ── Dict helpers ──────────────────────────────────────────────────────────

    /// <summary>A single key-value pair in a dict literal.</summary>
    public sealed class DictEntry
    {
        public GrowlNode Key   { get; }
        public GrowlNode Value { get; }

        public DictEntry(GrowlNode key, GrowlNode value)
        {
            Key   = key;
            Value = value;
        }
    }

    // ── Control-flow helpers ──────────────────────────────────────────────────

    /// <summary>An <c>elif condition: body</c> clause inside an if statement.</summary>
    public sealed class ElifClause
    {
        public GrowlNode       Condition { get; }
        public List<GrowlNode> Body      { get; }

        public ElifClause(GrowlNode condition, List<GrowlNode> body)
        {
            Condition = condition;
            Body      = body;
        }
    }

    /// <summary>A single <c>case pattern [if guard]: body</c> arm in a match statement.</summary>
    public sealed class CaseClause
    {
        public GrowlNode       Pattern { get; }
        public GrowlNode       Guard   { get; }
        public List<GrowlNode> Body    { get; }

        public CaseClause(GrowlNode pattern, GrowlNode guard, List<GrowlNode> body)
        {
            Pattern = pattern;
            Guard   = guard;
            Body    = body;
        }
    }

    // ── Declaration helpers ───────────────────────────────────────────────────

    /// <summary>A field declaration inside a struct.</summary>
    public sealed class FieldDecl
    {
        public string    Name           { get; }
        public TypeRef   TypeAnnotation { get; }
        public GrowlNode DefaultValue   { get; }

        public FieldDecl(Token name, TypeRef typeAnnotation = null, GrowlNode defaultValue = null)
        {
            Name           = name.Value;
            TypeAnnotation = typeAnnotation;
            DefaultValue   = defaultValue;
        }
    }

    /// <summary>A variant/member inside an enum declaration.</summary>
    public sealed class EnumMember
    {
        public string          Name   { get; }
        public List<GrowlNode> Fields { get; }
        public GrowlNode       Value  { get; }

        public EnumMember(Token name, List<GrowlNode> fields = null, GrowlNode value = null)
        {
            Name   = name.Value;
            Fields = fields ?? new List<GrowlNode>();
            Value  = value;
        }
    }

    // ── Biological helpers ────────────────────────────────────────────────────

    /// <summary>A single rule inside an <c>adapt</c> block.</summary>
    public sealed class AdaptRule
    {
        public GrowlNode Condition { get; }
        public GrowlNode Action    { get; }

        public AdaptRule(GrowlNode condition, GrowlNode action)
        {
            Condition = condition;
            Action    = action;
        }
    }

    /// <summary>A named time-point inside a <c>cycle</c> block.</summary>
    public sealed class CyclePoint
    {
        public GrowlNode       At   { get; }
        public List<GrowlNode> Body { get; }

        public CyclePoint(GrowlNode at, List<GrowlNode> body)
        {
            At   = at;
            Body = body;
        }
    }
}
