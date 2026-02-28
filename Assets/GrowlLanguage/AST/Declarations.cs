using System.Collections.Generic;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Declaration node types (fn, class, struct, enum, trait, mixin, const,
    // type alias).
    // ─────────────────────────────────────────────────────────────────────────

    public sealed class FnDecl : GrowlNode
    {
        public string          Name       { get; }
        public List<Param>     Params     { get; }
        public TypeRef         ReturnType { get; }
        public List<GrowlNode> Body       { get; }
        public List<Decorator> Decorators { get; }

        public FnDecl(Token name, List<Param> @params, TypeRef returnType,
                      List<GrowlNode> body, List<Decorator> decorators,
                      int line, int col)
            : base(line, col)
        {
            Name       = name.Value;
            Params     = @params;
            ReturnType = returnType;
            Body       = body;
            Decorators = decorators;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitFn(this);
    }

    public sealed class ClassDecl : GrowlNode
    {
        public string          Name       { get; }
        public TypeRef         Superclass { get; }
        public List<TypeRef>   Traits     { get; }
        public List<TypeRef>   Mixins     { get; }
        public List<GrowlNode> Members    { get; }
        public bool            IsAbstract { get; }
        public List<Decorator> Decorators { get; }

        public ClassDecl(Token name, TypeRef superclass,
                         List<TypeRef> traits, List<TypeRef> mixins,
                         List<GrowlNode> members, bool isAbstract,
                         List<Decorator> decorators,
                         int line, int col)
            : base(line, col)
        {
            Name       = name.Value;
            Superclass = superclass;
            Traits     = traits;
            Mixins     = mixins;
            Members    = members;
            IsAbstract = isAbstract;
            Decorators = decorators;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitClass(this);
    }

    public sealed class StructDecl : GrowlNode
    {
        public string          Name    { get; }
        public List<FieldDecl> Fields  { get; }
        public List<FnDecl>    Methods { get; }

        public StructDecl(Token name, List<FieldDecl> fields, List<FnDecl> methods,
                          int line, int col)
            : base(line, col)
        {
            Name    = name.Value;
            Fields  = fields;
            Methods = methods;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitStruct(this);
    }

    public sealed class EnumDecl : GrowlNode
    {
        public string           Name        { get; }
        public List<TypeRef>    SuperTraits { get; }
        public List<EnumMember> Members     { get; }
        public List<FnDecl>     Methods     { get; }

        public EnumDecl(Token name, List<TypeRef> superTraits,
                        List<EnumMember> members, List<FnDecl> methods,
                        int line, int col)
            : base(line, col)
        {
            Name        = name.Value;
            SuperTraits = superTraits;
            Members     = members;
            Methods     = methods;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitEnum(this);
    }

    public sealed class TraitDecl : GrowlNode
    {
        public string          Name    { get; }
        public List<GrowlNode> Members { get; }

        public TraitDecl(Token name, List<GrowlNode> members, int line, int col)
            : base(line, col)
        {
            Name    = name.Value;
            Members = members;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitTrait(this);
    }

    public sealed class MixinDecl : GrowlNode
    {
        public string       Name    { get; }
        public List<FnDecl> Methods { get; }

        public MixinDecl(Token name, List<FnDecl> methods, int line, int col)
            : base(line, col)
        {
            Name    = name.Value;
            Methods = methods;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitMixin(this);
    }

    public sealed class ConstDecl : GrowlNode
    {
        public string    Name           { get; }
        public TypeRef   TypeAnnotation { get; }
        public GrowlNode Value          { get; }

        public ConstDecl(Token name, TypeRef typeAnnotation, GrowlNode value,
                         int line, int col)
            : base(line, col)
        {
            Name           = name.Value;
            TypeAnnotation = typeAnnotation;
            Value          = value;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitConst(this);
    }

    public sealed class TypeAliasDecl : GrowlNode
    {
        public string        Name       { get; }
        public TypeRef       Target     { get; }
        public List<TypeRef> TypeParams { get; }

        public TypeAliasDecl(Token name, TypeRef target, List<TypeRef> typeParams,
                             int line, int col)
            : base(line, col)
        {
            Name       = name.Value;
            Target     = target;
            TypeParams = typeParams;
        }

        public override T Accept<T>(IGrowlVisitor<T> visitor) => visitor.VisitTypeAlias(this);
    }
}
