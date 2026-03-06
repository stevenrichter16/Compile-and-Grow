using System.Collections.Generic;
using GrowlLanguage.AST;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.Analyzer
{
    public sealed class SemanticAnalyzer
    {
        public static AnalyzeResult Analyze(string source)
        {
            var parse = Parser.Parser.Parse(source);
            var result = Analyze(parse.Program);

            if (parse.HasErrors)
            {
                foreach (var err in parse.Errors)
                {
                    result.Errors.Add(new AnalyzeError(
                        AnalyzeErrorCode.ParseError,
                        err.Message,
                        err.Line,
                        err.Column));
                }
            }

            return result;
        }

        public static AnalyzeResult Analyze(ProgramNode program)
        {
            var global = new Scope(parent: null, name: "global");
            SeedBuiltinSymbols(global);
            var errors = new List<AnalyzeError>();
            var scopeByNode = new Dictionary<GrowlNode, Scope>();
            var nameBindings = new Dictionary<NameExpr, SymbolInfo>();
            var expressionTypes = new Dictionary<GrowlNode, TypeSymbol>();

            var walker = new Walker(global, errors, scopeByNode, nameBindings, expressionTypes);
            walker.AnalyzeProgram(program);
            return new AnalyzeResult(program, errors, global, scopeByNode, nameBindings, expressionTypes);
        }

        private static void SeedBuiltinSymbols(Scope global)
        {
            DeclareBuiltinFunction(global, "print", TypeSymbol.None);
            DeclareBuiltinFunction(global, "log", TypeSymbol.None);
            DeclareBuiltinFunction(global, "len", TypeSymbol.Int);
            DeclareBuiltinFunction(global, "type", TypeSymbol.String);

            DeclareBuiltinFunction(global, "world_get", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "world_set", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "world_add", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "org_get", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "org_set", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "org_add", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "org_damage", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "org_heal", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "org_memory_get", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "org_memory_set", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "seed_get", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "seed_set", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "seed_add", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "emit_signal", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "spawn_seed", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "spawn", TypeSymbol.Unknown);

            DeclareBuiltinValue(global, "world", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "org", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "seed", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "env", TypeSymbol.Unknown);

            // Math builtins
            DeclareBuiltinFunction(global, "min", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "max", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "abs", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "round", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "sqrt", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "sin", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "cos", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "tan", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "clamp", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "lerp", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "remap", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "floor", TypeSymbol.Int);
            DeclareBuiltinFunction(global, "ceil", TypeSymbol.Int);
            DeclareBuiltinFunction(global, "pow", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "str", TypeSymbol.String);
            DeclareBuiltinFunction(global, "warn", TypeSymbol.None);
            DeclareBuiltinFunction(global, "error", TypeSymbol.None);

            // Random builtins
            DeclareBuiltinFunction(global, "random", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "random_int", TypeSymbol.Int);
            DeclareBuiltinFunction(global, "random_choice", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "noise", TypeSymbol.Float);
            DeclareBuiltinFunction(global, "chance", TypeSymbol.Bool);

            // Biological logic builtins
            DeclareBuiltinFunction(global, "every", TypeSymbol.Bool);
            DeclareBuiltinFunction(global, "after", TypeSymbol.Bool);
            DeclareBuiltinFunction(global, "between", TypeSymbol.Bool);
            DeclareBuiltinFunction(global, "season", TypeSymbol.String);
            DeclareBuiltinFunction(global, "time_of_day", TypeSymbol.String);

            // Constants
            DeclareBuiltinValue(global, "TICK", TypeSymbol.Int);
            DeclareBuiltinValue(global, "SELF", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "NONE", TypeSymbol.None);
            DeclareBuiltinValue(global, "UP", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "DOWN", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "LEFT", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "RIGHT", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "NORTH", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "SOUTH", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "EAST", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "WEST", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "math", TypeSymbol.Unknown);

            // Biological modules
            DeclareBuiltinValue(global, "root", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "stem", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "leaf", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "photo", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "morph", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "defense", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "reproduce", TypeSymbol.Unknown);
            DeclareBuiltinValue(global, "depot", TypeSymbol.Unknown);

            // Global biological functions
            DeclareBuiltinFunction(global, "synthesize", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "produce", TypeSymbol.Unknown);
            DeclareBuiltinFunction(global, "emit", TypeSymbol.Unknown);
        }

        private static void DeclareBuiltinFunction(Scope global, string name, TypeSymbol returnType)
        {
            if (global == null || string.IsNullOrEmpty(name))
                return;

            global.TryDeclare(new SymbolInfo(
                name,
                SymbolKind.Function,
                declLine: 0,
                declColumn: 0,
                declaration: null,
                type: returnType ?? TypeSymbol.Unknown));
        }

        private static void DeclareBuiltinValue(Scope global, string name, TypeSymbol type)
        {
            if (global == null || string.IsNullOrEmpty(name))
                return;

            global.TryDeclare(new SymbolInfo(
                name,
                SymbolKind.Variable,
                declLine: 0,
                declColumn: 0,
                declaration: null,
                type: type ?? TypeSymbol.Unknown));
        }

        private sealed class Walker
        {
            private readonly Scope _global;
            private readonly List<AnalyzeError> _errors;
            private readonly Dictionary<GrowlNode, Scope> _scopeByNode;
            private readonly Dictionary<NameExpr, SymbolInfo> _nameBindings;
            private readonly Dictionary<GrowlNode, TypeSymbol> _expressionTypes;
            private readonly Stack<FunctionContext> _functionContexts = new Stack<FunctionContext>();
            private readonly Dictionary<string, AggregateTypeInfo> _aggregateTypes =
                new Dictionary<string, AggregateTypeInfo>();

            private sealed class FunctionContext
            {
                public readonly string Name;
                public readonly TypeSymbol ReturnType;
                public readonly Scope FunctionScope;
                public readonly HashSet<SymbolInfo> KnownVariables = new HashSet<SymbolInfo>();
                public readonly HashSet<SymbolInfo> AssignedVariables = new HashSet<SymbolInfo>();

                public FunctionContext(string name, TypeSymbol returnType, Scope functionScope)
                {
                    Name = string.IsNullOrEmpty(name) ? "<anonymous>" : name;
                    ReturnType = returnType ?? TypeSymbol.Unknown;
                    FunctionScope = functionScope;
                }
            }

            private sealed class AggregateTypeInfo
            {
                public readonly string Name;
                public readonly Dictionary<string, MemberInfo> Members =
                    new Dictionary<string, MemberInfo>();

                public AggregateTypeInfo(string name)
                {
                    Name = name;
                }
            }

            private sealed class MemberInfo
            {
                public readonly string Name;
                public readonly TypeSymbol Type;
                public readonly bool IsMethod;
                public readonly FnDecl MethodDecl;

                public MemberInfo(string name, TypeSymbol type, bool isMethod, FnDecl methodDecl)
                {
                    Name = name;
                    Type = type ?? TypeSymbol.Unknown;
                    IsMethod = isMethod;
                    MethodDecl = methodDecl;
                }
            }

            public Walker(
                Scope global,
                List<AnalyzeError> errors,
                Dictionary<GrowlNode, Scope> scopeByNode,
                Dictionary<NameExpr, SymbolInfo> nameBindings,
                Dictionary<GrowlNode, TypeSymbol> expressionTypes)
            {
                _global = global;
                _errors = errors;
                _scopeByNode = scopeByNode;
                _nameBindings = nameBindings;
                _expressionTypes = expressionTypes;
            }

            public void AnalyzeProgram(ProgramNode program)
            {
                if (program == null)
                    return;

                _scopeByNode[program] = _global;
                AnalyzeStatements(program.Statements, _global);
            }

            private void AnalyzeStatements(List<GrowlNode> statements, Scope scope)
            {
                if (statements == null)
                    return;

                for (int i = 0; i < statements.Count; i++)
                    AnalyzeStatement(statements[i], scope);
            }

            private void AnalyzeStatement(GrowlNode node, Scope scope)
            {
                if (node == null)
                    return;

                _scopeByNode[node] = scope;

                switch (node)
                {
                    case ConstDecl c:
                        {
                            SymbolInfo symbol = Declare(scope, c.Name, SymbolKind.Const, c.Line, c.Column, c);
                            if (symbol != null)
                                symbol.Type = ResolveTypeRef(c.TypeAnnotation, scope);

                            TypeSymbol valueType = AnalyzeExpression(c.Value, scope);
                            if (symbol != null)
                            {
                                if (symbol.Type.IsUnknown)
                                    symbol.Type = valueType;
                                else
                                    CheckAssignmentCompatibility(
                                        symbol.Type,
                                        valueType,
                                        c.Line,
                                        c.Column,
                                        "Constant '" + c.Name + "'");
                            }
                        }
                        return;

                    case TypeAliasDecl t:
                        {
                            SymbolInfo symbol = Declare(scope, t.Name, SymbolKind.TypeAlias, t.Line, t.Column, t);
                            if (symbol != null)
                                symbol.Type = ResolveTypeRef(t.Target, scope);
                        }
                        return;

                    case ModuleDecl m:
                        Declare(scope, m.Name, SymbolKind.Module, m.Line, m.Column, m);
                        return;

                    case FnDecl f:
                        AnalyzeFnDecl(f, scope, declareInParent: true);
                        return;

                    case GeneDecl g:
                        AnalyzeFnDecl(g.Fn, scope, declareInParent: true);
                        return;

                    case ClassDecl c:
                        {
                            SymbolInfo symbol = Declare(scope, c.Name, SymbolKind.Class, c.Line, c.Column, c);
                            if (symbol != null)
                                symbol.Type = new TypeSymbol(c.Name);
                        }
                        AnalyzeClassDecl(c, scope);
                        return;

                    case StructDecl s:
                        {
                            SymbolInfo symbol = Declare(scope, s.Name, SymbolKind.Struct, s.Line, s.Column, s);
                            if (symbol != null)
                                symbol.Type = new TypeSymbol(s.Name);
                        }
                        AnalyzeStructDecl(s, scope);
                        return;

                    case EnumDecl e:
                        {
                            SymbolInfo symbol = Declare(scope, e.Name, SymbolKind.Enum, e.Line, e.Column, e);
                            if (symbol != null)
                                symbol.Type = new TypeSymbol(e.Name);
                        }
                        AnalyzeEnumDecl(e, scope);
                        return;

                    case TraitDecl t:
                        {
                            SymbolInfo symbol = Declare(scope, t.Name, SymbolKind.Trait, t.Line, t.Column, t);
                            if (symbol != null)
                                symbol.Type = new TypeSymbol(t.Name);
                        }
                        AnalyzeTraitDecl(t, scope);
                        return;

                    case MixinDecl m:
                        {
                            SymbolInfo symbol = Declare(scope, m.Name, SymbolKind.Mixin, m.Line, m.Column, m);
                            if (symbol != null)
                                symbol.Type = new TypeSymbol(m.Name);
                        }
                        AnalyzeMixinDecl(m, scope);
                        return;

                    case AssignStmt a:
                        {
                            SymbolInfo targetSymbol = AnalyzeAssignmentTarget(a.Target, scope);
                            TypeSymbol valueType = AnalyzeExpression(a.Value, scope);

                            if (targetSymbol != null)
                            {
                                if (targetSymbol.Type.IsUnknown && a.Op.Type == TokenType.Equal)
                                    targetSymbol.Type = valueType;

                                CheckAssignmentCompatibility(
                                    targetSymbol.Type,
                                    valueType,
                                    a.Line,
                                    a.Column,
                                    "Assignment to '" + targetSymbol.Name + "'");

                                MarkAssigned(targetSymbol);
                            }
                        }
                        return;

                    case ExprStmt e:
                        AnalyzeExpression(e.Expression, scope);
                        return;

                    case ReturnStmt r:
                        {
                            TypeSymbol returnValueType = r.Value == null
                                ? TypeSymbol.None
                                : AnalyzeExpression(r.Value, scope);
                            CheckReturnCompatibility(r, returnValueType);
                        }
                        return;

                    case YieldStmt y:
                        AnalyzeExpression(y.Value, scope);
                        return;

                    case WaitStmt w:
                        AnalyzeExpression(w.Ticks, scope);
                        return;

                    case DeferStmt d:
                        AnalyzeExpression(d.Duration, scope);
                        AnalyzeStatements(d.Body, scope.CreateChild("defer"));
                        return;

                    case MutateStmt m:
                        {
                            SymbolInfo targetSymbol = AnalyzeAssignmentTarget(m.Target, scope);
                            TypeSymbol valueType = AnalyzeExpression(m.Value, scope);
                            AnalyzeExpression(m.Interval, scope);

                            if (targetSymbol != null)
                            {
                                if (targetSymbol.Type.IsUnknown)
                                    targetSymbol.Type = valueType;
                                else
                                    CheckAssignmentCompatibility(
                                        targetSymbol.Type,
                                        valueType,
                                        m.Line,
                                        m.Column,
                                        "Mutation of '" + targetSymbol.Name + "'");
                            }
                        }
                        return;

                    case IfStmt i:
                        {
                            HashSet<SymbolInfo> before = SnapshotAssignedVariables();
                            TypeSymbol ifCondType = AnalyzeExpression(i.Condition, scope);
                            CheckConditionCompatibility(ifCondType, i.Condition, "if");

                            var branchStates = new List<HashSet<SymbolInfo>>();

                            RestoreAssignedVariables(CloneAssignedVariables(before));
                            AnalyzeStatements(i.ThenBody, scope.CreateChild("if.then"));
                            branchStates.Add(SnapshotAssignedVariables());

                            for (int k = 0; k < i.ElifClauses.Count; k++)
                            {
                                RestoreAssignedVariables(CloneAssignedVariables(before));
                                TypeSymbol elifCondType = AnalyzeExpression(i.ElifClauses[k].Condition, scope);
                                CheckConditionCompatibility(elifCondType, i.ElifClauses[k].Condition, "elif");
                                AnalyzeStatements(i.ElifClauses[k].Body, scope.CreateChild("if.elif"));
                                branchStates.Add(SnapshotAssignedVariables());
                            }

                            if (i.ElseBody != null)
                            {
                                RestoreAssignedVariables(CloneAssignedVariables(before));
                                AnalyzeStatements(i.ElseBody, scope.CreateChild("if.else"));
                                branchStates.Add(SnapshotAssignedVariables());
                                RestoreAssignedVariables(IntersectAssignedVariables(branchStates));
                            }
                            else
                            {
                                RestoreAssignedVariables(before);
                            }
                        }
                        return;

                    case ForStmt f:
                        {
                            HashSet<SymbolInfo> before = SnapshotAssignedVariables();
                            AnalyzeExpression(f.Iterable, scope);
                            Scope forScope = scope.CreateChild("for");
                            for (int k = 0; k < f.Targets.Count; k++)
                            {
                                string name = f.Targets[k];
                                if (!string.IsNullOrEmpty(name))
                                {
                                    SymbolInfo target = Declare(forScope, name, SymbolKind.Variable, f.Line, f.Column, f);
                                    MarkAssigned(target);
                                }
                            }
                            AnalyzeStatements(f.Body, forScope);
                            AnalyzeStatements(f.ElseBody, forScope);
                            RestoreAssignedVariables(before);
                        }
                        return;

                    case WhileStmt w:
                        {
                            HashSet<SymbolInfo> before = SnapshotAssignedVariables();
                            TypeSymbol whileCondType = AnalyzeExpression(w.Condition, scope);
                            CheckConditionCompatibility(whileCondType, w.Condition, "while");
                            RestoreAssignedVariables(CloneAssignedVariables(before));
                            AnalyzeStatements(w.Body, scope.CreateChild("while"));
                            RestoreAssignedVariables(before);
                        }
                        return;

                    case LoopStmt l:
                        AnalyzeStatements(l.Body, scope.CreateChild("loop"));
                        return;

                    case MatchStmt m:
                        {
                            HashSet<SymbolInfo> before = SnapshotAssignedVariables();
                            AnalyzeExpression(m.Subject, scope);
                            bool hasCatchAll = false;
                            var seenLiteralCases = new HashSet<string>();
                            for (int k = 0; k < m.Cases.Count; k++)
                            {
                                CaseClause currentCase = m.Cases[k];
                                if (hasCatchAll)
                                {
                                    AddError(
                                        AnalyzeErrorCode.UnreachableCase,
                                        "Unreachable case in match: a catch-all case already handled all remaining branches.",
                                        currentCase.Pattern != null ? currentCase.Pattern.Line : m.Line,
                                        currentCase.Pattern != null ? currentCase.Pattern.Column : m.Column);
                                }

                                if (TryGetLiteralPatternKey(currentCase.Pattern, out string literalKey))
                                {
                                    if (seenLiteralCases.Contains(literalKey))
                                    {
                                        AddError(
                                            AnalyzeErrorCode.DuplicateMatchCase,
                                            "Duplicate case pattern in match: " + literalKey + ".",
                                            currentCase.Pattern.Line,
                                            currentCase.Pattern.Column);
                                    }
                                    else
                                    {
                                        seenLiteralCases.Add(literalKey);
                                    }
                                }

                                if (IsCatchAllPattern(currentCase.Pattern))
                                    hasCatchAll = true;

                                RestoreAssignedVariables(CloneAssignedVariables(before));
                                Scope caseScope = scope.CreateChild("match.case");
                                AnalyzePattern(currentCase.Pattern, caseScope);
                                TypeSymbol guardType = AnalyzeExpression(currentCase.Guard, caseScope);
                                CheckConditionCompatibility(guardType, currentCase.Guard, "match guard");
                                AnalyzeStatements(currentCase.Body, caseScope);
                            }

                            if (!hasCatchAll)
                            {
                                AddError(
                                    AnalyzeErrorCode.NonExhaustiveMatch,
                                    "Match is non-exhaustive: add a default '_' case.",
                                    m.Line,
                                    m.Column);
                            }

                            RestoreAssignedVariables(before);
                        }
                        return;

                    case TryStmt t:
                        {
                            HashSet<SymbolInfo> before = SnapshotAssignedVariables();

                            RestoreAssignedVariables(CloneAssignedVariables(before));
                            AnalyzeStatements(t.TryBody, scope.CreateChild("try"));
                            HashSet<SymbolInfo> tryAfter = SnapshotAssignedVariables();

                            RestoreAssignedVariables(CloneAssignedVariables(before));
                            Scope recoverScope = scope.CreateChild("recover");
                            if (!string.IsNullOrEmpty(t.ErrorName))
                            {
                                SymbolInfo err = Declare(recoverScope, t.ErrorName, SymbolKind.Variable, t.Line, t.Column, t);
                                MarkAssigned(err);
                            }
                            AnalyzeStatements(t.RecoverBody, recoverScope);
                            HashSet<SymbolInfo> recoverAfter = SnapshotAssignedVariables();

                            RestoreAssignedVariables(IntersectAssignedVariables(tryAfter, recoverAfter));
                            AnalyzeStatements(t.AlwaysBody, scope.CreateChild("always"));
                        }
                        return;

                    case PhaseBlock p:
                        AnalyzeExpression(p.MinAge, scope);
                        AnalyzeExpression(p.MaxAge, scope);
                        AnalyzeExpression(p.Condition, scope);
                        AnalyzeStatements(p.Body, scope.CreateChild("phase"));
                        return;

                    case WhenBlock w:
                        AnalyzeExpression(w.Condition, scope);
                        AnalyzeStatements(w.Body, scope.CreateChild("when"));
                        AnalyzeStatements(w.ThenBlock, scope.CreateChild("when.then"));
                        return;

                    case RespondBlock r:
                        {
                            Scope respondScope = scope.CreateChild("respond");
                            if (!string.IsNullOrEmpty(r.Binding))
                            {
                                SymbolInfo binding = Declare(respondScope, r.Binding, SymbolKind.Variable, r.Line, r.Column, r);
                                MarkAssigned(binding);
                            }
                            AnalyzeStatements(r.Body, respondScope);
                        }
                        return;

                    case AdaptBlock a:
                        AnalyzeExpression(a.Subject, scope);
                        for (int k = 0; k < a.Rules.Count; k++)
                        {
                            AnalyzeExpression(a.Rules[k].Condition, scope);
                            AnalyzeExpression(a.Rules[k].Action, scope);
                        }
                        AnalyzeExpression(a.Budget, scope);
                        return;

                    case CycleBlock c:
                        AnalyzeExpression(c.Period, scope);
                        for (int k = 0; k < c.Points.Count; k++)
                        {
                            AnalyzeExpression(c.Points[k].At, scope);
                            AnalyzeStatements(c.Points[k].Body, scope.CreateChild("cycle.point"));
                        }
                        return;

                    case TickerDecl t:
                        AnalyzeExpression(t.Interval, scope);
                        AnalyzeStatements(t.Body, scope.CreateChild("ticker"));
                        return;
                }

                // Fallback: treat unknown nodes as expressions.
                AnalyzeExpression(node, scope);
            }

            private void AnalyzeFnDecl(FnDecl fn, Scope parentScope, bool declareInParent)
            {
                if (fn == null)
                    return;

                SymbolInfo fnSymbol = null;
                if (declareInParent)
                {
                    fnSymbol = Declare(parentScope, fn.Name, SymbolKind.Function, fn.Line, fn.Column, fn);
                    if (fnSymbol != null)
                        fnSymbol.Type = ResolveTypeRef(fn.ReturnType, parentScope);
                }

                Scope fnScope = parentScope.CreateChild("fn:" + fn.Name);
                _scopeByNode[fn] = fnScope;
                TypeSymbol expectedReturnType = ResolveTypeRef(fn.ReturnType, fnScope);
                var fnContext = new FunctionContext(fn.Name, expectedReturnType, fnScope);
                _functionContexts.Push(fnContext);

                try
                {
                    for (int i = 0; i < fn.Params.Count; i++)
                    {
                        var p = fn.Params[i];
                        if (string.IsNullOrEmpty(p.Name))
                            continue;

                        SymbolInfo param = Declare(fnScope, p.Name, SymbolKind.Parameter, fn.Line, fn.Column, fn);
                        if (param != null)
                            param.Type = ResolveTypeRef(p.TypeAnnotation, fnScope);

                        TypeSymbol defaultType = AnalyzeExpression(p.DefaultValue, fnScope);
                        if (param != null && p.DefaultValue != null)
                        {
                            CheckAssignmentCompatibility(
                                param.Type,
                                defaultType,
                                fn.Line,
                                fn.Column,
                                "Default value for parameter '" + p.Name + "'");
                        }
                    }
                    AnalyzeStatements(fn.Body, fnScope);
                }
                finally
                {
                    _functionContexts.Pop();
                }

                if (!expectedReturnType.IsUnknown &&
                    !expectedReturnType.IsNone &&
                    !StatementsGuaranteeReturn(fn.Body))
                {
                    AddError(
                        AnalyzeErrorCode.TypeMismatch,
                        "Function '" + fn.Name + "' does not return on all paths.",
                        fn.Line,
                        fn.Column);
                }
            }

            private void AnalyzeClassDecl(ClassDecl cls, Scope parentScope)
            {
                Scope classScope = parentScope.CreateChild("class:" + cls.Name);
                _scopeByNode[cls] = classScope;

                // self is implicitly available in all class methods
                Declare(classScope, "self", SymbolKind.Variable, cls.Line, cls.Column, cls);

                AggregateTypeInfo info = GetOrCreateAggregateType(cls.Name);
                for (int i = 0; i < cls.Members.Count; i++)
                {
                    if (cls.Members[i] is FnDecl fnMember)
                    {
                        TypeSymbol methodReturnType = ResolveTypeRef(fnMember.ReturnType, classScope);
                        info.Members[fnMember.Name] = new MemberInfo(
                            fnMember.Name,
                            methodReturnType,
                            isMethod: true,
                            methodDecl: fnMember);
                    }
                }

                for (int i = 0; i < cls.Members.Count; i++)
                    AnalyzeStatement(cls.Members[i], classScope);
            }

            private void AnalyzeStructDecl(StructDecl s, Scope parentScope)
            {
                Scope structScope = parentScope.CreateChild("struct:" + s.Name);
                _scopeByNode[s] = structScope;
                AggregateTypeInfo info = GetOrCreateAggregateType(s.Name);

                for (int i = 0; i < s.Fields.Count; i++)
                {
                    SymbolInfo field = Declare(structScope, s.Fields[i].Name, SymbolKind.Variable, s.Line, s.Column, s);
                    if (field != null)
                        field.Type = ResolveTypeRef(s.Fields[i].TypeAnnotation, structScope);

                    TypeSymbol valueType = AnalyzeExpression(s.Fields[i].DefaultValue, structScope);
                    if (field != null && s.Fields[i].DefaultValue != null)
                    {
                        if (field.Type.IsUnknown)
                            field.Type = valueType;
                        else
                            CheckAssignmentCompatibility(
                                field.Type,
                                valueType,
                                s.Line,
                                s.Column,
                                "Field '" + s.Fields[i].Name + "'");
                    }

                    TypeSymbol fieldType = field != null ? field.Type : ResolveTypeRef(s.Fields[i].TypeAnnotation, structScope);
                    info.Members[s.Fields[i].Name] = new MemberInfo(
                        s.Fields[i].Name,
                        fieldType,
                        isMethod: false,
                        methodDecl: null);
                }

                for (int i = 0; i < s.Methods.Count; i++)
                {
                    TypeSymbol methodReturnType = ResolveTypeRef(s.Methods[i].ReturnType, structScope);
                    info.Members[s.Methods[i].Name] = new MemberInfo(
                        s.Methods[i].Name,
                        methodReturnType,
                        isMethod: true,
                        methodDecl: s.Methods[i]);
                    AnalyzeFnDecl(s.Methods[i], structScope, declareInParent: true);
                }
            }

            private void AnalyzeEnumDecl(EnumDecl e, Scope parentScope)
            {
                Scope enumScope = parentScope.CreateChild("enum:" + e.Name);
                _scopeByNode[e] = enumScope;

                for (int i = 0; i < e.Members.Count; i++)
                {
                    SymbolInfo member = Declare(enumScope, e.Members[i].Name, SymbolKind.Variable, e.Line, e.Column, e);
                    TypeSymbol valueType = AnalyzeExpression(e.Members[i].Value, enumScope);
                    if (member != null && !valueType.IsUnknown)
                        member.Type = valueType;
                }

                for (int i = 0; i < e.Methods.Count; i++)
                    AnalyzeFnDecl(e.Methods[i], enumScope, declareInParent: true);
            }

            private void AnalyzeTraitDecl(TraitDecl t, Scope parentScope)
            {
                Scope traitScope = parentScope.CreateChild("trait:" + t.Name);
                _scopeByNode[t] = traitScope;
                for (int i = 0; i < t.Members.Count; i++)
                    AnalyzeStatement(t.Members[i], traitScope);
            }

            private void AnalyzeMixinDecl(MixinDecl m, Scope parentScope)
            {
                Scope mixinScope = parentScope.CreateChild("mixin:" + m.Name);
                _scopeByNode[m] = mixinScope;
                for (int i = 0; i < m.Methods.Count; i++)
                    AnalyzeFnDecl(m.Methods[i], mixinScope, declareInParent: true);
            }

            private void AnalyzePattern(GrowlNode pattern, Scope scope)
            {
                BindPattern(pattern, scope);
            }

            private void BindPattern(GrowlNode pattern, Scope scope)
            {
                if (pattern == null)
                    return;

                if (!_scopeByNode.ContainsKey(pattern))
                    _scopeByNode[pattern] = scope;

                switch (pattern)
                {
                    case NameExpr n:
                        // Wildcard pattern does not bind.
                        if (n.Name == "_")
                            return;

                        SymbolInfo declared = Declare(scope, n.Name, SymbolKind.Variable, n.Line, n.Column, n);
                        if (scope.TryResolve(n.Name, out var bound))
                        {
                            _nameBindings[n] = bound;
                            MarkAssigned(bound);
                        }
                        else
                        {
                            MarkAssigned(declared);
                        }
                        return;

                    case TupleExpr t:
                        for (int i = 0; i < t.Elements.Count; i++)
                            BindPattern(t.Elements[i], scope);
                        return;

                    case ListExpr l:
                        for (int i = 0; i < l.Elements.Count; i++)
                            BindPattern(l.Elements[i], scope);
                        return;

                    case DictExpr d:
                        // Dict pattern keys are field selectors; bind only values.
                        for (int i = 0; i < d.Entries.Count; i++)
                            BindPattern(d.Entries[i].Value, scope);
                        return;

                    case CallExpr c:
                        // Constructor-like patterns: TypeName(binding, ...)
                        AnalyzeExpression(c.Callee, scope);
                        for (int i = 0; i < c.Args.Count; i++)
                            BindPattern(c.Args[i].Value, scope);
                        return;

                    case BinaryExpr b when b.Op.Type == TokenType.Is:
                        // Type pattern: <binder> is <TypeName>
                        BindPattern(b.Left, scope);
                        AnalyzeExpression(b.Right, scope);
                        return;

                    case BinaryExpr b when b.Op.Type == TokenType.Pipe:
                        // Or-pattern.
                        BindPattern(b.Left, scope);
                        BindPattern(b.Right, scope);
                        return;
                }

                // Non-binding pattern fragment (literal, arithmetic, etc.).
                AnalyzeExpression(pattern, scope);
            }

            private SymbolInfo AnalyzeAssignmentTarget(GrowlNode target, Scope scope)
            {
                if (target == null)
                    return null;

                if (target is NameExpr n)
                {
                    if (scope.TryResolve(n.Name, out var resolved))
                    {
                        _nameBindings[n] = resolved;
                        SetExpressionType(n, resolved.Type);
                        return resolved;
                    }

                    Scope declarationScope = GetAssignmentDeclarationScope(scope);
                    var introduced = new SymbolInfo(n.Name, SymbolKind.Variable, n.Line, n.Column, n);
                    declarationScope.TryDeclare(introduced);
                    TrackDeclaredSymbol(declarationScope, introduced, initiallyAssigned: false);
                    _nameBindings[n] = introduced;
                    SetExpressionType(n, introduced.Type);
                    return introduced;
                }

                if (target is AttributeExpr a)
                {
                    TypeSymbol objectType = AnalyzeExpression(a.Object, scope);
                    if (TryResolveMember(objectType, a.FieldName, out var member))
                    {
                        if (member.IsMethod)
                        {
                            AddError(
                                AnalyzeErrorCode.TypeMismatch,
                                "Cannot assign to method '" + a.FieldName + "' on type '" + objectType.Name + "'.",
                                a.Line,
                                a.Column);
                            return null;
                        }

                        return new SymbolInfo(a.FieldName, SymbolKind.Variable, a.Line, a.Column, a, member.Type);
                    }

                    if (IsKnownAggregateType(objectType))
                    {
                        AddError(
                            AnalyzeErrorCode.TypeMismatch,
                            "Type '" + objectType.Name + "' has no member '" + a.FieldName + "'.",
                            a.Line,
                            a.Column);
                    }
                    return null;
                }

                if (target is SubscriptExpr s)
                {
                    AnalyzeExpression(s.Object, scope);
                    AnalyzeExpression(s.Key, scope);
                    return null;
                }

                AnalyzeExpression(target, scope);
                return null;
            }

            private TypeSymbol AnalyzeExpression(GrowlNode node, Scope scope)
            {
                if (node == null)
                    return TypeSymbol.Unknown;

                if (!_scopeByNode.ContainsKey(node))
                    _scopeByNode[node] = scope;

                switch (node)
                {
                    // Literals
                    case IntegerLiteralExpr _:
                        SetExpressionType(node, TypeSymbol.Int);
                        return TypeSymbol.Int;
                    case FloatLiteralExpr _:
                        SetExpressionType(node, TypeSymbol.Float);
                        return TypeSymbol.Float;
                    case StringLiteralExpr _:
                        SetExpressionType(node, TypeSymbol.String);
                        return TypeSymbol.String;
                    case BoolLiteralExpr _:
                        SetExpressionType(node, TypeSymbol.Bool);
                        return TypeSymbol.Bool;
                    case NoneLiteralExpr _:
                        SetExpressionType(node, TypeSymbol.None);
                        return TypeSymbol.None;
                    case ColorLiteralExpr _:
                        SetExpressionType(node, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;

                    case InterpolatedStringExpr s:
                        for (int i = 0; i < s.Segments.Count; i++)
                            AnalyzeExpression(s.Segments[i], scope);
                        SetExpressionType(s, TypeSymbol.String);
                        return TypeSymbol.String;

                    case NameExpr n:
                        if (n.Name == "_")
                        {
                            SetExpressionType(n, TypeSymbol.Unknown);
                            return TypeSymbol.Unknown;
                        }

                        if (scope.TryResolve(n.Name, out var symbol))
                        {
                            _nameBindings[n] = symbol;
                            SetExpressionType(n, symbol.Type);

                            if (RequiresDefiniteAssignment(symbol) && !IsDefinitelyAssigned(symbol))
                            {
                                AddError(
                                    AnalyzeErrorCode.ReadBeforeAssignment,
                                    "Variable '" + n.Name + "' may be read before assignment.",
                                    n.Line,
                                    n.Column);
                            }

                            return symbol.Type;
                        }

                        AddError(
                            AnalyzeErrorCode.UnresolvedName,
                            "Unresolved name '" + n.Name + "'.",
                            n.Line,
                            n.Column);
                        SetExpressionType(n, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;

                    case BinaryExpr b:
                        {
                            TypeSymbol left = AnalyzeExpression(b.Left, scope);
                            TypeSymbol right = AnalyzeExpression(b.Right, scope);
                            TypeSymbol result = InferBinaryType(b, left, right);
                            SetExpressionType(b, result);
                            return result;
                        }

                    case UnaryExpr u:
                        {
                            TypeSymbol operandType = AnalyzeExpression(u.Operand, scope);
                            TypeSymbol result = InferUnaryType(u, operandType);
                            SetExpressionType(u, result);
                            return result;
                        }

                    case TernaryExpr t:
                        {
                            TypeSymbol thenType = AnalyzeExpression(t.ThenExpr, scope);
                            TypeSymbol conditionType = AnalyzeExpression(t.Condition, scope);
                            CheckConditionCompatibility(conditionType, t.Condition, "ternary");
                            TypeSymbol elseType = AnalyzeExpression(t.ElseExpr, scope);
                            TypeSymbol merged = MergeTypes(thenType, elseType);
                            SetExpressionType(t, merged);
                            return merged;
                        }

                    case CallExpr c:
                        {
                            TypeSymbol callReturnType = TypeSymbol.Unknown;
                            if (c.Callee is AttributeExpr memberCallee)
                            {
                                TypeSymbol ownerType = AnalyzeExpression(memberCallee.Object, scope);
                                if (TryResolveMember(ownerType, memberCallee.FieldName, out var member))
                                {
                                    if (member.IsMethod && member.MethodDecl != null)
                                    {
                                        AnalyzeAndValidateCallArguments(c, member.MethodDecl, scope, skipLeadingSelfParameter: true);
                                        callReturnType = ResolveTypeRef(member.MethodDecl.ReturnType, scope);
                                    }
                                    else
                                    {
                                        for (int i = 0; i < c.Args.Count; i++)
                                            AnalyzeExpression(c.Args[i].Value, scope);

                                        AddError(
                                            AnalyzeErrorCode.TypeMismatch,
                                            "Member '" + memberCallee.FieldName + "' on type '" + ownerType.Name + "' is not callable.",
                                            c.Line,
                                            c.Column);
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < c.Args.Count; i++)
                                        AnalyzeExpression(c.Args[i].Value, scope);

                                    if (IsKnownAggregateType(ownerType))
                                    {
                                        AddError(
                                            AnalyzeErrorCode.TypeMismatch,
                                            "Type '" + ownerType.Name + "' has no member '" + memberCallee.FieldName + "'.",
                                            c.Line,
                                            c.Column);
                                    }
                                }
                            }
                            else
                            {
                                AnalyzeExpression(c.Callee, scope);
                                if (TryResolveCalledFunction(c.Callee, scope, out var functionSymbol, out var functionDecl))
                                {
                                    AnalyzeAndValidateCallArguments(c, functionDecl, scope, skipLeadingSelfParameter: false);
                                    callReturnType = functionSymbol.Type ?? ResolveTypeRef(functionDecl.ReturnType, scope);
                                }
                                else
                                {
                                    for (int i = 0; i < c.Args.Count; i++)
                                        AnalyzeExpression(c.Args[i].Value, scope);
                                }
                            }

                            SetExpressionType(c, callReturnType);
                            return callReturnType;
                        }

                    case AttributeExpr a:
                        {
                            TypeSymbol objectType = AnalyzeExpression(a.Object, scope);
                            if (TryResolveMember(objectType, a.FieldName, out var member))
                            {
                                TypeSymbol memberType = member.IsMethod ? TypeSymbol.Unknown : member.Type;
                                SetExpressionType(a, memberType);
                                return memberType;
                            }

                            if (IsKnownAggregateType(objectType))
                            {
                                AddError(
                                    AnalyzeErrorCode.TypeMismatch,
                                    "Type '" + objectType.Name + "' has no member '" + a.FieldName + "'.",
                                    a.Line,
                                    a.Column);
                            }

                            SetExpressionType(a, TypeSymbol.Unknown);
                            return TypeSymbol.Unknown;
                        }

                    case SubscriptExpr s:
                        {
                            TypeSymbol objectType = AnalyzeExpression(s.Object, scope);
                            TypeSymbol indexType = AnalyzeExpression(s.Key, scope);

                            if (objectType.IsUnknown)
                            {
                                SetExpressionType(s, TypeSymbol.Unknown);
                                return TypeSymbol.Unknown;
                            }

                            if (objectType.IsString)
                            {
                                if (!indexType.IsUnknown && indexType.Name != "int")
                                {
                                    AddError(
                                        AnalyzeErrorCode.TypeMismatch,
                                        "String index expects 'int' but got '" + indexType.Name + "'.",
                                        s.Key.Line,
                                        s.Key.Column);
                                }

                                SetExpressionType(s, TypeSymbol.String);
                                return TypeSymbol.String;
                            }

                            if (TypeSymbol.TryGetListElementType(objectType, out var elementType))
                            {
                                if (!indexType.IsUnknown && indexType.Name != "int")
                                {
                                    AddError(
                                        AnalyzeErrorCode.TypeMismatch,
                                        "List index expects 'int' but got '" + indexType.Name + "'.",
                                        s.Key.Line,
                                        s.Key.Column);
                                }

                                SetExpressionType(s, elementType);
                                return elementType;
                            }

                            if (TypeSymbol.TryGetDictTypes(objectType, out var keyType, out var valueType))
                            {
                                if (!indexType.IsUnknown && !AreTypesCompatible(keyType, indexType))
                                {
                                    AddError(
                                        AnalyzeErrorCode.TypeMismatch,
                                        "Dict index expects '" + keyType.Name + "' but got '" + indexType.Name + "'.",
                                        s.Key.Line,
                                        s.Key.Column);
                                }

                                SetExpressionType(s, valueType);
                                return valueType;
                            }

                            AddError(
                                AnalyzeErrorCode.TypeMismatch,
                                "Cannot use subscript on type '" + objectType.Name + "'.",
                                s.Line,
                                s.Column);
                            SetExpressionType(s, TypeSymbol.Unknown);
                            return TypeSymbol.Unknown;
                        }

                    case LambdaExpr l:
                        {
                            Scope lambdaScope = scope.CreateChild("lambda");
                            _scopeByNode[l] = lambdaScope;

                            for (int i = 0; i < l.Params.Count; i++)
                            {
                                var p = l.Params[i];
                                if (!string.IsNullOrEmpty(p.Name))
                                {
                                    SymbolInfo param = Declare(lambdaScope, p.Name, SymbolKind.Parameter, l.Line, l.Column, l);
                                    if (param != null)
                                        param.Type = ResolveTypeRef(p.TypeAnnotation, lambdaScope);
                                }

                                AnalyzeExpression(p.DefaultValue, lambdaScope);
                            }

                            AnalyzeExpression(l.Body, lambdaScope);
                            SetExpressionType(l, TypeSymbol.Unknown);
                            return TypeSymbol.Unknown;
                        }

                    case ListExpr l:
                        {
                            TypeSymbol elementType = TypeSymbol.Unknown;
                            for (int i = 0; i < l.Elements.Count; i++)
                            {
                                TypeSymbol currentType = AnalyzeExpression(l.Elements[i], scope);
                                elementType = elementType.IsUnknown ? currentType : MergeTypes(elementType, currentType);
                            }

                            TypeSymbol listType = TypeSymbol.CreateList(elementType);
                            SetExpressionType(l, listType);
                            return listType;
                        }

                    case ListComprehensionExpr l:
                        {
                            Scope compScope = scope.CreateChild("listcomp");
                            for (int i = 0; i < l.Clauses.Count; i++)
                            {
                                AnalyzeExpression(l.Clauses[i].Iterable, compScope);
                                for (int k = 0; k < l.Clauses[i].Targets.Count; k++)
                                {
                                    SymbolInfo target = Declare(compScope, l.Clauses[i].Targets[k], SymbolKind.Variable, l.Line, l.Column, l);
                                    MarkAssigned(target);
                                }
                                AnalyzeExpression(l.Clauses[i].Filter, compScope);
                            }
                            AnalyzeExpression(l.Element, compScope);
                            SetExpressionType(l, TypeSymbol.Unknown);
                            return TypeSymbol.Unknown;
                        }

                    case DictExpr d:
                        {
                            TypeSymbol keyType = TypeSymbol.Unknown;
                            TypeSymbol valueType = TypeSymbol.Unknown;
                            for (int i = 0; i < d.Entries.Count; i++)
                            {
                                TypeSymbol currentKeyType = AnalyzeExpression(d.Entries[i].Key, scope);
                                TypeSymbol currentValueType = AnalyzeExpression(d.Entries[i].Value, scope);

                                keyType = keyType.IsUnknown ? currentKeyType : MergeTypes(keyType, currentKeyType);
                                valueType = valueType.IsUnknown ? currentValueType : MergeTypes(valueType, currentValueType);
                            }

                            TypeSymbol dictType = TypeSymbol.CreateDict(keyType, valueType);
                            SetExpressionType(d, dictType);
                            return dictType;
                        }

                    case DictComprehensionExpr d:
                        {
                            Scope compScope = scope.CreateChild("dictcomp");
                            for (int i = 0; i < d.Clauses.Count; i++)
                            {
                                AnalyzeExpression(d.Clauses[i].Iterable, compScope);
                                for (int k = 0; k < d.Clauses[i].Targets.Count; k++)
                                {
                                    SymbolInfo target = Declare(compScope, d.Clauses[i].Targets[k], SymbolKind.Variable, d.Line, d.Column, d);
                                    MarkAssigned(target);
                                }
                                AnalyzeExpression(d.Clauses[i].Filter, compScope);
                            }
                            AnalyzeExpression(d.Key, compScope);
                            AnalyzeExpression(d.Value, compScope);
                            SetExpressionType(d, TypeSymbol.Unknown);
                            return TypeSymbol.Unknown;
                        }

                    case SetExpr s:
                        for (int i = 0; i < s.Elements.Count; i++)
                            AnalyzeExpression(s.Elements[i], scope);
                        SetExpressionType(s, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;

                    case TupleExpr t:
                        for (int i = 0; i < t.Elements.Count; i++)
                            AnalyzeExpression(t.Elements[i], scope);
                        SetExpressionType(t, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;

                    case RangeExpr r:
                        AnalyzeExpression(r.Start, scope);
                        AnalyzeExpression(r.End, scope);
                        AnalyzeExpression(r.Step, scope);
                        SetExpressionType(r, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;

                    case VectorExpr v:
                        AnalyzeExpression(v.X, scope);
                        AnalyzeExpression(v.Y, scope);
                        AnalyzeExpression(v.Z, scope);
                        SetExpressionType(v, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;

                    case SpreadExpr s:
                        AnalyzeExpression(s.Operand, scope);
                        SetExpressionType(s, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;

                    case ProgramNode p:
                        _scopeByNode[p] = scope;
                        AnalyzeStatements(p.Statements, scope);
                        SetExpressionType(p, TypeSymbol.Unknown);
                        return TypeSymbol.Unknown;
                }

                SetExpressionType(node, TypeSymbol.Unknown);
                return TypeSymbol.Unknown;
            }

            private void SetExpressionType(GrowlNode node, TypeSymbol type)
            {
                if (node == null)
                    return;

                _expressionTypes[node] = type ?? TypeSymbol.Unknown;
            }

            private static TypeSymbol MergeTypes(TypeSymbol a, TypeSymbol b)
            {
                if (a == null || a.IsUnknown)
                    return b ?? TypeSymbol.Unknown;
                if (b == null || b.IsUnknown)
                    return a;
                if (a.Name == b.Name)
                    return a;
                if (a.IsNumeric && b.IsNumeric)
                    return PromoteNumeric(a, b);
                return TypeSymbol.Conflict;
            }

            private TypeSymbol ResolveTypeRef(TypeRef typeRef, Scope scope)
            {
                if (typeRef == null)
                    return TypeSymbol.Unknown;

                string typeName = typeRef.Name ?? string.Empty;
                string lowered = typeName.ToLowerInvariant();

                if (lowered == "list" && typeRef.TypeArgs != null && typeRef.TypeArgs.Count == 1)
                {
                    TypeSymbol elementType = ResolveTypeRef(typeRef.TypeArgs[0], scope);
                    return TypeSymbol.CreateList(elementType);
                }

                if (lowered == "dict" && typeRef.TypeArgs != null && typeRef.TypeArgs.Count == 2)
                {
                    TypeSymbol keyType = ResolveTypeRef(typeRef.TypeArgs[0], scope);
                    TypeSymbol valueType = ResolveTypeRef(typeRef.TypeArgs[1], scope);
                    return TypeSymbol.CreateDict(keyType, valueType);
                }

                TypeSymbol builtin = TypeSymbol.FromBuiltinName(typeRef.Name);
                if (!builtin.IsUnknown)
                    return builtin;

                if (scope != null &&
                    scope.TryResolve(typeName, out var symbol) &&
                    symbol != null &&
                    IsTypeSymbolKind(symbol.Kind) &&
                    symbol.Type != null &&
                    !symbol.Type.IsUnknown)
                {
                    return symbol.Type;
                }

                if (typeRef.TypeArgs != null && typeRef.TypeArgs.Count > 0)
                {
                    var renderedArgs = new List<string>(typeRef.TypeArgs.Count);
                    for (int i = 0; i < typeRef.TypeArgs.Count; i++)
                        renderedArgs.Add(ResolveTypeRef(typeRef.TypeArgs[i], scope).Name);

                    return new TypeSymbol(typeName + "<" + string.Join(",", renderedArgs) + ">");
                }

                return TypeSymbol.Unknown;
            }

            private static bool IsTypeSymbolKind(SymbolKind kind)
            {
                switch (kind)
                {
                    case SymbolKind.TypeAlias:
                    case SymbolKind.Class:
                    case SymbolKind.Struct:
                    case SymbolKind.Enum:
                    case SymbolKind.Trait:
                    case SymbolKind.Mixin:
                        return true;
                    default:
                        return false;
                }
            }

            private Scope GetAssignmentDeclarationScope(Scope currentScope)
            {
                if (_functionContexts.Count == 0)
                    return currentScope;

                var context = _functionContexts.Peek();
                return context != null && context.FunctionScope != null
                    ? context.FunctionScope
                    : currentScope;
            }

            private HashSet<SymbolInfo> SnapshotAssignedVariables()
            {
                if (_functionContexts.Count == 0)
                    return null;

                return new HashSet<SymbolInfo>(_functionContexts.Peek().AssignedVariables);
            }

            private static HashSet<SymbolInfo> CloneAssignedVariables(HashSet<SymbolInfo> state)
            {
                if (state == null)
                    return null;
                return new HashSet<SymbolInfo>(state);
            }

            private void RestoreAssignedVariables(HashSet<SymbolInfo> state)
            {
                if (_functionContexts.Count == 0)
                    return;

                var context = _functionContexts.Peek();
                context.AssignedVariables.Clear();
                if (state == null)
                    return;

                foreach (var symbol in state)
                    context.AssignedVariables.Add(symbol);
            }

            private static HashSet<SymbolInfo> IntersectAssignedVariables(
                HashSet<SymbolInfo> left,
                HashSet<SymbolInfo> right)
            {
                if (left == null && right == null)
                    return null;
                if (left == null)
                    return CloneAssignedVariables(right);
                if (right == null)
                    return CloneAssignedVariables(left);

                var intersection = new HashSet<SymbolInfo>(left);
                intersection.IntersectWith(right);
                return intersection;
            }

            private static HashSet<SymbolInfo> IntersectAssignedVariables(List<HashSet<SymbolInfo>> states)
            {
                if (states == null || states.Count == 0)
                    return null;

                HashSet<SymbolInfo> result = CloneAssignedVariables(states[0]) ?? new HashSet<SymbolInfo>();
                for (int i = 1; i < states.Count; i++)
                {
                    if (states[i] == null)
                    {
                        result.Clear();
                        continue;
                    }

                    result.IntersectWith(states[i]);
                }

                return result;
            }

            private static bool IsTrackableAssignmentKind(SymbolKind kind)
            {
                return kind == SymbolKind.Variable ||
                       kind == SymbolKind.Parameter ||
                       kind == SymbolKind.Const;
            }

            private void TrackDeclaredSymbol(Scope declarationScope, SymbolInfo symbol, bool initiallyAssigned)
            {
                if (symbol == null || !IsTrackableAssignmentKind(symbol.Kind))
                    return;
                if (_functionContexts.Count == 0)
                    return;

                var context = _functionContexts.Peek();
                if (context == null || context.FunctionScope == null)
                    return;
                if (!IsScopeWithin(declarationScope, context.FunctionScope))
                    return;

                context.KnownVariables.Add(symbol);
                if (initiallyAssigned)
                    context.AssignedVariables.Add(symbol);
            }

            private static bool IsScopeWithin(Scope inner, Scope outer)
            {
                if (inner == null || outer == null)
                    return false;

                for (Scope s = inner; s != null; s = s.Parent)
                {
                    if (ReferenceEquals(s, outer))
                        return true;
                }

                return false;
            }

            private void MarkAssigned(SymbolInfo symbol)
            {
                if (symbol == null || !IsTrackableAssignmentKind(symbol.Kind))
                    return;
                if (_functionContexts.Count == 0)
                    return;

                var context = _functionContexts.Peek();
                if (context == null)
                    return;

                context.KnownVariables.Add(symbol);
                context.AssignedVariables.Add(symbol);
            }

            private bool RequiresDefiniteAssignment(SymbolInfo symbol)
            {
                if (symbol == null || !IsTrackableAssignmentKind(symbol.Kind))
                    return false;
                if (_functionContexts.Count == 0)
                    return false;

                return _functionContexts.Peek().KnownVariables.Contains(symbol);
            }

            private bool IsDefinitelyAssigned(SymbolInfo symbol)
            {
                if (symbol == null)
                    return true;
                if (_functionContexts.Count == 0)
                    return true;

                var context = _functionContexts.Peek();
                if (context == null || !context.KnownVariables.Contains(symbol))
                    return true;

                return context.AssignedVariables.Contains(symbol);
            }

            private TypeSymbol InferUnaryType(UnaryExpr expr, TypeSymbol operandType)
            {
                if (expr == null)
                    return TypeSymbol.Unknown;

                switch (expr.Op.Type)
                {
                    case TokenType.Not:
                        return TypeSymbol.Bool;
                    case TokenType.Plus:
                    case TokenType.Minus:
                        if (!operandType.IsUnknown && !operandType.IsNumeric)
                        {
                            AddError(
                                AnalyzeErrorCode.InvalidBinaryOperands,
                                "Unary operator '" + expr.Op.Value + "' requires a numeric operand.",
                                expr.Op.Line,
                                expr.Op.Column);
                            return TypeSymbol.Unknown;
                        }
                        return operandType;
                    default:
                        return TypeSymbol.Unknown;
                }
            }

            private TypeSymbol InferBinaryType(BinaryExpr expr, TypeSymbol left, TypeSymbol right)
            {
                if (expr == null)
                    return TypeSymbol.Unknown;

                switch (expr.Op.Type)
                {
                    case TokenType.Plus:
                        if (left.IsUnknown || right.IsUnknown)
                            return TypeSymbol.Unknown;
                        if (left.IsNumeric && right.IsNumeric)
                            return PromoteNumeric(left, right);
                        if (left.IsString && right.IsString)
                            return TypeSymbol.String;
                        AddInvalidBinaryOperandError(expr, left, right);
                        return TypeSymbol.Unknown;

                    case TokenType.Minus:
                    case TokenType.Star:
                    case TokenType.Slash:
                    case TokenType.SlashSlash:
                    case TokenType.Percent:
                    case TokenType.StarStar:
                        if (left.IsUnknown || right.IsUnknown)
                            return TypeSymbol.Unknown;
                        if (!left.IsNumeric || !right.IsNumeric)
                        {
                            AddInvalidBinaryOperandError(expr, left, right);
                            return TypeSymbol.Unknown;
                        }

                        if (expr.Op.Type == TokenType.Slash)
                            return TypeSymbol.Float;

                        return PromoteNumeric(left, right);

                    case TokenType.EqualEqual:
                    case TokenType.BangEqual:
                    case TokenType.Less:
                    case TokenType.Greater:
                    case TokenType.LessEqual:
                    case TokenType.GreaterEqual:
                    case TokenType.In:
                    case TokenType.Is:
                    case TokenType.And:
                    case TokenType.Or:
                        return TypeSymbol.Bool;

                    case TokenType.QuestionQuestion:
                        return MergeTypes(left, right);

                    default:
                        return TypeSymbol.Unknown;
                }
            }

            private static TypeSymbol PromoteNumeric(TypeSymbol left, TypeSymbol right)
            {
                if (left == null || right == null)
                    return TypeSymbol.Unknown;

                if (left.Name == "float" || right.Name == "float")
                    return TypeSymbol.Float;
                return TypeSymbol.Int;
            }

            private void AddInvalidBinaryOperandError(BinaryExpr expr, TypeSymbol left, TypeSymbol right)
            {
                string op = string.IsNullOrEmpty(expr.Op.Value)
                    ? expr.Op.Type.ToString()
                    : expr.Op.Value;

                AddError(
                    AnalyzeErrorCode.InvalidBinaryOperands,
                    "Operator '" + op + "' does not support operands '" + left.Name + "' and '" + right.Name + "'.",
                    expr.Op.Line,
                    expr.Op.Column);
            }

            private void CheckAssignmentCompatibility(
                TypeSymbol expected,
                TypeSymbol actual,
                int line,
                int column,
                string context)
            {
                if (expected == null || actual == null)
                    return;
                if (expected.IsUnknown || actual.IsUnknown)
                    return;
                if (AreTypesCompatible(expected, actual))
                    return;

                AddError(
                    AnalyzeErrorCode.TypeMismatch,
                    context + " expects '" + expected.Name + "' but got '" + actual.Name + "'.",
                    line,
                    column);
            }

            private void CheckReturnCompatibility(ReturnStmt returnStmt, TypeSymbol actualReturnType)
            {
                if (returnStmt == null || _functionContexts.Count == 0)
                    return;

                FunctionContext currentFunction = _functionContexts.Peek();
                if (currentFunction == null || currentFunction.ReturnType == null || currentFunction.ReturnType.IsUnknown)
                    return;

                CheckAssignmentCompatibility(
                    currentFunction.ReturnType,
                    actualReturnType ?? TypeSymbol.None,
                    returnStmt.Line,
                    returnStmt.Column,
                    "Return in function '" + currentFunction.Name + "'");
            }

            private void CheckConditionCompatibility(TypeSymbol conditionType, GrowlNode conditionNode, string contextName)
            {
                if (conditionNode == null || conditionType == null || conditionType.IsUnknown)
                    return;
                if (conditionType.IsBool)
                    return;

                AddError(
                    AnalyzeErrorCode.TypeMismatch,
                    contextName + " condition expects 'bool' but got '" + conditionType.Name + "'.",
                    conditionNode.Line,
                    conditionNode.Column);
            }

            private bool TryResolveCalledFunction(
                GrowlNode callee,
                Scope scope,
                out SymbolInfo functionSymbol,
                out FnDecl functionDecl)
            {
                functionSymbol = null;
                functionDecl = null;

                if (callee is NameExpr name)
                {
                    if (!_nameBindings.TryGetValue(name, out functionSymbol))
                    {
                        if (scope == null || !scope.TryResolve(name.Name, out functionSymbol))
                            return false;
                    }

                    if (functionSymbol != null &&
                        functionSymbol.Kind == SymbolKind.Function &&
                        functionSymbol.Declaration is FnDecl fn)
                    {
                        functionDecl = fn;
                        return true;
                    }
                }

                return false;
            }

            private void AnalyzeAndValidateCallArguments(CallExpr call, FnDecl callee, Scope scope)
            {
                if (call == null)
                    return;

                if (callee == null || callee.Params == null)
                {
                    for (int i = 0; i < call.Args.Count; i++)
                        AnalyzeExpression(call.Args[i].Value, scope);
                    return;
                }

                AnalyzeAndValidateCallArguments(call, callee, scope, skipLeadingSelfParameter: false);
            }

            private void AnalyzeAndValidateCallArguments(
                CallExpr call,
                FnDecl callee,
                Scope scope,
                bool skipLeadingSelfParameter)
            {
                if (call == null)
                    return;
                if (callee == null || callee.Params == null)
                {
                    for (int i = 0; i < call.Args.Count; i++)
                        AnalyzeExpression(call.Args[i].Value, scope);
                    return;
                }

                int startParamIndex = 0;
                if (skipLeadingSelfParameter &&
                    callee.Params.Count > 0 &&
                    (callee.Params[0].Name == "self" || callee.Params[0].Name == "cls"))
                {
                    startParamIndex = 1;
                }

                var parameters = new List<Param>();
                for (int i = startParamIndex; i < callee.Params.Count; i++)
                    parameters.Add(callee.Params[i]);

                var consumed = new bool[parameters.Count];
                int nextPositionalParam = 0;
                int variadicParamIndex = FindVariadicParameterIndex(parameters);
                bool reportedTooMany = false;

                for (int argIndex = 0; argIndex < call.Args.Count; argIndex++)
                {
                    var arg = call.Args[argIndex];
                    TypeSymbol argType = AnalyzeExpression(arg.Value, scope);

                    int paramIndex = -1;
                    Param targetParam = null;

                    if (!string.IsNullOrEmpty(arg.Name))
                    {
                        paramIndex = FindParameterByName(parameters, arg.Name);
                        if (paramIndex < 0)
                        {
                            AddError(
                                AnalyzeErrorCode.TypeMismatch,
                                "Call to function '" + callee.Name + "' has unknown argument '" + arg.Name + "'.",
                                call.Line,
                                call.Column);
                        }
                        else
                        {
                            targetParam = parameters[paramIndex];
                            if (!targetParam.IsVariadic)
                                consumed[paramIndex] = true;
                        }
                    }
                    else
                    {
                        while (nextPositionalParam < parameters.Count &&
                               consumed[nextPositionalParam] &&
                               !parameters[nextPositionalParam].IsVariadic)
                        {
                            nextPositionalParam++;
                        }

                        if (nextPositionalParam < parameters.Count)
                        {
                            paramIndex = nextPositionalParam;
                            targetParam = parameters[paramIndex];

                            if (!targetParam.IsVariadic)
                            {
                                consumed[paramIndex] = true;
                                nextPositionalParam++;
                            }
                        }
                        else if (variadicParamIndex >= 0)
                        {
                            paramIndex = variadicParamIndex;
                            targetParam = parameters[paramIndex];
                        }
                        else if (!reportedTooMany)
                        {
                            int max = CountMaximumArguments(parameters);
                            string maxText = max == int.MaxValue ? "unbounded" : max.ToString();
                            AddError(
                                AnalyzeErrorCode.TypeMismatch,
                                "Call to function '" + callee.Name + "' has too many arguments: expected at most " + maxText + " but got " + call.Args.Count + ".",
                                call.Line,
                                call.Column);
                            reportedTooMany = true;
                        }
                    }

                    if (targetParam == null)
                        continue;

                    TypeSymbol expected = ResolveTypeRef(targetParam.TypeAnnotation, scope);
                    CheckAssignmentCompatibility(
                        expected,
                        argType,
                        arg.Value != null ? arg.Value.Line : call.Line,
                        arg.Value != null ? arg.Value.Column : call.Column,
                        "Call to function '" + callee.Name + "' argument '" + targetParam.Name + "'");
                }

                var missingRequired = new List<string>();
                for (int i = 0; i < parameters.Count; i++)
                {
                    var p = parameters[i];
                    if (p.IsVariadic)
                        continue;
                    if (p.DefaultValue != null)
                        continue;
                    if (consumed[i])
                        continue;

                    missingRequired.Add(p.Name);
                }

                if (missingRequired.Count > 0)
                {
                    AddError(
                        AnalyzeErrorCode.TypeMismatch,
                        "Call to function '" + callee.Name + "' has too few arguments: missing " + string.Join(", ", missingRequired) + ".",
                        call.Line,
                        call.Column);
                }
            }

            private static int FindVariadicParameterIndex(List<Param> parameters)
            {
                if (parameters == null)
                    return -1;

                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].IsVariadic)
                        return i;
                }

                return -1;
            }

            private static int FindParameterByName(List<Param> parameters, string name)
            {
                if (parameters == null || string.IsNullOrEmpty(name))
                    return -1;

                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].Name == name)
                        return i;
                }

                return -1;
            }

            private static int CountMaximumArguments(List<Param> parameters)
            {
                if (parameters == null)
                    return 0;

                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].IsVariadic)
                        return int.MaxValue;
                }

                return parameters.Count;
            }

            private bool StatementsGuaranteeReturn(List<GrowlNode> statements)
            {
                if (statements == null || statements.Count == 0)
                    return false;

                for (int i = 0; i < statements.Count; i++)
                {
                    if (StatementGuaranteesReturn(statements[i]))
                        return true;
                }

                return false;
            }

            private bool StatementGuaranteesReturn(GrowlNode statement)
            {
                if (statement == null)
                    return false;

                switch (statement)
                {
                    case ReturnStmt _:
                        return true;

                    case IfStmt i:
                        {
                            if (!StatementsGuaranteeReturn(i.ThenBody))
                                return false;

                            for (int k = 0; k < i.ElifClauses.Count; k++)
                            {
                                if (!StatementsGuaranteeReturn(i.ElifClauses[k].Body))
                                    return false;
                            }

                            if (i.ElseBody == null || i.ElseBody.Count == 0)
                                return false;

                            return StatementsGuaranteeReturn(i.ElseBody);
                        }

                    case TryStmt t:
                        {
                            if (StatementsGuaranteeReturn(t.AlwaysBody))
                                return true;

                            bool tryReturns = StatementsGuaranteeReturn(t.TryBody);
                            bool recoverReturns = StatementsGuaranteeReturn(t.RecoverBody);
                            return tryReturns && recoverReturns;
                        }

                    case MatchStmt m:
                        {
                            if (m.Cases == null || m.Cases.Count == 0)
                                return false;

                            bool hasCatchAll = false;
                            for (int i = 0; i < m.Cases.Count; i++)
                            {
                                if (!StatementsGuaranteeReturn(m.Cases[i].Body))
                                    return false;

                                if (IsCatchAllPattern(m.Cases[i].Pattern))
                                    hasCatchAll = true;
                            }

                            return hasCatchAll;
                        }

                    default:
                        return false;
                }
            }

            private static bool IsCatchAllPattern(GrowlNode pattern)
            {
                if (pattern is NameExpr n)
                    return n.Name == "_";
                return false;
            }

            private static bool TryGetLiteralPatternKey(GrowlNode pattern, out string key)
            {
                key = null;
                if (pattern == null)
                    return false;

                switch (pattern)
                {
                    case IntegerLiteralExpr i:
                        key = "int:" + i.Value;
                        return true;
                    case FloatLiteralExpr f:
                        key = "float:" + f.Value;
                        return true;
                    case StringLiteralExpr s:
                        key = "string:" + s.Value;
                        return true;
                    case BoolLiteralExpr b:
                        key = "bool:" + b.Value;
                        return true;
                    case NoneLiteralExpr _:
                        key = "none";
                        return true;
                    case ColorLiteralExpr c:
                        key = "color:" + c.Hex;
                        return true;
                    default:
                        return false;
                }
            }

            private AggregateTypeInfo GetOrCreateAggregateType(string typeName)
            {
                if (string.IsNullOrEmpty(typeName))
                    return null;

                if (!_aggregateTypes.TryGetValue(typeName, out var info))
                {
                    info = new AggregateTypeInfo(typeName);
                    _aggregateTypes[typeName] = info;
                }

                return info;
            }

            private bool IsKnownAggregateType(TypeSymbol type)
            {
                if (type == null || string.IsNullOrEmpty(type.Name) || type.IsUnknown)
                    return false;

                return _aggregateTypes.ContainsKey(type.Name);
            }

            private bool TryResolveMember(TypeSymbol ownerType, string memberName, out MemberInfo member)
            {
                member = null;
                if (ownerType == null || ownerType.IsUnknown || string.IsNullOrEmpty(memberName))
                    return false;

                if (!_aggregateTypes.TryGetValue(ownerType.Name, out var aggregate) || aggregate == null)
                    return false;

                return aggregate.Members.TryGetValue(memberName, out member);
            }

            private static bool AreTypesCompatible(TypeSymbol expected, TypeSymbol actual)
            {
                if (expected == null || actual == null)
                    return true;
                if (expected.IsUnknown || actual.IsUnknown)
                    return true;
                if (expected.Name == actual.Name)
                    return true;
                if (TypeSymbol.TryGetListElementType(expected, out var expectedElement) &&
                    TypeSymbol.TryGetListElementType(actual, out var actualElement))
                {
                    return AreTypesCompatible(expectedElement, actualElement);
                }
                if (TypeSymbol.TryGetDictTypes(expected, out var expectedKey, out var expectedValue) &&
                    TypeSymbol.TryGetDictTypes(actual, out var actualKey, out var actualValue))
                {
                    return AreTypesCompatible(expectedKey, actualKey) &&
                           AreTypesCompatible(expectedValue, actualValue);
                }
                if (expected.Name == "float" && actual.Name == "int")
                    return true;
                return false;
            }

            private SymbolInfo Declare(
                Scope scope,
                string name,
                SymbolKind kind,
                int line,
                int column,
                GrowlNode declaration)
            {
                if (string.IsNullOrEmpty(name))
                    return null;

                var symbol = new SymbolInfo(name, kind, line, column, declaration);
                if (!scope.TryDeclare(symbol))
                {
                    AddError(
                        AnalyzeErrorCode.DuplicateSymbol,
                        "Duplicate symbol '" + name + "'.",
                        line,
                        column);
                    return null;
                }

                bool initiallyAssigned = kind == SymbolKind.Parameter || kind == SymbolKind.Const;
                TrackDeclaredSymbol(scope, symbol, initiallyAssigned);
                return symbol;
            }

            private void AddError(AnalyzeErrorCode code, string message, int line, int column)
            {
                _errors.Add(new AnalyzeError(code, message, line, column));
            }
        }
    }
}
