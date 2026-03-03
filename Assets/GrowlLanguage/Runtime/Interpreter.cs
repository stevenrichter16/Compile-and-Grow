using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using GrowlLanguage.AST;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.Runtime
{
    internal sealed class RuntimeExecutionException : Exception
    {
        public int Line { get; }
        public int Column { get; }

        public RuntimeExecutionException(string message, int line, int column)
            : base(message)
        {
            Line = line;
            Column = column;
        }
    }

    internal sealed class ReturnSignal : Exception
    {
        public object Value { get; }

        public ReturnSignal(object value)
        {
            Value = value;
        }
    }

    internal sealed class BreakSignal : Exception
    {
    }

    internal sealed class ContinueSignal : Exception
    {
    }

    internal readonly struct RuntimeArgument
    {
        public string Name { get; }
        public object Value { get; }

        public RuntimeArgument(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    internal interface IRuntimeCallable
    {
        string Name { get; }
        object Invoke(Interpreter interpreter, List<RuntimeArgument> args, GrowlNode callSite);
    }

    internal sealed class RuntimeBuiltinFunction : IRuntimeCallable
    {
        private readonly Func<Interpreter, List<RuntimeArgument>, GrowlNode, object> _impl;

        public string Name { get; }

        public RuntimeBuiltinFunction(string name, Func<Interpreter, List<RuntimeArgument>, GrowlNode, object> impl)
        {
            Name = name;
            _impl = impl;
        }

        public object Invoke(Interpreter interpreter, List<RuntimeArgument> args, GrowlNode callSite)
        {
            return _impl(interpreter, args, callSite);
        }
    }

    internal sealed class RuntimeUserFunction : IRuntimeCallable
    {
        public string Name { get; }
        public List<Param> Parameters { get; }
        public List<GrowlNode> StatementBody { get; }
        public GrowlNode ExpressionBody { get; }
        public RuntimeEnvironment Closure { get; }

        public RuntimeUserFunction(
            string name,
            List<Param> parameters,
            List<GrowlNode> statementBody,
            GrowlNode expressionBody,
            RuntimeEnvironment closure)
        {
            Name = string.IsNullOrEmpty(name) ? "<lambda>" : name;
            Parameters = parameters ?? new List<Param>();
            StatementBody = statementBody;
            ExpressionBody = expressionBody;
            Closure = closure;
        }

        public object Invoke(Interpreter interpreter, List<RuntimeArgument> args, GrowlNode callSite)
        {
            return interpreter.InvokeUserFunction(this, args, callSite);
        }

        public static RuntimeUserFunction FromFunctionDeclaration(FnDecl fnDecl, RuntimeEnvironment closure)
        {
            return new RuntimeUserFunction(
                fnDecl.Name,
                fnDecl.Params,
                fnDecl.Body,
                expressionBody: null,
                closure: closure);
        }

        public static RuntimeUserFunction FromLambda(LambdaExpr lambda, RuntimeEnvironment closure)
        {
            if (lambda.Body is ProgramNode program)
            {
                return new RuntimeUserFunction(
                    "<lambda>",
                    lambda.Params,
                    program.Statements,
                    expressionBody: null,
                    closure: closure);
            }

            return new RuntimeUserFunction(
                "<lambda>",
                lambda.Params,
                statementBody: null,
                expressionBody: lambda.Body,
                closure: closure);
        }
    }

    internal sealed class RuntimeStructType : IRuntimeCallable
    {
        public string Name { get; }
        public List<FieldDecl> Fields { get; }
        public RuntimeEnvironment Closure { get; }

        public RuntimeStructType(string name, List<FieldDecl> fields, RuntimeEnvironment closure)
        {
            Name = name;
            Fields = fields ?? new List<FieldDecl>();
            Closure = closure;
        }

        public object Invoke(Interpreter interpreter, List<RuntimeArgument> args, GrowlNode callSite)
        {
            return interpreter.InvokeStructConstructor(this, args, callSite);
        }
    }

    internal sealed class RuntimeEnvironment
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.Ordinal);

        public RuntimeEnvironment Parent { get; }

        public RuntimeEnvironment(RuntimeEnvironment parent)
        {
            Parent = parent;
        }

        public void Define(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
                return;

            _values[name] = value;
        }

        public bool TryGet(string name, out object value)
        {
            for (RuntimeEnvironment env = this; env != null; env = env.Parent)
            {
                if (env._values.TryGetValue(name, out value))
                    return true;
            }

            value = null;
            return false;
        }

        public bool TryAssign(string name, object value)
        {
            for (RuntimeEnvironment env = this; env != null; env = env.Parent)
            {
                if (env._values.ContainsKey(name))
                {
                    env._values[name] = value;
                    return true;
                }
            }

            return false;
        }

        public void AssignOrDefine(string name, object value)
        {
            if (!TryAssign(name, value))
                Define(name, value);
        }

        public Dictionary<string, object> SnapshotLocalValues()
        {
            return new Dictionary<string, object>(_values, StringComparer.Ordinal);
        }

        public void RestoreLocalValues(Dictionary<string, object> snapshot)
        {
            _values.Clear();
            if (snapshot == null)
                return;

            foreach (KeyValuePair<string, object> pair in snapshot)
                _values[pair.Key] = pair.Value;
        }
    }

    internal sealed class Interpreter
    {
        private readonly RuntimeOptions _options;
        private readonly IGrowlRuntimeHost _host;
        private readonly BiologicalContext _bioContext;
        private readonly List<string> _outputLines;
        private readonly RuntimeEnvironment _globals;
        private RuntimeEnvironment _environment;
        private object _lastValue;
        private int _loopIterations;

        public Interpreter(RuntimeOptions options, List<string> outputLines)
        {
            _options = options ?? new RuntimeOptions();
            _host = _options.Host;
            _bioContext = _options.BioContext;
            _outputLines = outputLines ?? new List<string>();
            _globals = new RuntimeEnvironment(parent: null);
            _environment = _globals;

            RegisterBuiltins();
            SeedHostGlobals();
        }

        public object Execute(ProgramNode program)
        {
            if (program == null)
                return null;

            ExecuteBlock(program.Statements, _globals);

            if (_options.AutoInvokeEntryFunction &&
                !string.IsNullOrEmpty(_options.EntryFunctionName) &&
                _globals.TryGet(_options.EntryFunctionName, out object callableValue) &&
                callableValue is IRuntimeCallable callable)
            {
                _lastValue = callable.Invoke(this, new List<RuntimeArgument>(), program);
            }

            return _lastValue;
        }

        internal object InvokeUserFunction(RuntimeUserFunction function, List<RuntimeArgument> args, GrowlNode callSite)
        {
            var callEnv = new RuntimeEnvironment(function.Closure);
            callEnv.Define(function.Name, function);

            BindFunctionArguments(function, args, callEnv, callSite);

            try
            {
                if (function.StatementBody != null)
                {
                    ExecuteBlock(function.StatementBody, callEnv);
                    return null;
                }

                return EvaluateInEnvironment(function.ExpressionBody, callEnv);
            }
            catch (ReturnSignal ret)
            {
                return ret.Value;
            }
        }

        internal object InvokeStructConstructor(RuntimeStructType structType, List<RuntimeArgument> args, GrowlNode callSite)
        {
            var instance = new Dictionary<object, object>();
            instance["__type"] = structType.Name;

            int positionalCount = 0;
            for (int i = 0; i < args.Count; i++)
            {
                if (string.IsNullOrEmpty(args[i].Name))
                    positionalCount++;
            }

            if (positionalCount > structType.Fields.Count)
            {
                RuntimeError(
                    "Struct '" + structType.Name + "' expected at most " + structType.Fields.Count + " positional arguments but got " + positionalCount + ".",
                    callSite);
            }

            for (int i = 0; i < structType.Fields.Count; i++)
            {
                FieldDecl field = structType.Fields[i];
                object defaultValue = EvaluateInEnvironment(field.DefaultValue, structType.Closure);
                instance[field.Name] = defaultValue;
            }

            int nextPositionalField = 0;
            for (int i = 0; i < args.Count; i++)
            {
                RuntimeArgument arg = args[i];
                if (string.IsNullOrEmpty(arg.Name))
                {
                    FieldDecl field = structType.Fields[nextPositionalField];
                    instance[field.Name] = arg.Value;
                    nextPositionalField++;
                    continue;
                }

                bool foundField = false;
                for (int f = 0; f < structType.Fields.Count; f++)
                {
                    if (structType.Fields[f].Name == arg.Name)
                    {
                        instance[arg.Name] = arg.Value;
                        foundField = true;
                        break;
                    }
                }

                if (!foundField)
                {
                    RuntimeError(
                        "Struct '" + structType.Name + "' has no field named '" + arg.Name + "'.",
                        callSite);
                }
            }

            return instance;
        }

        private void RegisterBuiltins()
        {
            _globals.Define("print", new RuntimeBuiltinFunction("print", BuiltinPrint));
            _globals.Define("log", new RuntimeBuiltinFunction("log", BuiltinPrint));
            _globals.Define("len", new RuntimeBuiltinFunction("len", BuiltinLen));
            _globals.Define("type", new RuntimeBuiltinFunction("type", BuiltinType));

            RegisterHostBuiltin("world_get");
            RegisterHostBuiltin("world_set");
            RegisterHostBuiltin("world_add");
            RegisterHostBuiltin("org_get");
            RegisterHostBuiltin("org_set");
            RegisterHostBuiltin("org_add");
            RegisterHostBuiltin("org_damage");
            RegisterHostBuiltin("org_heal");
            RegisterHostBuiltin("org_memory_get");
            RegisterHostBuiltin("org_memory_set");
            RegisterHostBuiltin("seed_get");
            RegisterHostBuiltin("seed_set");
            RegisterHostBuiltin("seed_add");
            RegisterHostBuiltin("emit_signal");
            RegisterHostBuiltin("spawn_seed");
        }

        private void RegisterHostBuiltin(string name)
        {
            _globals.Define(name, new RuntimeBuiltinFunction(
                name,
                (_, args, site) => InvokeHostBuiltin(name, args, site)));
        }

        private void SeedHostGlobals()
        {
            if (_host == null)
                return;

            var hostGlobals = new Dictionary<string, object>(StringComparer.Ordinal);
            _host.PopulateGlobals(hostGlobals);
            foreach (KeyValuePair<string, object> pair in hostGlobals)
                _globals.Define(pair.Key, pair.Value);
        }

        private object InvokeHostBuiltin(string name, List<RuntimeArgument> args, GrowlNode site)
        {
            if (_host == null)
            {
                RuntimeError(
                    "Builtin '" + name + "' requires a runtime host bridge, but no host is configured.",
                    site);
            }

            var converted = new List<RuntimeCallArgument>(args.Count);
            for (int i = 0; i < args.Count; i++)
                converted.Add(new RuntimeCallArgument(args[i].Name, args[i].Value));

            if (_host.TryInvokeBuiltin(name, converted, out object result, out string errorMessage))
                return result;

            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = "Runtime host does not implement builtin '" + name + "'.";

            RuntimeError(errorMessage, site);
            return null;
        }

        private object BuiltinPrint(Interpreter _, List<RuntimeArgument> args, GrowlNode __)
        {
            var parts = new List<string>(args.Count);
            for (int i = 0; i < args.Count; i++)
            {
                object value = args[i].Value;
                parts.Add(value is string s ? s : RuntimeValueFormatter.Format(value));
            }

            _outputLines.Add(string.Join(" ", parts));
            return null;
        }

        private object BuiltinLen(Interpreter _, List<RuntimeArgument> args, GrowlNode callSite)
        {
            if (args.Count != 1)
                RuntimeError("len() expects exactly one argument.", callSite);

            object value = args[0].Value;
            if (value is string s)
                return (long)s.Length;
            if (value is IList list)
                return (long)list.Count;
            if (value is IDictionary dict)
                return (long)dict.Count;
            if (value is GrowlTuple tuple)
                return (long)tuple.Elements.Count;
            if (value is GrowlSet set)
                return (long)set.Elements.Count;

            RuntimeError("len() does not support value of type '" + GetTypeName(value) + "'.", callSite);
            return null;
        }

        private object BuiltinType(Interpreter _, List<RuntimeArgument> args, GrowlNode callSite)
        {
            if (args.Count != 1)
                RuntimeError("type() expects exactly one argument.", callSite);

            return GetTypeName(args[0].Value);
        }

        private void BindFunctionArguments(
            RuntimeUserFunction function,
            List<RuntimeArgument> args,
            RuntimeEnvironment callEnv,
            GrowlNode callSite)
        {
            var assigned = new Dictionary<string, object>(StringComparer.Ordinal);
            var variadicAssigned = new Dictionary<string, List<object>>(StringComparer.Ordinal);
            int nextPositionalParam = 0;

            for (int i = 0; i < args.Count; i++)
            {
                RuntimeArgument arg = args[i];

                if (!string.IsNullOrEmpty(arg.Name))
                {
                    int namedIndex = FindParameter(function.Parameters, arg.Name);
                    if (namedIndex < 0)
                    {
                        RuntimeError(
                            "Function '" + function.Name + "' has no parameter named '" + arg.Name + "'.",
                            callSite);
                    }

                    Param namedParam = function.Parameters[namedIndex];
                    if (namedParam.IsVariadic)
                    {
                        if (!variadicAssigned.TryGetValue(namedParam.Name, out List<object> namedList))
                        {
                            namedList = new List<object>();
                            variadicAssigned[namedParam.Name] = namedList;
                        }

                        namedList.Add(arg.Value);
                        continue;
                    }

                    if (assigned.ContainsKey(namedParam.Name))
                    {
                        RuntimeError(
                            "Function '" + function.Name + "' received duplicate argument for parameter '" + namedParam.Name + "'.",
                            callSite);
                    }

                    assigned[namedParam.Name] = arg.Value;
                    continue;
                }

                while (nextPositionalParam < function.Parameters.Count)
                {
                    Param candidate = function.Parameters[nextPositionalParam];
                    if (candidate.IsVariadic)
                        break;

                    if (assigned.ContainsKey(candidate.Name))
                    {
                        nextPositionalParam++;
                        continue;
                    }

                    break;
                }

                if (nextPositionalParam >= function.Parameters.Count)
                {
                    RuntimeError(
                        "Function '" + function.Name + "' received too many positional arguments.",
                        callSite);
                }

                Param positionalParam = function.Parameters[nextPositionalParam];
                if (positionalParam.IsVariadic)
                {
                    if (!variadicAssigned.TryGetValue(positionalParam.Name, out List<object> list))
                    {
                        list = new List<object>();
                        variadicAssigned[positionalParam.Name] = list;
                    }

                    list.Add(arg.Value);
                }
                else
                {
                    assigned[positionalParam.Name] = arg.Value;
                    nextPositionalParam++;
                }
            }

            for (int p = 0; p < function.Parameters.Count; p++)
            {
                Param param = function.Parameters[p];
                if (param.IsVariadic)
                {
                    if (!variadicAssigned.TryGetValue(param.Name, out List<object> variadicValues))
                        variadicValues = new List<object>();

                    callEnv.Define(param.Name, variadicValues);
                    continue;
                }

                if (assigned.TryGetValue(param.Name, out object explicitValue))
                {
                    callEnv.Define(param.Name, explicitValue);
                    continue;
                }

                if (param.DefaultValue != null)
                {
                    object defaultValue = EvaluateInEnvironment(param.DefaultValue, callEnv);
                    callEnv.Define(param.Name, defaultValue);
                    continue;
                }

                RuntimeError(
                    "Function '" + function.Name + "' is missing required argument '" + param.Name + "'.",
                    callSite);
            }
        }

        private static int FindParameter(List<Param> parameters, string name)
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

        private void ExecuteBlock(List<GrowlNode> statements, RuntimeEnvironment env)
        {
            if (statements == null)
                return;

            RuntimeEnvironment previous = _environment;
            _environment = env;
            try
            {
                for (int i = 0; i < statements.Count; i++)
                    ExecuteStatement(statements[i]);
            }
            finally
            {
                _environment = previous;
            }
        }

        private void ExecuteStatement(GrowlNode node)
        {
            if (node == null)
                return;

            switch (node)
            {
                case FnDecl fn:
                    _environment.Define(fn.Name, RuntimeUserFunction.FromFunctionDeclaration(fn, _environment));
                    return;

                case GeneDecl gene:
                    _environment.Define(gene.Fn.Name, RuntimeUserFunction.FromFunctionDeclaration(gene.Fn, _environment));
                    return;

                case StructDecl structDecl:
                    _environment.Define(structDecl.Name, new RuntimeStructType(structDecl.Name, structDecl.Fields, _environment));
                    return;

                case EnumDecl enumDecl:
                    {
                        var enumMap = new Dictionary<object, object>();
                        enumMap["__type"] = enumDecl.Name;
                        for (int i = 0; i < enumDecl.Members.Count; i++)
                        {
                            EnumMember member = enumDecl.Members[i];
                            enumMap[member.Name] = member.Value != null ? Evaluate(member.Value) : member.Name;
                        }

                        _environment.Define(enumDecl.Name, enumMap);
                    }
                    return;

                case ClassDecl classDecl:
                case TraitDecl _:
                case MixinDecl _:
                case TypeAliasDecl _:
                case ModuleDecl _:
                case ImportStmt _:
                    // Declarations are currently no-op at runtime unless they are callable.
                    if (node is ClassDecl cd)
                        _environment.Define(cd.Name, cd.Name);
                    return;

                case ConstDecl constDecl:
                    _environment.Define(constDecl.Name, Evaluate(constDecl.Value));
                    return;

                case AssignStmt assign:
                    ExecuteAssignment(assign);
                    return;

                case ExprStmt exprStmt:
                    _lastValue = Evaluate(exprStmt.Expression);
                    return;

                case ReturnStmt returnStmt:
                    throw new ReturnSignal(Evaluate(returnStmt.Value));

                case BreakStmt _:
                    throw new BreakSignal();

                case ContinueStmt _:
                    throw new ContinueSignal();

                case YieldStmt yieldStmt:
                    _lastValue = Evaluate(yieldStmt.Value);
                    return;

                case WaitStmt waitStmt:
                    Evaluate(waitStmt.Ticks);
                    return;

                case DeferStmt deferStmt:
                    Evaluate(deferStmt.Duration);
                    ExecuteBlock(deferStmt.Body, new RuntimeEnvironment(_environment));
                    return;

                case MutateStmt mutateStmt:
                    {
                        object value = Evaluate(mutateStmt.Value);
                        Evaluate(mutateStmt.Interval);
                        WriteAssignmentTarget(mutateStmt.Target, value, mutateStmt);
                    }
                    return;

                case IfStmt ifStmt:
                    ExecuteIf(ifStmt);
                    return;

                case ForStmt forStmt:
                    ExecuteFor(forStmt);
                    return;

                case WhileStmt whileStmt:
                    ExecuteWhile(whileStmt);
                    return;

                case LoopStmt loopStmt:
                    ExecuteLoop(loopStmt);
                    return;

                case MatchStmt matchStmt:
                    ExecuteMatch(matchStmt);
                    return;

                case TryStmt tryStmt:
                    ExecuteTry(tryStmt);
                    return;

                case PhaseBlock phaseBlock:
                    ExecutePhaseBlock(phaseBlock);
                    return;

                case WhenBlock whenBlock:
                    ExecuteWhenBlock(whenBlock);
                    return;

                case RespondBlock respondBlock:
                    ExecuteRespondBlock(respondBlock);
                    return;

                case AdaptBlock adapt:
                    ExecuteAdaptBlock(adapt);
                    return;

                case CycleBlock cycle:
                    ExecuteCycleBlock(cycle);
                    return;

                case TickerDecl ticker:
                    ExecuteTickerDecl(ticker);
                    return;
            }

            _lastValue = Evaluate(node);
        }

        private void ExecuteAssignment(AssignStmt assign)
        {
            object value = Evaluate(assign.Value);
            if (assign.Op.Type == TokenType.Equal)
            {
                WriteAssignmentTarget(assign.Target, value, assign);
                return;
            }

            object current = ReadAssignmentTarget(assign.Target, assign);
            TokenType mappedBinary = MapCompoundOperator(assign.Op.Type, assign);
            var syntheticOp = new Token(mappedBinary, assign.Op.Value, assign.Op.Line, assign.Op.Column);
            object next = ApplyBinaryOperator(syntheticOp, current, value, assign);
            WriteAssignmentTarget(assign.Target, next, assign);
        }

        private static TokenType MapCompoundOperator(TokenType tokenType, GrowlNode node)
        {
            switch (tokenType)
            {
                case TokenType.PlusEqual:
                    return TokenType.Plus;
                case TokenType.MinusEqual:
                    return TokenType.Minus;
                case TokenType.StarEqual:
                    return TokenType.Star;
                case TokenType.SlashEqual:
                    return TokenType.Slash;
                case TokenType.SlashSlashEqual:
                    return TokenType.SlashSlash;
                case TokenType.PercentEqual:
                    return TokenType.Percent;
                case TokenType.StarStarEqual:
                    return TokenType.StarStar;
                default:
                    throw new RuntimeExecutionException(
                        "Unsupported assignment operator '" + tokenType + "'.",
                        node.Line,
                        node.Column);
            }
        }

        private object ReadAssignmentTarget(GrowlNode target, GrowlNode site)
        {
            switch (target)
            {
                case NameExpr name:
                    if (_environment.TryGet(name.Name, out object value))
                        return value;

                    RuntimeError("Cannot apply compound assignment to undefined variable '" + name.Name + "'.", site);
                    return null;

                case AttributeExpr attr:
                    return ReadAttribute(attr);

                case SubscriptExpr sub:
                    return ReadSubscript(sub);

                default:
                    RuntimeError("Invalid assignment target.", site);
                    return null;
            }
        }

        // ── Biological construct implementations ─────────────────────

        private void ExecutePhaseBlock(PhaseBlock phaseBlock)
        {
            if (phaseBlock.Condition != null)
            {
                // Conditional phase: execute body only when condition is true
                if (IsTruthy(Evaluate(phaseBlock.Condition)))
                    ExecuteBlock(phaseBlock.Body, new RuntimeEnvironment(_environment));
                return;
            }

            // Age-range phase: check organism maturity against (MinAge, MaxAge)
            double minAge = ToDouble(Evaluate(phaseBlock.MinAge));
            double maxAge = ToDouble(Evaluate(phaseBlock.MaxAge));

            double maturity = 0d;
            if (_host != null)
            {
                var matArgs = new List<RuntimeCallArgument> { new RuntimeCallArgument("key", "maturity") };
                if (_host.TryInvokeBuiltin("org_get", matArgs, out object matVal, out _) && matVal != null)
                    ToDouble(matVal, out maturity);
            }

            if (maturity >= minAge && maturity <= maxAge)
                ExecuteBlock(phaseBlock.Body, new RuntimeEnvironment(_environment));
        }

        private void ExecuteWhenBlock(WhenBlock whenBlock)
        {
            bool currentTrue = IsTruthy(Evaluate(whenBlock.Condition));

            if (_bioContext == null)
            {
                // No bio context — fall back to level detection
                if (currentTrue)
                    ExecuteBlock(whenBlock.Body, new RuntimeEnvironment(_environment));
                return;
            }

            // Edge detection: use source location as key
            string key = whenBlock.Line + ":" + whenBlock.Column;
            _bioContext.WhenPreviousState.TryGetValue(key, out bool previousTrue);

            if (currentTrue && !previousTrue)
            {
                // Rising edge — condition just became true
                ExecuteBlock(whenBlock.Body, new RuntimeEnvironment(_environment));
            }
            else if (!currentTrue && previousTrue && whenBlock.ThenBlock != null && whenBlock.ThenBlock.Count > 0)
            {
                // Falling edge — condition just became false, fire then block
                ExecuteBlock(whenBlock.ThenBlock, new RuntimeEnvironment(_environment));
            }

            _bioContext.WhenPreviousState[key] = currentTrue;
        }

        private void ExecuteRespondBlock(RespondBlock respondBlock)
        {
            if (_bioContext == null)
                return; // No bio context — cannot dispatch events

            if (!_bioContext.PendingEvents.TryGetValue(respondBlock.EventName, out List<object> events))
                return; // No events queued for this handler

            if (events.Count == 0)
                return;

            // Dispatch each queued event
            for (int i = 0; i < events.Count; i++)
            {
                var respondEnv = new RuntimeEnvironment(_environment);
                if (!string.IsNullOrEmpty(respondBlock.Binding))
                    respondEnv.Define(respondBlock.Binding, events[i]);

                ExecuteBlock(respondBlock.Body, respondEnv);
            }

            // Consume events after dispatch
            events.Clear();
        }

        private void ExecuteAdaptBlock(AdaptBlock adapt)
        {
            // Get current value of the subject
            object currentObj = Evaluate(adapt.Subject);
            if (!ToDouble(currentObj, out double currentValue))
                return;

            // Find target: first matching rule wins
            double targetValue = currentValue;
            bool found = false;
            for (int i = 0; i < adapt.Rules.Count; i++)
            {
                GrowlNode condition = adapt.Rules[i].Condition;
                bool matches;

                // otherwise rule: NoneLiteralExpr or null condition = always true
                if (condition == null || condition is NoneLiteralExpr)
                    matches = true;
                else
                    matches = IsTruthy(Evaluate(condition));

                if (matches)
                {
                    object actionVal = Evaluate(adapt.Rules[i].Action);
                    if (ToDouble(actionVal, out double tv))
                        targetValue = tv;
                    found = true;
                    break;
                }
            }

            if (!found)
                return;

            // Get rate (max change per tick)
            double rate = 1d;
            if (adapt.Budget != null)
            {
                object budgetVal = Evaluate(adapt.Budget);
                if (ToDouble(budgetVal, out double r))
                    rate = Math.Abs(r);
            }

            // Apply rate-limited interpolation
            double delta = targetValue - currentValue;
            if (Math.Abs(delta) > rate)
                delta = delta > 0 ? rate : -rate;

            double newValue = currentValue + delta;

            // Write back to the subject
            WriteAssignmentTarget(adapt.Subject, newValue, adapt);
        }

        private void ExecuteCycleBlock(CycleBlock cycle)
        {
            if (_bioContext == null)
            {
                // No bio context — fall back to running first point
                if (cycle.Points.Count > 0)
                    ExecuteBlock(cycle.Points[0].Body, new RuntimeEnvironment(_environment));
                return;
            }

            long period = ToLong(Evaluate(cycle.Period));
            if (period <= 0) period = 1;

            // Initialize cycle start tick if missing
            if (!_bioContext.CycleStartTicks.TryGetValue(cycle.Name, out long startTick))
            {
                startTick = _bioContext.CurrentTick;
                _bioContext.CycleStartTicks[cycle.Name] = startTick;
            }

            long position = (_bioContext.CurrentTick - startTick) % period;

            // Find the active point: last point whose At value <= current position
            int activeIndex = -1;
            for (int i = 0; i < cycle.Points.Count; i++)
            {
                long atValue = ToLong(Evaluate(cycle.Points[i].At));
                if (atValue <= position)
                    activeIndex = i;
            }

            if (activeIndex >= 0)
                ExecuteBlock(cycle.Points[activeIndex].Body, new RuntimeEnvironment(_environment));
        }

        private void ExecuteTickerDecl(TickerDecl ticker)
        {
            long interval = ToLong(Evaluate(ticker.Interval));
            if (interval <= 0) interval = 1;

            if (_bioContext == null)
            {
                // No bio context — fall back to always executing
                ExecuteBlock(ticker.Body, new RuntimeEnvironment(_environment));
                return;
            }

            _bioContext.TickerLastFired.TryGetValue(ticker.Name, out long lastFired);

            if (_bioContext.CurrentTick - lastFired >= interval)
            {
                // Deduct 0.5 energy cost
                if (_host != null)
                {
                    var costArgs = new List<RuntimeCallArgument>
                    {
                        new RuntimeCallArgument("key", "energy"),
                        new RuntimeCallArgument("delta", -0.5),
                    };
                    _host.TryInvokeBuiltin("org_add", costArgs, out _, out _);
                }

                ExecuteBlock(ticker.Body, new RuntimeEnvironment(_environment));
                _bioContext.TickerLastFired[ticker.Name] = _bioContext.CurrentTick;
            }
        }

        // Numeric conversion helpers for biological constructs
        private static double ToDouble(object value)
        {
            ToDouble(value, out double d);
            return d;
        }

        private static bool ToDouble(object value, out double number)
        {
            switch (value)
            {
                case sbyte v: number = v; return true;
                case byte v: number = v; return true;
                case short v: number = v; return true;
                case ushort v: number = v; return true;
                case int v: number = v; return true;
                case uint v: number = v; return true;
                case long v: number = v; return true;
                case ulong v: number = v; return true;
                case float v: number = v; return true;
                case double v: number = v; return true;
                case decimal v: number = (double)v; return true;
                default:
                    number = 0d;
                    return false;
            }
        }

        private static long ToLong(object value)
        {
            if (ToDouble(value, out double d))
                return (long)Math.Round(d);
            return 0L;
        }

        // ── Assignment helpers ──────────────────────────────────────────

        private void WriteAssignmentTarget(GrowlNode target, object value, GrowlNode site)
        {
            switch (target)
            {
                case NameExpr name:
                    _environment.AssignOrDefine(name.Name, value);
                    return;

                case AttributeExpr attr:
                    {
                        object owner = Evaluate(attr.Object);
                        if (owner is IDictionary dictionary)
                        {
                            SetDictionaryValue(dictionary, attr.FieldName, value);
                            return;
                        }

                        RuntimeError("Cannot assign attribute on value of type '" + GetTypeName(owner) + "'.", site);
                        return;
                    }

                case SubscriptExpr sub:
                    {
                        object owner = Evaluate(sub.Object);
                        object key = Evaluate(sub.Key);

                        if (owner is IList list)
                        {
                            long index = ExpectInteger(key, site, "List index must be an integer.");
                            int intIndex = NormalizeIndex(list.Count, index, site);
                            list[intIndex] = value;
                            return;
                        }

                        if (owner is IDictionary dictionary)
                        {
                            SetDictionaryValue(dictionary, key, value);
                            return;
                        }

                        RuntimeError("Cannot assign subscript on value of type '" + GetTypeName(owner) + "'.", site);
                        return;
                    }

                default:
                    RuntimeError("Left side of assignment must be name, attribute, or subscript.", site);
                    return;
            }
        }

        private void ExecuteIf(IfStmt ifStmt)
        {
            if (IsTruthy(Evaluate(ifStmt.Condition)))
            {
                ExecuteBlock(ifStmt.ThenBody, _environment);
                return;
            }

            for (int i = 0; i < ifStmt.ElifClauses.Count; i++)
            {
                ElifClause clause = ifStmt.ElifClauses[i];
                if (IsTruthy(Evaluate(clause.Condition)))
                {
                    ExecuteBlock(clause.Body, _environment);
                    return;
                }
            }

            ExecuteBlock(ifStmt.ElseBody, _environment);
        }

        private void ExecuteFor(ForStmt forStmt)
        {
            object iterableValue = Evaluate(forStmt.Iterable);
            IEnumerable<object> items = AsEnumerable(iterableValue, forStmt.Iterable);

            bool ranBody = false;
            bool broke = false;
            var loopScope = new RuntimeEnvironment(_environment);

            foreach (object item in items)
            {
                EnsureLoopBudget(forStmt);
                ranBody = true;
                BindTargets(loopScope, forStmt.Targets, item, forStmt);

                try
                {
                    ExecuteBlock(forStmt.Body, loopScope);
                }
                catch (ContinueSignal)
                {
                    continue;
                }
                catch (BreakSignal)
                {
                    broke = true;
                    break;
                }
            }

            if (!ranBody || broke)
            {
                if (!broke)
                    ExecuteBlock(forStmt.ElseBody, _environment);
            }
        }

        private void ExecuteWhile(WhileStmt whileStmt)
        {
            while (IsTruthy(Evaluate(whileStmt.Condition)))
            {
                EnsureLoopBudget(whileStmt);
                try
                {
                    ExecuteBlock(whileStmt.Body, _environment);
                }
                catch (ContinueSignal)
                {
                    continue;
                }
                catch (BreakSignal)
                {
                    break;
                }
            }
        }

        private void ExecuteLoop(LoopStmt loopStmt)
        {
            while (true)
            {
                EnsureLoopBudget(loopStmt);
                try
                {
                    ExecuteBlock(loopStmt.Body, _environment);
                }
                catch (ContinueSignal)
                {
                    continue;
                }
                catch (BreakSignal)
                {
                    break;
                }
            }
        }

        private void ExecuteMatch(MatchStmt matchStmt)
        {
            object subject = Evaluate(matchStmt.Subject);

            for (int i = 0; i < matchStmt.Cases.Count; i++)
            {
                CaseClause caseClause = matchStmt.Cases[i];
                var caseScope = new RuntimeEnvironment(_environment);

                if (!TryMatchPattern(caseClause.Pattern, subject, caseScope))
                    continue;

                if (caseClause.Guard != null && !IsTruthy(EvaluateInEnvironment(caseClause.Guard, caseScope)))
                    continue;

                ExecuteBlock(caseClause.Body, caseScope);
                return;
            }
        }

        private void ExecuteTry(TryStmt tryStmt)
        {
            RuntimeExecutionException failure = null;

            try
            {
                ExecuteBlock(tryStmt.TryBody, _environment);
            }
            catch (RuntimeExecutionException ex)
            {
                failure = ex;
                var recoverScope = new RuntimeEnvironment(_environment);
                if (!string.IsNullOrEmpty(tryStmt.ErrorName))
                    recoverScope.Define(tryStmt.ErrorName, ex.Message);

                ExecuteBlock(tryStmt.RecoverBody, recoverScope);
            }
            finally
            {
                ExecuteBlock(tryStmt.AlwaysBody, _environment);
            }

            if (failure != null && (tryStmt.RecoverBody == null || tryStmt.RecoverBody.Count == 0))
                throw failure;
        }

        private object Evaluate(GrowlNode node)
        {
            if (node == null)
                return null;

            switch (node)
            {
                case IntegerLiteralExpr integerLiteral:
                    return integerLiteral.Value;

                case FloatLiteralExpr floatLiteral:
                    return floatLiteral.Value;

                case StringLiteralExpr stringLiteral:
                    return stringLiteral.Value;

                case BoolLiteralExpr boolLiteral:
                    return boolLiteral.Value;

                case NoneLiteralExpr _:
                    return null;

                case ColorLiteralExpr colorLiteral:
                    return colorLiteral.Hex;

                case InterpolatedStringExpr interpolated:
                    {
                        var pieces = new List<string>(interpolated.Segments.Count);
                        for (int i = 0; i < interpolated.Segments.Count; i++)
                        {
                            object segmentValue = Evaluate(interpolated.Segments[i]);
                            pieces.Add(segmentValue is string s ? s : RuntimeValueFormatter.Format(segmentValue));
                        }

                        return string.Join(string.Empty, pieces);
                    }

                case NameExpr nameExpr:
                    if (_environment.TryGet(nameExpr.Name, out object nameValue))
                        return nameValue;

                    RuntimeError("Undefined name '" + nameExpr.Name + "'.", nameExpr);
                    return null;

                case BinaryExpr binaryExpr:
                    {
                        if (binaryExpr.Op.Type == TokenType.And)
                        {
                            bool leftTruth = IsTruthy(Evaluate(binaryExpr.Left));
                            if (!leftTruth)
                                return false;
                            return IsTruthy(Evaluate(binaryExpr.Right));
                        }

                        if (binaryExpr.Op.Type == TokenType.Or)
                        {
                            bool leftTruth = IsTruthy(Evaluate(binaryExpr.Left));
                            if (leftTruth)
                                return true;
                            return IsTruthy(Evaluate(binaryExpr.Right));
                        }

                        if (binaryExpr.Op.Type == TokenType.PipeGreater)
                            return EvaluatePipeline(binaryExpr);

                        object left = Evaluate(binaryExpr.Left);
                        object right = Evaluate(binaryExpr.Right);
                        return ApplyBinaryOperator(binaryExpr.Op, left, right, binaryExpr);
                    }

                case UnaryExpr unaryExpr:
                    {
                        object operand = Evaluate(unaryExpr.Operand);
                        return ApplyUnaryOperator(unaryExpr.Op, operand, unaryExpr);
                    }

                case TernaryExpr ternaryExpr:
                    return IsTruthy(Evaluate(ternaryExpr.Condition))
                        ? Evaluate(ternaryExpr.ThenExpr)
                        : Evaluate(ternaryExpr.ElseExpr);

                case CallExpr callExpr:
                    return EvaluateCall(callExpr);

                case AttributeExpr attributeExpr:
                    return ReadAttribute(attributeExpr);

                case SubscriptExpr subscriptExpr:
                    return ReadSubscript(subscriptExpr);

                case LambdaExpr lambdaExpr:
                    return RuntimeUserFunction.FromLambda(lambdaExpr, _environment);

                case ListExpr listExpr:
                    {
                        var values = new List<object>(listExpr.Elements.Count);
                        for (int i = 0; i < listExpr.Elements.Count; i++)
                            values.Add(Evaluate(listExpr.Elements[i]));

                        return values;
                    }

                case ListComprehensionExpr listComp:
                    return EvaluateListComprehension(listComp);

                case DictExpr dictExpr:
                    return EvaluateDictionary(dictExpr);

                case DictComprehensionExpr dictComp:
                    return EvaluateDictionaryComprehension(dictComp);

                case SetExpr setExpr:
                    {
                        var values = new List<object>();
                        for (int i = 0; i < setExpr.Elements.Count; i++)
                        {
                            object value = Evaluate(setExpr.Elements[i]);
                            if (!ContainsValue(values, value))
                                values.Add(value);
                        }

                        return new GrowlSet(values);
                    }

                case TupleExpr tupleExpr:
                    {
                        var values = new List<object>(tupleExpr.Elements.Count);
                        for (int i = 0; i < tupleExpr.Elements.Count; i++)
                            values.Add(Evaluate(tupleExpr.Elements[i]));

                        return new GrowlTuple(values);
                    }

                case RangeExpr rangeExpr:
                    {
                        long start = ExpectInteger(Evaluate(rangeExpr.Start), rangeExpr, "Range start must be an integer.");
                        long end = ExpectInteger(Evaluate(rangeExpr.End), rangeExpr, "Range end must be an integer.");
                        long step = 1;
                        if (rangeExpr.Step != null)
                            step = ExpectInteger(Evaluate(rangeExpr.Step), rangeExpr, "Range step must be an integer.");

                        if (step == 0)
                            RuntimeError("Range step cannot be 0.", rangeExpr);

                        return new GrowlRange(start, end, rangeExpr.Inclusive, step);
                    }

                case VectorExpr vectorExpr:
                    {
                        double x = ExpectNumber(Evaluate(vectorExpr.X), vectorExpr, "Vector component x must be numeric.");
                        double y = ExpectNumber(Evaluate(vectorExpr.Y), vectorExpr, "Vector component y must be numeric.");
                        double z = vectorExpr.Z == null
                            ? 0d
                            : ExpectNumber(Evaluate(vectorExpr.Z), vectorExpr, "Vector component z must be numeric.");

                        return new GrowlVector(x, y, z);
                    }

                case SpreadExpr spreadExpr:
                    return Evaluate(spreadExpr.Operand);

                case ProgramNode program:
                    ExecuteBlock(program.Statements, new RuntimeEnvironment(_environment));
                    return null;
            }

            RuntimeError("Unsupported expression node: " + node.GetType().Name + ".", node);
            return null;
        }

        private object EvaluateCall(CallExpr callExpr)
        {
            object calleeValue = Evaluate(callExpr.Callee);
            var args = EvaluateCallArguments(callExpr.Args, callExpr);
            return InvokeCallable(calleeValue, args, callExpr);
        }

        private object EvaluatePipeline(BinaryExpr pipeline)
        {
            object leftValue = Evaluate(pipeline.Left);

            if (pipeline.Right is CallExpr rightCall)
            {
                object calleeValue = Evaluate(rightCall.Callee);
                List<RuntimeArgument> args = EvaluateCallArguments(rightCall.Args, rightCall);
                args.Insert(0, new RuntimeArgument(name: null, value: leftValue));
                return InvokeCallable(calleeValue, args, rightCall);
            }

            object rightValue = Evaluate(pipeline.Right);
            var pipelineArgs = new List<RuntimeArgument>
            {
                new RuntimeArgument(name: null, value: leftValue),
            };
            return InvokeCallable(rightValue, pipelineArgs, pipeline);
        }

        private List<RuntimeArgument> EvaluateCallArguments(List<Argument> args, GrowlNode site)
        {
            var evaluated = new List<RuntimeArgument>();
            if (args == null)
                return evaluated;

            for (int i = 0; i < args.Count; i++)
            {
                Argument arg = args[i];
                if (arg.Value is SpreadExpr spreadExpr)
                {
                    object spreadValue = Evaluate(spreadExpr.Operand);
                    foreach (object item in AsEnumerable(spreadValue, site))
                        evaluated.Add(new RuntimeArgument(name: null, value: item));

                    continue;
                }

                evaluated.Add(new RuntimeArgument(arg.Name, Evaluate(arg.Value)));
            }

            return evaluated;
        }

        private object InvokeCallable(object calleeValue, List<RuntimeArgument> args, GrowlNode callSite)
        {
            if (calleeValue is IRuntimeCallable callable)
                return callable.Invoke(this, args, callSite);

            RuntimeError("Value of type '" + GetTypeName(calleeValue) + "' is not callable.", callSite);
            return null;
        }

        private object ReadAttribute(AttributeExpr attributeExpr)
        {
            object owner = Evaluate(attributeExpr.Object);
            string field = attributeExpr.FieldName;

            if (owner is IDictionary dictionary)
            {
                if (TryGetDictionaryValue(dictionary, field, out object value))
                    return value;

                RuntimeError("Dictionary has no key '" + field + "'.", attributeExpr);
            }

            if (owner is GrowlVector vector)
            {
                switch (field)
                {
                    case "x": return vector.X;
                    case "y": return vector.Y;
                    case "z": return vector.Z;
                    case "magnitude": return vector.Magnitude;
                }
            }

            if (owner is string s)
            {
                if (field == "length" || field == "len")
                    return (long)s.Length;
            }

            if (owner is IList list)
            {
                if (field == "length" || field == "count")
                    return (long)list.Count;
            }

            if (owner is GrowlTuple tuple)
            {
                if (field == "length" || field == "count")
                    return (long)tuple.Elements.Count;
            }

            if (owner is GrowlSet set)
            {
                if (field == "length" || field == "count")
                    return (long)set.Elements.Count;
            }

            if (owner is IRuntimeCallable callable)
            {
                if (field == "name")
                    return callable.Name;
            }

            RuntimeError("Type '" + GetTypeName(owner) + "' has no attribute '" + field + "'.", attributeExpr);
            return null;
        }

        private object ReadSubscript(SubscriptExpr subscriptExpr)
        {
            object owner = Evaluate(subscriptExpr.Object);
            object key = Evaluate(subscriptExpr.Key);

            if (owner is string s)
            {
                long index = ExpectInteger(key, subscriptExpr, "String index must be an integer.");
                int normalized = NormalizeIndex(s.Length, index, subscriptExpr);
                return s[normalized].ToString();
            }

            if (owner is IList list)
            {
                long index = ExpectInteger(key, subscriptExpr, "List index must be an integer.");
                int normalized = NormalizeIndex(list.Count, index, subscriptExpr);
                return list[normalized];
            }

            if (owner is GrowlTuple tuple)
            {
                long index = ExpectInteger(key, subscriptExpr, "Tuple index must be an integer.");
                int normalized = NormalizeIndex(tuple.Elements.Count, index, subscriptExpr);
                return tuple.Elements[normalized];
            }

            if (owner is IDictionary dictionary)
            {
                if (TryGetDictionaryValue(dictionary, key, out object value))
                    return value;

                RuntimeError("Dictionary key not found.", subscriptExpr);
            }

            RuntimeError("Cannot subscript value of type '" + GetTypeName(owner) + "'.", subscriptExpr);
            return null;
        }

        private object EvaluateListComprehension(ListComprehensionExpr listComp)
        {
            var results = new List<object>();
            var compRoot = new RuntimeEnvironment(_environment);
            EvaluateListComprehensionClause(listComp, clauseIndex: 0, compRoot, results);
            return results;
        }

        private void EvaluateListComprehensionClause(
            ListComprehensionExpr listComp,
            int clauseIndex,
            RuntimeEnvironment env,
            List<object> results)
        {
            if (clauseIndex >= listComp.Clauses.Count)
            {
                results.Add(EvaluateInEnvironment(listComp.Element, env));
                return;
            }

            ComprehensionClause clause = listComp.Clauses[clauseIndex];
            object iterable = EvaluateInEnvironment(clause.Iterable, env);
            IEnumerable<object> items = AsEnumerable(iterable, listComp);

            foreach (object item in items)
            {
                EnsureLoopBudget(listComp);
                var iterEnv = new RuntimeEnvironment(env);
                BindTargets(iterEnv, clause.Targets, item, listComp);

                if (clause.Filter != null && !IsTruthy(EvaluateInEnvironment(clause.Filter, iterEnv)))
                    continue;

                EvaluateListComprehensionClause(listComp, clauseIndex + 1, iterEnv, results);
            }
        }

        private object EvaluateDictionary(DictExpr dictExpr)
        {
            var dict = new Dictionary<object, object>();
            for (int i = 0; i < dictExpr.Entries.Count; i++)
            {
                DictEntry entry = dictExpr.Entries[i];
                if (entry.Key is SpreadExpr spread)
                {
                    object spreadValue = Evaluate(spread.Operand);
                    IDictionary spreadDictionary = spreadValue as IDictionary;
                    if (spreadDictionary == null)
                        RuntimeError("Dictionary spread expects dictionary value.", spread);

                    foreach (DictionaryEntry spreadEntry in spreadDictionary)
                        SetDictionaryValue(dict, spreadEntry.Key, spreadEntry.Value);

                    continue;
                }

                object key = Evaluate(entry.Key);
                object value = Evaluate(entry.Value);
                SetDictionaryValue(dict, key, value);
            }

            return dict;
        }

        private object EvaluateDictionaryComprehension(DictComprehensionExpr dictComp)
        {
            var dict = new Dictionary<object, object>();
            var compRoot = new RuntimeEnvironment(_environment);
            EvaluateDictionaryComprehensionClause(dictComp, clauseIndex: 0, compRoot, dict);
            return dict;
        }

        private void EvaluateDictionaryComprehensionClause(
            DictComprehensionExpr dictComp,
            int clauseIndex,
            RuntimeEnvironment env,
            Dictionary<object, object> output)
        {
            if (clauseIndex >= dictComp.Clauses.Count)
            {
                object key = EvaluateInEnvironment(dictComp.Key, env);
                object value = EvaluateInEnvironment(dictComp.Value, env);
                SetDictionaryValue(output, key, value);
                return;
            }

            ComprehensionClause clause = dictComp.Clauses[clauseIndex];
            object iterable = EvaluateInEnvironment(clause.Iterable, env);
            IEnumerable<object> items = AsEnumerable(iterable, dictComp);

            foreach (object item in items)
            {
                EnsureLoopBudget(dictComp);
                var iterEnv = new RuntimeEnvironment(env);
                BindTargets(iterEnv, clause.Targets, item, dictComp);

                if (clause.Filter != null && !IsTruthy(EvaluateInEnvironment(clause.Filter, iterEnv)))
                    continue;

                EvaluateDictionaryComprehensionClause(dictComp, clauseIndex + 1, iterEnv, output);
            }
        }

        private bool TryMatchPattern(GrowlNode pattern, object subject, RuntimeEnvironment bindings)
        {
            if (pattern == null)
                return false;

            switch (pattern)
            {
                case NameExpr namePattern:
                    if (namePattern.Name == "_")
                        return true;

                    bindings.Define(namePattern.Name, subject);
                    return true;

                case IntegerLiteralExpr integerLiteral:
                    return RuntimeEquals(subject, integerLiteral.Value);

                case FloatLiteralExpr floatLiteral:
                    return RuntimeEquals(subject, floatLiteral.Value);

                case StringLiteralExpr stringLiteral:
                    return RuntimeEquals(subject, stringLiteral.Value);

                case BoolLiteralExpr boolLiteral:
                    return RuntimeEquals(subject, boolLiteral.Value);

                case NoneLiteralExpr _:
                    return subject == null;

                case TupleExpr tuplePattern:
                    {
                        if (!TryDecomposeValue(subject, out List<object> tupleValues))
                            return false;
                        if (tupleValues.Count != tuplePattern.Elements.Count)
                            return false;

                        for (int i = 0; i < tuplePattern.Elements.Count; i++)
                        {
                            if (!TryMatchPattern(tuplePattern.Elements[i], tupleValues[i], bindings))
                                return false;
                        }

                        return true;
                    }

                case ListExpr listPattern:
                    {
                        if (!TryDecomposeValue(subject, out List<object> listValues))
                            return false;
                        if (listValues.Count != listPattern.Elements.Count)
                            return false;

                        for (int i = 0; i < listPattern.Elements.Count; i++)
                        {
                            if (!TryMatchPattern(listPattern.Elements[i], listValues[i], bindings))
                                return false;
                        }

                        return true;
                    }

                case DictExpr dictPattern:
                    {
                        if (!(subject is IDictionary subjectDictionary))
                            return false;

                        for (int i = 0; i < dictPattern.Entries.Count; i++)
                        {
                            DictEntry entry = dictPattern.Entries[i];
                            if (entry.Key is SpreadExpr)
                                continue;

                            object key = EvaluateInEnvironment(entry.Key, bindings);
                            if (!TryGetDictionaryValue(subjectDictionary, key, out object value))
                                return false;

                            if (!TryMatchPattern(entry.Value, value, bindings))
                                return false;
                        }

                        return true;
                    }

                case BinaryExpr binary when binary.Op.Type == TokenType.Pipe:
                    {
                        Dictionary<string, object> snapshot = bindings.SnapshotLocalValues();
                        if (TryMatchPattern(binary.Left, subject, bindings))
                            return true;

                        bindings.RestoreLocalValues(snapshot);
                        if (TryMatchPattern(binary.Right, subject, bindings))
                            return true;

                        bindings.RestoreLocalValues(snapshot);
                        return false;
                    }

                case BinaryExpr binary when binary.Op.Type == TokenType.Is:
                    {
                        string typeName = null;
                        if (binary.Right is NameExpr typeNameExpr)
                            typeName = typeNameExpr.Name;
                        else
                            typeName = EvaluateInEnvironment(binary.Right, bindings)?.ToString();

                        if (!IsValueOfTypeName(subject, typeName))
                            return false;

                        return TryMatchPattern(binary.Left, subject, bindings);
                    }
            }

            object patternValue = EvaluateInEnvironment(pattern, bindings);
            return RuntimeEquals(subject, patternValue);
        }

        private bool IsValueOfTypeName(object value, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            string runtimeType = GetTypeName(value);
            return string.Equals(runtimeType, typeName, StringComparison.OrdinalIgnoreCase);
        }

        private object ApplyUnaryOperator(Token op, object operand, GrowlNode site)
        {
            switch (op.Type)
            {
                case TokenType.Not:
                    return !IsTruthy(operand);

                case TokenType.Plus:
                    ExpectNumber(operand, site, "Unary '+' expects a numeric operand.");
                    return operand;

                case TokenType.Minus:
                    {
                        if (TryGetLong(operand, out long intValue))
                            return -intValue;
                        if (TryGetDouble(operand, out double floatValue))
                            return -floatValue;

                        RuntimeError("Unary '-' expects a numeric operand.", site);
                        return null;
                    }

                case TokenType.Tilde:
                    return ~ExpectInteger(operand, site, "Unary '~' expects an integer operand.");

                case TokenType.Caret:
                    {
                        if (operand is GrowlVector vector)
                        {
                            double mag = vector.Magnitude;
                            if (mag == 0)
                                return vector;

                            return new GrowlVector(vector.X / mag, vector.Y / mag, vector.Z / mag);
                        }

                        ExpectNumber(operand, site, "Unary '^' expects a vector or numeric operand.");
                        return operand;
                    }

                case TokenType.Pipe:
                    {
                        if (operand is GrowlVector vector)
                            return vector.Magnitude;
                        if (TryGetLong(operand, out long intValue))
                            return Math.Abs(intValue);
                        if (TryGetDouble(operand, out double floatValue))
                            return Math.Abs(floatValue);
                        if (operand is string s)
                            return (long)s.Length;
                        if (operand is IList list)
                            return (long)list.Count;
                        if (operand is GrowlTuple tuple)
                            return (long)tuple.Elements.Count;
                        if (operand is GrowlSet set)
                            return (long)set.Elements.Count;
                        if (operand is IDictionary dict)
                            return (long)dict.Count;

                        RuntimeError("Magnitude syntax '|expr|' does not support type '" + GetTypeName(operand) + "'.", site);
                        return null;
                    }

                default:
                    RuntimeError("Unsupported unary operator '" + op.Type + "'.", site);
                    return null;
            }
        }

        private object ApplyBinaryOperator(Token op, object left, object right, GrowlNode site)
        {
            switch (op.Type)
            {
                case TokenType.Plus:
                    {
                        if (TryGetLong(left, out long li) && TryGetLong(right, out long ri))
                            return li + ri;
                        if (TryGetDouble(left, out double ld) && TryGetDouble(right, out double rd))
                            return ld + rd;
                        if (left is string || right is string)
                            return ToConcatString(left) + ToConcatString(right);
                        if (left is IList leftList && right is IList rightList)
                            return ConcatLists(leftList, rightList);
                        if (left is GrowlTuple leftTuple && right is GrowlTuple rightTuple)
                            return new GrowlTuple(ConcatReadOnly(leftTuple.Elements, rightTuple.Elements));

                        RuntimeError("Operator '+' does not support operands '" + GetTypeName(left) + "' and '" + GetTypeName(right) + "'.", site);
                        return null;
                    }

                case TokenType.Minus:
                    {
                        if (TryGetLong(left, out long li) && TryGetLong(right, out long ri))
                            return li - ri;
                        if (TryGetDouble(left, out double ld) && TryGetDouble(right, out double rd))
                            return ld - rd;

                        RuntimeError("Operator '-' expects numeric operands.", site);
                        return null;
                    }

                case TokenType.Star:
                    {
                        if (TryGetLong(left, out long li) && TryGetLong(right, out long ri))
                            return li * ri;
                        if (TryGetDouble(left, out double ld) && TryGetDouble(right, out double rd))
                            return ld * rd;

                        RuntimeError("Operator '*' expects numeric operands.", site);
                        return null;
                    }

                case TokenType.Slash:
                    {
                        double denominator = ExpectNumber(right, site, "Division expects numeric operands.");
                        if (Math.Abs(denominator) < double.Epsilon)
                            RuntimeError("Division by zero.", site);

                        double numerator = ExpectNumber(left, site, "Division expects numeric operands.");
                        return numerator / denominator;
                    }

                case TokenType.SlashSlash:
                    {
                        double denominator = ExpectNumber(right, site, "Floor division expects numeric operands.");
                        if (Math.Abs(denominator) < double.Epsilon)
                            RuntimeError("Division by zero.", site);

                        double numerator = ExpectNumber(left, site, "Floor division expects numeric operands.");
                        return (long)Math.Floor(numerator / denominator);
                    }

                case TokenType.Percent:
                    {
                        if (TryGetLong(left, out long li) && TryGetLong(right, out long ri))
                            return li % ri;
                        if (TryGetDouble(left, out double ld) && TryGetDouble(right, out double rd))
                            return ld % rd;

                        RuntimeError("Operator '%' expects numeric operands.", site);
                        return null;
                    }

                case TokenType.StarStar:
                    {
                        double exp = ExpectNumber(right, site, "Exponentiation expects numeric operands.");
                        double bas = ExpectNumber(left, site, "Exponentiation expects numeric operands.");
                        return Math.Pow(bas, exp);
                    }

                case TokenType.Ampersand:
                    {
                        if (left is GrowlSet leftSet && right is GrowlSet rightSet)
                            return IntersectSets(leftSet, rightSet);

                        return ExpectInteger(left, site, "Bitwise '&' expects integers.") &
                               ExpectInteger(right, site, "Bitwise '&' expects integers.");
                    }

                case TokenType.Pipe:
                    {
                        if (left is GrowlSet leftSet && right is GrowlSet rightSet)
                            return UnionSets(leftSet, rightSet);

                        return ExpectInteger(left, site, "Bitwise '|' expects integers.") |
                               ExpectInteger(right, site, "Bitwise '|' expects integers.");
                    }

                case TokenType.Caret:
                    {
                        if (left is GrowlSet leftSet && right is GrowlSet rightSet)
                            return SymmetricDiffSets(leftSet, rightSet);

                        return ExpectInteger(left, site, "Bitwise '^' expects integers.") ^
                               ExpectInteger(right, site, "Bitwise '^' expects integers.");
                    }

                case TokenType.LessLess:
                    return ExpectInteger(left, site, "Bit shift expects integers.") <<
                           (int)ExpectInteger(right, site, "Bit shift expects integers.");

                case TokenType.GreaterGreater:
                    return ExpectInteger(left, site, "Bit shift expects integers.") >>
                           (int)ExpectInteger(right, site, "Bit shift expects integers.");

                case TokenType.EqualEqual:
                    return RuntimeEquals(left, right);

                case TokenType.BangEqual:
                    return !RuntimeEquals(left, right);

                case TokenType.Less:
                    return CompareValues(left, right, site) < 0;

                case TokenType.LessEqual:
                    return CompareValues(left, right, site) <= 0;

                case TokenType.Greater:
                    return CompareValues(left, right, site) > 0;

                case TokenType.GreaterEqual:
                    return CompareValues(left, right, site) >= 0;

                case TokenType.QuestionQuestion:
                    return left ?? right;

                case TokenType.In:
                    {
                        bool contains = ContainsValue(right, left);
                        if (string.Equals(op.Value, "not in", StringComparison.Ordinal))
                            return !contains;
                        return contains;
                    }

                case TokenType.Is:
                    {
                        bool equal = RuntimeEquals(left, right);
                        if (string.Equals(op.Value, "is not", StringComparison.Ordinal))
                            return !equal;
                        return equal;
                    }

                default:
                    RuntimeError("Unsupported binary operator '" + op.Type + "'.", site);
                    return null;
            }
        }

        private bool ContainsValue(object container, object value)
        {
            if (container is string text)
            {
                string probe = value == null ? "none" : value.ToString();
                return text.Contains(probe);
            }

            if (container is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (RuntimeEquals(entry.Key, value))
                        return true;
                }

                return false;
            }

            if (container is GrowlTuple tuple)
                return ContainsValue(tuple.Elements, value);

            if (container is GrowlSet set)
                return ContainsValue(set.Elements, value);

            if (container is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (RuntimeEquals(item, value))
                        return true;
                }

                return false;
            }

            return false;
        }

        private static IList ConcatLists(IList left, IList right)
        {
            var merged = new List<object>(left.Count + right.Count);
            for (int i = 0; i < left.Count; i++)
                merged.Add(left[i]);
            for (int i = 0; i < right.Count; i++)
                merged.Add(right[i]);
            return merged;
        }

        private static IReadOnlyList<object> ConcatReadOnly(IReadOnlyList<object> left, IReadOnlyList<object> right)
        {
            var merged = new List<object>(left.Count + right.Count);
            for (int i = 0; i < left.Count; i++)
                merged.Add(left[i]);
            for (int i = 0; i < right.Count; i++)
                merged.Add(right[i]);
            return merged;
        }

        private GrowlSet UnionSets(GrowlSet left, GrowlSet right)
        {
            var values = new List<object>();
            for (int i = 0; i < left.Elements.Count; i++)
                if (!ContainsValue(values, left.Elements[i]))
                    values.Add(left.Elements[i]);
            for (int i = 0; i < right.Elements.Count; i++)
                if (!ContainsValue(values, right.Elements[i]))
                    values.Add(right.Elements[i]);
            return new GrowlSet(values);
        }

        private GrowlSet IntersectSets(GrowlSet left, GrowlSet right)
        {
            var values = new List<object>();
            for (int i = 0; i < left.Elements.Count; i++)
            {
                if (ContainsValue(right.Elements, left.Elements[i]) && !ContainsValue(values, left.Elements[i]))
                    values.Add(left.Elements[i]);
            }

            return new GrowlSet(values);
        }

        private GrowlSet SymmetricDiffSets(GrowlSet left, GrowlSet right)
        {
            var values = new List<object>();

            for (int i = 0; i < left.Elements.Count; i++)
            {
                object item = left.Elements[i];
                if (!ContainsValue(right.Elements, item) && !ContainsValue(values, item))
                    values.Add(item);
            }

            for (int i = 0; i < right.Elements.Count; i++)
            {
                object item = right.Elements[i];
                if (!ContainsValue(left.Elements, item) && !ContainsValue(values, item))
                    values.Add(item);
            }

            return new GrowlSet(values);
        }

        private int CompareValues(object left, object right, GrowlNode site)
        {
            if (TryGetDouble(left, out double ld) && TryGetDouble(right, out double rd))
                return ld.CompareTo(rd);

            if (left is string ls && right is string rs)
                return string.Compare(ls, rs, StringComparison.Ordinal);

            RuntimeError("Values of type '" + GetTypeName(left) + "' and '" + GetTypeName(right) + "' cannot be compared.", site);
            return 0;
        }

        private static string ToConcatString(object value)
        {
            if (value == null)
                return "none";
            if (value is string s)
                return s;
            return RuntimeValueFormatter.Format(value);
        }

        private static bool TryGetLong(object value, out long result)
        {
            switch (value)
            {
                case sbyte v: result = v; return true;
                case byte v: result = v; return true;
                case short v: result = v; return true;
                case ushort v: result = v; return true;
                case int v: result = v; return true;
                case uint v: result = v; return true;
                case long v: result = v; return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static bool TryGetDouble(object value, out double result)
        {
            switch (value)
            {
                case sbyte v: result = v; return true;
                case byte v: result = v; return true;
                case short v: result = v; return true;
                case ushort v: result = v; return true;
                case int v: result = v; return true;
                case uint v: result = v; return true;
                case long v: result = v; return true;
                case ulong v: result = v; return true;
                case float v: result = v; return true;
                case double v: result = v; return true;
                case decimal v: result = (double)v; return true;
                default:
                    result = 0d;
                    return false;
            }
        }

        private long ExpectInteger(object value, GrowlNode site, string message)
        {
            if (TryGetLong(value, out long asLong))
                return asLong;

            if (TryGetDouble(value, out double asDouble) && Math.Abs(asDouble % 1d) < double.Epsilon)
                return (long)asDouble;

            RuntimeError(message, site);
            return 0;
        }

        private double ExpectNumber(object value, GrowlNode site, string message)
        {
            if (TryGetDouble(value, out double asDouble))
                return asDouble;

            RuntimeError(message, site);
            return 0d;
        }

        private static int NormalizeIndex(int length, long index, GrowlNode site)
        {
            long normalized = index;
            if (normalized < 0)
                normalized = length + normalized;

            if (normalized < 0 || normalized >= length)
            {
                throw new RuntimeExecutionException(
                    "Index " + index + " is out of range.",
                    site.Line,
                    site.Column);
            }

            return (int)normalized;
        }

        private static bool IsTruthy(object value)
        {
            if (value == null)
                return false;
            if (value is bool b)
                return b;
            if (TryGetDouble(value, out double number))
                return Math.Abs(number) > double.Epsilon;
            if (value is string s)
                return s.Length > 0;
            if (value is IList list)
                return list.Count > 0;
            if (value is IDictionary dictionary)
                return dictionary.Count > 0;
            if (value is GrowlTuple tuple)
                return tuple.Elements.Count > 0;
            if (value is GrowlSet set)
                return set.Elements.Count > 0;

            return true;
        }

        private IEnumerable<object> AsEnumerable(object value, GrowlNode site)
        {
            if (value is GrowlRange range)
                return range.Enumerate();

            if (value is GrowlTuple tuple)
                return tuple.Elements;

            if (value is GrowlSet set)
                return set.Elements;

            if (value is string text)
            {
                var chars = new List<object>(text.Length);
                for (int i = 0; i < text.Length; i++)
                    chars.Add(text[i].ToString());
                return chars;
            }

            if (value is IDictionary dictionary)
            {
                var pairs = new List<object>(dictionary.Count);
                foreach (DictionaryEntry entry in dictionary)
                    pairs.Add(new GrowlTuple(new List<object> { entry.Key, entry.Value }));
                return pairs;
            }

            if (value is IEnumerable enumerable)
            {
                var items = new List<object>();
                foreach (object item in enumerable)
                    items.Add(item);
                return items;
            }

            RuntimeError("Value of type '" + GetTypeName(value) + "' is not iterable.", site);
            return new List<object>();
        }

        private void BindTargets(RuntimeEnvironment targetScope, List<string> targets, object value, GrowlNode site)
        {
            if (targets == null || targets.Count == 0)
                return;

            if (targets.Count == 1)
            {
                targetScope.Define(targets[0], value);
                return;
            }

            if (!TryDecomposeValue(value, out List<object> values) || values.Count != targets.Count)
            {
                RuntimeError(
                    "Expected iterable with exactly " + targets.Count + " values when unpacking for-targets.",
                    site);
            }

            for (int i = 0; i < targets.Count; i++)
                targetScope.Define(targets[i], values[i]);
        }

        private bool TryDecomposeValue(object value, out List<object> values)
        {
            if (value is GrowlTuple tuple)
            {
                values = new List<object>(tuple.Elements.Count);
                for (int i = 0; i < tuple.Elements.Count; i++)
                    values.Add(tuple.Elements[i]);
                return true;
            }

            if (value is IList list)
            {
                values = new List<object>(list.Count);
                for (int i = 0; i < list.Count; i++)
                    values.Add(list[i]);
                return true;
            }

            if (value is IEnumerable enumerable && !(value is string) && !(value is IDictionary))
            {
                values = new List<object>();
                foreach (object item in enumerable)
                    values.Add(item);
                return true;
            }

            values = null;
            return false;
        }

        private object EvaluateInEnvironment(GrowlNode node, RuntimeEnvironment env)
        {
            RuntimeEnvironment previous = _environment;
            _environment = env;
            try
            {
                return Evaluate(node);
            }
            finally
            {
                _environment = previous;
            }
        }

        private static bool TryGetDictionaryValue(IDictionary dictionary, object key, out object value)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (RuntimeEquals(entry.Key, key))
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static void SetDictionaryValue(IDictionary dictionary, object key, object value)
        {
            object existingKey = null;
            bool found = false;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (RuntimeEquals(entry.Key, key))
                {
                    existingKey = entry.Key;
                    found = true;
                    break;
                }
            }

            if (found)
                dictionary[existingKey] = value;
            else
                dictionary[key] = value;
        }

        private static bool RuntimeEquals(object left, object right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;

            if (TryGetDouble(left, out double ld) && TryGetDouble(right, out double rd))
                return Math.Abs(ld - rd) < double.Epsilon;

            if (left is string ls && right is string rs)
                return ls == rs;

            if (left is bool lb && right is bool rb)
                return lb == rb;

            if (left is GrowlTuple leftTuple && right is GrowlTuple rightTuple)
            {
                if (leftTuple.Elements.Count != rightTuple.Elements.Count)
                    return false;

                for (int i = 0; i < leftTuple.Elements.Count; i++)
                {
                    if (!RuntimeEquals(leftTuple.Elements[i], rightTuple.Elements[i]))
                        return false;
                }

                return true;
            }

            if (left is GrowlSet leftSet && right is GrowlSet rightSet)
            {
                if (leftSet.Elements.Count != rightSet.Elements.Count)
                    return false;

                for (int i = 0; i < leftSet.Elements.Count; i++)
                {
                    if (!ContainsValueInEnumerable(rightSet.Elements, leftSet.Elements[i]))
                        return false;
                }

                return true;
            }

            if (left is IList leftList && right is IList rightList)
            {
                if (leftList.Count != rightList.Count)
                    return false;

                for (int i = 0; i < leftList.Count; i++)
                {
                    if (!RuntimeEquals(leftList[i], rightList[i]))
                        return false;
                }

                return true;
            }

            if (left is IDictionary leftDict && right is IDictionary rightDict)
            {
                if (leftDict.Count != rightDict.Count)
                    return false;

                foreach (DictionaryEntry entry in leftDict)
                {
                    if (!TryGetDictionaryValue(rightDict, entry.Key, out object rightValue))
                        return false;
                    if (!RuntimeEquals(entry.Value, rightValue))
                        return false;
                }

                return true;
            }

            return left.Equals(right);
        }

        private static bool ContainsValueInEnumerable(IEnumerable values, object target)
        {
            if (values == null)
                return false;

            foreach (object value in values)
            {
                if (RuntimeEquals(value, target))
                    return true;
            }

            return false;
        }

        private static string GetTypeName(object value)
        {
            if (value == null)
                return "none";
            if (value is bool)
                return "bool";
            if (value is string)
                return "string";
            if (value is sbyte || value is byte || value is short || value is ushort || value is int ||
                value is uint || value is long || value is ulong)
                return "int";
            if (value is float || value is double || value is decimal)
                return "float";
            if (value is GrowlTuple)
                return "tuple";
            if (value is GrowlSet)
                return "set";
            if (value is GrowlRange)
                return "range";
            if (value is GrowlVector)
                return "vector";
            if (value is IDictionary dict && TryGetDictionaryValue(dict, "__type", out object typeName) && typeName is string name)
                return name;
            if (value is IDictionary)
                return "dict";
            if (value is IList)
                return "list";
            if (value is IRuntimeCallable)
                return "fn";

            return value.GetType().Name;
        }

        private void EnsureLoopBudget(GrowlNode site)
        {
            if (_options.MaxLoopIterations <= 0)
                return;

            _loopIterations++;
            if (_loopIterations > _options.MaxLoopIterations)
            {
                RuntimeError(
                    "Loop iteration limit exceeded (" + _options.MaxLoopIterations + ").",
                    site);
            }
        }

        private void RuntimeError(string message, GrowlNode node)
        {
            int line = node != null ? node.Line : 1;
            int column = node != null ? node.Column : 1;
            throw new RuntimeExecutionException(message, line, column);
        }
    }
}
