using System.Collections.Generic;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Lexer;

namespace GrowlLanguage.Parser
{
    // ─────────────────────────────────────────────────────────────────────────
    // Growl Parser (phase 1)
    //
    // Current scope:
    // - Expressions with precedence (including calls, attributes, subscripts)
    // - Assignment / expression statements
    // - if/elif/else, for, while, loop
    // - fn declarations + decorators
    // - @role/@gene + fn => GeneDecl
    // - module/import forms
    //
    // The parser is intentionally recovery-friendly. Syntax errors produce
    // diagnostics and the parser attempts to continue.
    // ─────────────────────────────────────────────────────────────────────────
    public sealed class Parser
    {
        private readonly List<Token> _tokens;
        private readonly List<ParseError> _errors = new List<ParseError>();
        private int _current;

        private sealed class ParseAbortException : System.Exception { }

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        public static ParseResult Parse(string source)
        {
            var lexResult = GrowlLanguage.Lexer.Lexer.Lex(source);
            return Parse(lexResult);
        }

        public static ParseResult Parse(LexResult lexResult)
        {
            var parser = new Parser(lexResult?.Tokens);
            var parseResult = parser.Run();

            if (lexResult == null || lexResult.Errors.Count == 0)
                return parseResult;

            var merged = new List<ParseError>(lexResult.Errors.Count + parseResult.Errors.Count);
            foreach (var lexError in lexResult.Errors)
                merged.Add(new ParseError("Lexer: " + lexError.Message, lexError.Line, lexError.Column));
            merged.AddRange(parseResult.Errors);

            return new ParseResult(parseResult.Program, merged);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Construction / run
        // ─────────────────────────────────────────────────────────────────────

        private Parser(List<Token> tokens)
        {
            _tokens = tokens ?? new List<Token>();
            if (_tokens.Count == 0 || _tokens[_tokens.Count - 1].Type != TokenType.Eof)
                _tokens.Add(new Token(TokenType.Eof, string.Empty, 1, 1));
        }

        private ParseResult Run()
        {
            ProgramNode program = ParseProgram();
            return new ParseResult(program, _errors);
        }

        private ProgramNode ParseProgram()
        {
            var statements = new List<GrowlNode>();

            while (!IsAtEnd())
            {
                ConsumeTopLevelIgnorable();
                if (IsAtEnd()) break;

                int start = _current;
                GrowlNode stmt = ParseStatement();
                if (stmt != null)
                    statements.Add(stmt);

                ConsumeTopLevelIgnorable();

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress; skipping token.");
                    Advance();
                }
            }

            return new ProgramNode(statements);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Statements
        // ─────────────────────────────────────────────────────────────────────

        private GrowlNode ParseStatement()
        {
            try
            {
                if (Check(TokenType.At)) return ParseDecoratedDeclaration();

                if (Check(TokenType.Abstract) && CheckNext(TokenType.Class))
                {
                    Advance(); // abstract
                    Token classToken = Advance(); // class
                    return ParseClassDeclCore(new List<Decorator>(), isAbstract: true, classToken);
                }

                if (Match(TokenType.Fn))       return ParseFnDeclCore(new List<Decorator>(), Previous());
                if (Match(TokenType.Class))    return ParseClassDeclCore(new List<Decorator>(), isAbstract: false, Previous());
                if (Match(TokenType.Struct))   return ParseStructDecl(Previous());
                if (Match(TokenType.Enum))     return ParseEnumDecl(Previous());
                if (Match(TokenType.Trait))    return ParseTraitDecl(Previous());
                if (Match(TokenType.Mixin))    return ParseMixinDecl(Previous());
                if (Match(TokenType.Const))    return ParseConstDecl(Previous());
                if (Match(TokenType.Type))     return ParseTypeAliasDecl(Previous());
                if (Match(TokenType.Phase))    return ParsePhaseBlock(Previous());
                if (Match(TokenType.When))     return ParseWhenBlock(Previous());
                if (Match(TokenType.Respond))  return ParseRespondBlock(Previous());
                if (Match(TokenType.Adapt))    return ParseAdaptBlock(Previous());
                if (Match(TokenType.Cycle))    return ParseCycleBlock(Previous());
                if (Match(TokenType.Ticker))   return ParseTickerDecl(Previous());
                if (Match(TokenType.Match))    return ParseMatchStmt(Previous());
                if (Match(TokenType.Try))      return ParseTryStmt(Previous());
                if (Match(TokenType.If))       return ParseIfStmt(Previous());
                if (Match(TokenType.For))      return ParseForStmt(Previous());
                if (Match(TokenType.While))    return ParseWhileStmt(Previous());
                if (Match(TokenType.Loop))     return ParseLoopStmt(Previous());
                if (Match(TokenType.Return))   return ParseReturnStmt(Previous());
                if (Match(TokenType.Break))    return new BreakStmt(Previous());
                if (Match(TokenType.Continue)) return new ContinueStmt(Previous());
                if (Match(TokenType.Yield))    return ParseYieldStmt(Previous());
                if (Match(TokenType.Wait))     return ParseWaitStmt(Previous());
                if (Match(TokenType.Defer))    return ParseDeferStmt(Previous());
                if (Match(TokenType.Mutate))   return ParseMutateStmt(Previous());
                if (Match(TokenType.Module))   return ParseModuleDecl(Previous());
                if (Match(TokenType.Import))   return ParseImportStmt(Previous());
                if (Match(TokenType.From))     return ParseFromImportStmt(Previous());

                return ParseAssignmentOrExpressionStmt();
            }
            catch (ParseAbortException)
            {
                SynchronizeToNextStatement();
                return null;
            }
        }

        private GrowlNode ParseDecoratedDeclaration()
        {
            List<Decorator> decorators = ParseDecorators();
            Decorator geneDecorator = decorators.FirstOrDefault(
                d => d.Name == "role" || d.Name == "gene");

            if (geneDecorator != null)
            {
                Token fnToken = Consume(TokenType.Fn, "Expected 'fn' after @role/@gene decorator.");
                List<Decorator> fnDecorators = decorators
                    .Where(d => !ReferenceEquals(d, geneDecorator))
                    .ToList();

                FnDecl fn = ParseFnDeclCore(fnDecorators, fnToken);
                string roleName = ExtractGeneRoleName(geneDecorator, fnToken);
                bool isRole = geneDecorator.Name == "role";
                return new GeneDecl(roleName, isRole, fn, fnToken.Line, fnToken.Column);
            }

            if (Check(TokenType.Abstract) && CheckNext(TokenType.Class))
            {
                Advance(); // abstract
                Token classToken = Advance(); // class
                return ParseClassDeclCore(decorators, isAbstract: true, classToken);
            }

            if (Match(TokenType.Class))
                return ParseClassDeclCore(decorators, isAbstract: false, Previous());

            if (Match(TokenType.Fn))
                return ParseFnDeclCore(decorators, Previous());

            AddError(Current(), "Expected 'fn' or 'class' after decorator list.");
            throw new ParseAbortException();
        }

        private List<Decorator> ParseDecorators()
        {
            var decorators = new List<Decorator>();

            while (Match(TokenType.At))
            {
                Token name = ConsumeIdentifier("Expected decorator name after '@'.");
                var args = new List<GrowlNode>();

                if (Match(TokenType.LeftParen))
                {
                    if (!Check(TokenType.RightParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        }
                        while (Match(TokenType.Comma));
                    }

                    Consume(TokenType.RightParen, "Expected ')' after decorator arguments.");
                }

                Consume(TokenType.Newline, "Expected newline after decorator.");
                decorators.Add(new Decorator(name, args));
            }

            return decorators;
        }

        private string ExtractGeneRoleName(Decorator decorator, Token fallback)
        {
            if (decorator.Args.Count == 1)
            {
                var s = decorator.Args[0] as StringLiteralExpr;
                if (s != null)
                    return s.Value;
            }

            AddError(fallback, "Gene/role decorator expects exactly one string argument.");
            return decorator.Name;
        }

        private FnDecl ParseFnDeclCore(List<Decorator> decorators, Token fnToken)
        {
            Token name = ConsumeIdentifier("Expected function name after 'fn'.");
            Consume(TokenType.LeftParen, "Expected '(' after function name.");

            var parameters = new List<Param>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    parameters.Add(ParseParam());
                }
                while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after function parameters.");

            TypeRef returnType = null;
            if (Match(TokenType.Arrow))
                returnType = ParseTypeRef();

            Consume(TokenType.Colon, "Expected ':' after function signature.");
            List<GrowlNode> body = ParseIndentedBlock("function");

            return new FnDecl(name, parameters, returnType, body, decorators, fnToken.Line, fnToken.Column);
        }

        private Param ParseParam()
        {
            bool isVariadic = false;
            bool isKeyword = false;

            if (Match(TokenType.StarStar))
            {
                isVariadic = true;
                isKeyword = true;
            }
            else if (Match(TokenType.Star))
            {
                isVariadic = true;
            }

            Token name = ConsumeParameterName("Expected parameter name.");
            TypeRef typeAnnotation = null;
            GrowlNode defaultValue = null;

            if (Match(TokenType.Colon))
                typeAnnotation = ParseTypeRef();

            if (Match(TokenType.Equal))
                defaultValue = ParseExpression();

            return new Param(name, typeAnnotation, defaultValue, isVariadic, isKeyword);
        }

        private TypeRef ParseTypeRef()
        {
            if (Match(TokenType.Fn))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'fn' in function type.");

                var argTypes = new List<TypeRef>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        argTypes.Add(ParseTypeRef());
                    }
                    while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expected ')' in function type.");
                Consume(TokenType.Arrow, "Expected '->' in function type.");
                TypeRef ret = ParseTypeRef();
                argTypes.Add(ret);
                return new TypeRef("fn", argTypes);
            }

            Token name = ConsumeIdentifier("Expected type name.");
            string fullName = name.Value;

            while (Match(TokenType.Dot))
            {
                Token part = ConsumeIdentifier("Expected identifier after '.'.");
                fullName += "." + part.Value;
            }

            List<TypeRef> args = null;
            if (Match(TokenType.LeftBracket))
            {
                args = new List<TypeRef>();
                if (!Check(TokenType.RightBracket))
                {
                    do
                    {
                        args.Add(ParseTypeRef());
                    }
                    while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightBracket, "Expected ']' after generic type arguments.");
            }

            return new TypeRef(fullName, args);
        }

        private List<GrowlNode> ParseIndentedBlock(string blockName)
        {
            Consume(TokenType.Newline, $"Expected newline before {blockName} block.");
            Consume(TokenType.Indent, $"Expected indented {blockName} block.");

            var body = new List<GrowlNode>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                int start = _current;
                GrowlNode stmt = ParseStatement();
                if (stmt != null)
                    body.Add(stmt);

                ConsumeStatementTerminators();

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress inside block; skipping token.");
                    Advance();
                }
            }

            Consume(TokenType.Dedent, $"Expected dedent to close {blockName} block.");
            return body;
        }

        private GrowlNode ParseAssignmentOrExpressionStmt()
        {
            GrowlNode expr = ParseExpression();

            if (MatchAny(TokenType.Equal, TokenType.PlusEqual, TokenType.MinusEqual,
                         TokenType.StarEqual, TokenType.SlashEqual, TokenType.SlashSlashEqual,
                         TokenType.PercentEqual, TokenType.StarStarEqual))
            {
                Token op = Previous();
                GrowlNode value = ParseExpression();

                if (IsAssignableTarget(expr))
                    return new AssignStmt(expr, op, value);

                AddError(op, "Left side of assignment must be a name, attribute, or subscript.");
            }

            return new ExprStmt(expr);
        }

        private bool IsAssignableTarget(GrowlNode expr)
            => expr is NameExpr || expr is AttributeExpr || expr is SubscriptExpr;

        private IfStmt ParseIfStmt(Token ifToken)
        {
            GrowlNode condition = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after if condition.");
            List<GrowlNode> thenBody = ParseIndentedBlock("if");

            var elifClauses = new List<ElifClause>();
            while (Match(TokenType.Elif))
            {
                GrowlNode elifCond = ParseExpression();
                Consume(TokenType.Colon, "Expected ':' after elif condition.");
                List<GrowlNode> elifBody = ParseIndentedBlock("elif");
                elifClauses.Add(new ElifClause(elifCond, elifBody));
            }

            List<GrowlNode> elseBody = null;
            if (Match(TokenType.Else))
            {
                Consume(TokenType.Colon, "Expected ':' after else.");
                elseBody = ParseIndentedBlock("else");
            }

            return new IfStmt(condition, thenBody, elifClauses, elseBody, ifToken.Line, ifToken.Column);
        }

        private ForStmt ParseForStmt(Token forToken)
        {
            List<string> targets = ParseTargetList();
            Consume(TokenType.In, "Expected 'in' in for statement.");
            GrowlNode iterable = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after for header.");
            List<GrowlNode> body = ParseIndentedBlock("for");

            List<GrowlNode> elseBody = null;
            if (Match(TokenType.Else))
            {
                Consume(TokenType.Colon, "Expected ':' after for-else.");
                elseBody = ParseIndentedBlock("for-else");
            }

            return new ForStmt(targets, iterable, body, elseBody, forToken.Line, forToken.Column);
        }

        private List<string> ParseTargetList()
        {
            var targets = new List<string>();
            targets.Add(ConsumeIdentifier("Expected loop target identifier.").Value);

            while (Match(TokenType.Comma))
                targets.Add(ConsumeIdentifier("Expected identifier after ','.").Value);

            return targets;
        }

        private WhileStmt ParseWhileStmt(Token whileToken)
        {
            GrowlNode condition = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after while condition.");
            List<GrowlNode> body = ParseIndentedBlock("while");
            return new WhileStmt(condition, body, whileToken.Line, whileToken.Column);
        }

        private LoopStmt ParseLoopStmt(Token loopToken)
        {
            Consume(TokenType.Colon, "Expected ':' after loop.");
            List<GrowlNode> body = ParseIndentedBlock("loop");
            return new LoopStmt(body, loopToken.Line, loopToken.Column);
        }

        private ReturnStmt ParseReturnStmt(Token keyword)
        {
            if (Check(TokenType.Newline) || Check(TokenType.Dedent) || Check(TokenType.Eof))
                return new ReturnStmt(keyword, null);

            GrowlNode value = ParseExpression();
            return new ReturnStmt(keyword, value);
        }

        private YieldStmt ParseYieldStmt(Token keyword)
        {
            GrowlNode value = null;
            if (!Check(TokenType.Newline) && !Check(TokenType.Dedent) && !Check(TokenType.Eof))
                value = ParseExpression();

            return new YieldStmt(keyword, value);
        }

        private WaitStmt ParseWaitStmt(Token keyword)
        {
            GrowlNode ticks;
            if (Check(TokenType.Newline) || Check(TokenType.Dedent) || Check(TokenType.Eof))
            {
                AddError(keyword, "Expected wait duration expression.");
                ticks = MakeNoneLiteral(keyword.Line, keyword.Column);
            }
            else
            {
                ticks = ParseExpression();
            }

            return new WaitStmt(keyword, ticks);
        }

        private DeferStmt ParseDeferStmt(Token deferToken)
        {
            bool isUntil = false;
            GrowlNode duration;

            if (Match(TokenType.Until))
            {
                isUntil = true;
                duration = ParseExpression();
            }
            else
            {
                duration = ParseExpression();
                if (CheckSoftKeyword("ticks"))
                    Advance();
            }

            Consume(TokenType.Colon, "Expected ':' after defer header.");
            List<GrowlNode> body = ParseIndentedBlock("defer");
            return new DeferStmt(duration, isUntil, body, deferToken.Line, deferToken.Column);
        }

        private MutateStmt ParseMutateStmt(Token mutateToken)
        {
            GrowlNode target = ParseExpression();
            if (!IsAssignableTarget(target))
                AddError(mutateToken, "Mutate target must be a name, attribute, or subscript.");

            Consume(TokenType.By, "Expected 'by' in mutate statement.");
            GrowlNode value = ParseExpression();

            GrowlNode interval = null;
            if (Match(TokenType.Every))
            {
                interval = ParseExpression();
                if (CheckSoftKeyword("ticks"))
                    Advance();
            }

            return new MutateStmt(target, value, interval, mutateToken.Line, mutateToken.Column);
        }

        private MatchStmt ParseMatchStmt(Token matchToken)
        {
            GrowlNode subject = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after match subject.");
            Consume(TokenType.Newline, "Expected newline before match block.");
            Consume(TokenType.Indent, "Expected indented match block.");

            var cases = new List<CaseClause>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                try
                {
                    Consume(TokenType.Case, "Expected 'case' in match block.");
                    GrowlNode pattern = ParseCasePatternExpression();

                    GrowlNode guard = null;
                    if (Match(TokenType.If))
                        guard = ParseExpression();

                    Consume(TokenType.Colon, "Expected ':' after case pattern.");
                    List<GrowlNode> body = ParseIndentedBlock("case");
                    cases.Add(new CaseClause(pattern, guard, body));
                }
                catch (ParseAbortException)
                {
                    SynchronizeWithinMatch();
                }

                ConsumeStatementTerminators();
            }

            Consume(TokenType.Dedent, "Expected dedent to close match block.");
            if (cases.Count == 0)
                AddError(matchToken, "Match statement requires at least one case.");

            return new MatchStmt(subject, cases, matchToken.Line, matchToken.Column);
        }

        private GrowlNode ParseCasePatternExpression()
        {
            // Case patterns intentionally do not include ternary "if ... else ..."
            // because "if" starts an optional case guard.
            return ParseOrExpression();
        }

        private TryStmt ParseTryStmt(Token tryToken)
        {
            Consume(TokenType.Colon, "Expected ':' after 'try'.");
            List<GrowlNode> tryBody = ParseIndentedBlock("try");

            if (!Match(TokenType.Recover))
            {
                AddError(Current(), "Expected 'recover' after try block.");
                SkipDanglingAlwaysBlock();

                var syntheticErrorName = new Token(TokenType.Identifier, "error",
                                                   tryToken.Line, tryToken.Column);
                return new TryStmt(tryBody, syntheticErrorName,
                                   new List<GrowlNode>(), null,
                                   tryToken.Line, tryToken.Column);
            }

            Token errorName = ConsumeIdentifier("Expected error binding after 'recover'.");
            Consume(TokenType.Colon, "Expected ':' after recover binding.");
            List<GrowlNode> recoverBody = ParseIndentedBlock("recover");

            List<GrowlNode> alwaysBody = null;
            if (Match(TokenType.Always))
            {
                Consume(TokenType.Colon, "Expected ':' after 'always'.");
                alwaysBody = ParseIndentedBlock("always");
            }

            return new TryStmt(tryBody, errorName, recoverBody, alwaysBody, tryToken.Line, tryToken.Column);
        }

        private void SynchronizeWithinMatch()
        {
            while (!IsAtEnd())
            {
                if (Check(TokenType.Case))
                    return;

                if (Check(TokenType.Dedent))
                {
                    if (CheckNext(TokenType.Case))
                    {
                        Advance(); // consume the inner block dedent and continue
                        continue;
                    }

                    return;
                }

                Advance();
            }
        }

        private void SkipDanglingAlwaysBlock()
        {
            if (!Match(TokenType.Always))
                return;

            if (Check(TokenType.Colon))
                Advance();

            if (Check(TokenType.Newline))
                Advance();

            if (!Match(TokenType.Indent))
                return;

            int depth = 1;
            while (!IsAtEnd() && depth > 0)
            {
                if (Match(TokenType.Indent))
                {
                    depth++;
                    continue;
                }

                if (Match(TokenType.Dedent))
                {
                    depth--;
                    continue;
                }

                Advance();
            }
        }

        private ModuleDecl ParseModuleDecl(Token moduleToken)
        {
            Token name = ConsumeIdentifier("Expected module name after 'module'.");
            return new ModuleDecl(name, moduleToken.Line, moduleToken.Column);
        }

        private ImportStmt ParseImportStmt(Token importToken)
        {
            List<string> path = ParseDottedName();
            string alias = null;
            if (Match(TokenType.As))
                alias = ConsumeIdentifier("Expected alias name after 'as'.").Value;

            return new ImportStmt(path, alias, null, importToken.Line, importToken.Column);
        }

        private ImportStmt ParseFromImportStmt(Token fromToken)
        {
            List<string> path = ParseDottedName();
            Consume(TokenType.Import, "Expected 'import' after module path.");

            var names = new List<string>();
            if (Match(TokenType.Star))
            {
                names.Add("*");
            }
            else
            {
                names.Add(ConsumeIdentifier("Expected imported name.").Value);
                while (Match(TokenType.Comma))
                    names.Add(ConsumeIdentifier("Expected imported name after ','.").Value);
            }

            return new ImportStmt(path, null, names, fromToken.Line, fromToken.Column);
        }

        private List<string> ParseDottedName()
        {
            var parts = new List<string>();
            parts.Add(ConsumeIdentifier("Expected identifier.").Value);

            while (Match(TokenType.Dot))
                parts.Add(ConsumeIdentifier("Expected identifier after '.'.").Value);

            return parts;
        }

        private ConstDecl ParseConstDecl(Token constToken)
        {
            Token name = ConsumeIdentifier("Expected constant name after 'const'.");
            TypeRef typeAnnotation = null;
            if (Match(TokenType.Colon))
                typeAnnotation = ParseTypeRef();

            Consume(TokenType.Equal, "Expected '=' in const declaration.");
            GrowlNode value = ParseExpression();
            return new ConstDecl(name, typeAnnotation, value, constToken.Line, constToken.Column);
        }

        private TypeAliasDecl ParseTypeAliasDecl(Token typeToken)
        {
            Token name = ConsumeIdentifier("Expected alias name after 'type'.");
            List<TypeRef> typeParams = ParseOptionalGenericParamList();

            Consume(TokenType.Equal, "Expected '=' in type alias declaration.");
            TypeRef target = ParseTypeRef();

            if (MatchSoftKeyword("where"))
                ParseExpression();

            return new TypeAliasDecl(name, target, typeParams, typeToken.Line, typeToken.Column);
        }

        private ClassDecl ParseClassDeclCore(List<Decorator> decorators, bool isAbstract, Token classToken)
        {
            Token name = ConsumeIdentifier("Expected class name after 'class'.");
            ParseOptionalGenericParamList();

            TypeRef superclass = null;
            var traits = new List<TypeRef>();
            var mixins = new List<TypeRef>();

            if (MatchSoftKeyword("extends"))
                superclass = ParseTypeRef();
            if (MatchSoftKeyword("implements"))
                traits = ParseTypeRefList();
            if (MatchSoftKeyword("with"))
                mixins = ParseTypeRefList();

            Consume(TokenType.Colon, "Expected ':' after class declaration.");
            Consume(TokenType.Newline, "Expected newline before class body.");
            Consume(TokenType.Indent, "Expected indented class body.");

            var members = new List<GrowlNode>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                int start = _current;
                GrowlNode member = ParseClassMember();
                if (member != null)
                    members.Add(member);

                ConsumeStatementTerminators();

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress inside class body; skipping token.");
                    Advance();
                }
            }

            Consume(TokenType.Dedent, "Expected dedent to close class body.");
            return new ClassDecl(name, superclass, traits, mixins, members, isAbstract, decorators, classToken.Line, classToken.Column);
        }

        private GrowlNode ParseClassMember()
        {
            if (Check(TokenType.At))
            {
                List<Decorator> decorators = ParseDecorators();
                if (Match(TokenType.Fn))
                    return ParseFnDeclCore(decorators, Previous());

                AddError(Current(), "Expected 'fn' after class member decorators.");
                throw new ParseAbortException();
            }

            if (Match(TokenType.Fn))
                return ParseFnDeclCore(new List<Decorator>(), Previous());

            if (Match(TokenType.Abstract))
            {
                if (Match(TokenType.Fn))
                    return ParseAbstractFnDecl(Previous());

                AddError(Current(), "Expected 'fn' after 'abstract' in class body.");
                throw new ParseAbortException();
            }

            // Class fields are part of the full language spec but do not have a
            // dedicated AST node yet. Parse and discard for forward compatibility.
            if (Check(TokenType.Identifier) && CheckNext(TokenType.Colon))
            {
                ParseFieldDecl();
                return null;
            }

            AddError(Current(), "Unsupported class member.");
            throw new ParseAbortException();
        }

        private StructDecl ParseStructDecl(Token structToken)
        {
            Token name = ConsumeIdentifier("Expected struct name after 'struct'.");
            ParseOptionalGenericParamList();
            Consume(TokenType.Colon, "Expected ':' after struct declaration.");
            Consume(TokenType.Newline, "Expected newline before struct body.");
            Consume(TokenType.Indent, "Expected indented struct body.");

            var fields = new List<FieldDecl>();
            var methods = new List<FnDecl>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                int start = _current;

                if (Check(TokenType.At))
                {
                    List<Decorator> decorators = ParseDecorators();
                    Token fnToken = Consume(TokenType.Fn, "Expected 'fn' after struct member decorators.");
                    methods.Add(ParseFnDeclCore(decorators, fnToken));
                }
                else if (Match(TokenType.Fn))
                {
                    methods.Add(ParseFnDeclCore(new List<Decorator>(), Previous()));
                }
                else if (Check(TokenType.Identifier) && CheckNext(TokenType.Colon))
                {
                    fields.Add(ParseFieldDecl());
                }
                else
                {
                    AddError(Current(), "Expected struct field or method.");
                    throw new ParseAbortException();
                }

                ConsumeStatementTerminators();

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress inside struct body; skipping token.");
                    Advance();
                }
            }

            Consume(TokenType.Dedent, "Expected dedent to close struct body.");
            return new StructDecl(name, fields, methods, structToken.Line, structToken.Column);
        }

        private EnumDecl ParseEnumDecl(Token enumToken)
        {
            Token name = ConsumeIdentifier("Expected enum name after 'enum'.");
            var superTraits = new List<TypeRef>();

            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                    superTraits = ParseTypeRefList();
                Consume(TokenType.RightParen, "Expected ')' after enum super traits.");
            }

            Consume(TokenType.Colon, "Expected ':' after enum declaration.");
            Consume(TokenType.Newline, "Expected newline before enum body.");
            Consume(TokenType.Indent, "Expected indented enum body.");

            var members = new List<EnumMember>();
            var methods = new List<FnDecl>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                int start = _current;

                if (Check(TokenType.At))
                {
                    List<Decorator> decorators = ParseDecorators();
                    Token fnToken = Consume(TokenType.Fn, "Expected 'fn' after enum member decorators.");
                    methods.Add(ParseFnDeclCore(decorators, fnToken));
                }
                else if (Match(TokenType.Fn))
                {
                    methods.Add(ParseFnDeclCore(new List<Decorator>(), Previous()));
                }
                else if (Check(TokenType.Identifier))
                {
                    Token memberName = Advance();
                    GrowlNode value = null;
                    if (Match(TokenType.Equal))
                        value = ParseExpression();

                    members.Add(new EnumMember(memberName, value: value));
                }
                else
                {
                    AddError(Current(), "Expected enum member or method.");
                    throw new ParseAbortException();
                }

                ConsumeStatementTerminators();

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress inside enum body; skipping token.");
                    Advance();
                }
            }

            Consume(TokenType.Dedent, "Expected dedent to close enum body.");
            return new EnumDecl(name, superTraits, members, methods, enumToken.Line, enumToken.Column);
        }

        private TraitDecl ParseTraitDecl(Token traitToken)
        {
            Token name = ConsumeIdentifier("Expected trait name after 'trait'.");
            ParseOptionalGenericParamList();
            Consume(TokenType.Colon, "Expected ':' after trait declaration.");
            Consume(TokenType.Newline, "Expected newline before trait body.");
            Consume(TokenType.Indent, "Expected indented trait body.");

            var members = new List<GrowlNode>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                int start = _current;

                if (Check(TokenType.At))
                {
                    List<Decorator> decorators = ParseDecorators();
                    Token fnToken = Consume(TokenType.Fn, "Expected 'fn' after trait member decorators.");
                    members.Add(ParseFnDeclCore(decorators, fnToken));
                }
                else if (Match(TokenType.Fn))
                {
                    members.Add(ParseFnDeclCore(new List<Decorator>(), Previous()));
                }
                else if (Match(TokenType.Abstract))
                {
                    Consume(TokenType.Fn, "Expected 'fn' after 'abstract' in trait.");
                    members.Add(ParseAbstractFnDecl(Previous()));
                }
                else
                {
                    AddError(Current(), "Expected trait method declaration.");
                    throw new ParseAbortException();
                }

                ConsumeStatementTerminators();

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress inside trait body; skipping token.");
                    Advance();
                }
            }

            Consume(TokenType.Dedent, "Expected dedent to close trait body.");
            return new TraitDecl(name, members, traitToken.Line, traitToken.Column);
        }

        private MixinDecl ParseMixinDecl(Token mixinToken)
        {
            Token name = ConsumeIdentifier("Expected mixin name after 'mixin'.");
            Consume(TokenType.Colon, "Expected ':' after mixin declaration.");
            Consume(TokenType.Newline, "Expected newline before mixin body.");
            Consume(TokenType.Indent, "Expected indented mixin body.");

            var methods = new List<FnDecl>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                int start = _current;

                if (Check(TokenType.At))
                {
                    List<Decorator> decorators = ParseDecorators();
                    Token fnToken = Consume(TokenType.Fn, "Expected 'fn' after mixin member decorators.");
                    methods.Add(ParseFnDeclCore(decorators, fnToken));
                }
                else if (Match(TokenType.Fn))
                {
                    methods.Add(ParseFnDeclCore(new List<Decorator>(), Previous()));
                }
                else
                {
                    AddError(Current(), "Expected mixin method declaration.");
                    throw new ParseAbortException();
                }

                ConsumeStatementTerminators();

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress inside mixin body; skipping token.");
                    Advance();
                }
            }

            Consume(TokenType.Dedent, "Expected dedent to close mixin body.");
            return new MixinDecl(name, methods, mixinToken.Line, mixinToken.Column);
        }

        private FnDecl ParseAbstractFnDecl(Token fnToken)
        {
            Token name = ConsumeIdentifier("Expected method name after 'fn'.");
            Consume(TokenType.LeftParen, "Expected '(' after method name.");

            var parameters = new List<Param>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    parameters.Add(ParseParam());
                }
                while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after parameters.");

            TypeRef returnType = null;
            if (Match(TokenType.Arrow))
                returnType = ParseTypeRef();

            return new FnDecl(name, parameters, returnType, new List<GrowlNode>(), new List<Decorator>(), fnToken.Line, fnToken.Column);
        }

        private FieldDecl ParseFieldDecl()
        {
            Token name = ConsumeIdentifier("Expected field name.");
            Consume(TokenType.Colon, "Expected ':' after field name.");
            TypeRef typeAnnotation = ParseTypeRef();

            GrowlNode defaultValue = null;
            if (Match(TokenType.Equal))
                defaultValue = ParseExpression();

            return new FieldDecl(name, typeAnnotation, defaultValue);
        }

        private List<TypeRef> ParseOptionalGenericParamList()
        {
            var typeParams = new List<TypeRef>();
            if (!Match(TokenType.LeftBracket))
                return typeParams;

            if (!Check(TokenType.RightBracket))
            {
                do
                {
                    Token paramName = ConsumeIdentifier("Expected generic type parameter name.");
                    typeParams.Add(new TypeRef(paramName.Value));
                }
                while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightBracket, "Expected ']' after generic type parameter list.");
            return typeParams;
        }

        private List<TypeRef> ParseTypeRefList()
        {
            var types = new List<TypeRef> { ParseTypeRef() };
            while (Match(TokenType.Comma))
                types.Add(ParseTypeRef());
            return types;
        }

        private PhaseBlock ParsePhaseBlock(Token phaseToken)
        {
            Token nameToken = ConsumeStringLiteralToken("Expected phase name string after 'phase'.");
            GrowlNode minAge = null;
            GrowlNode maxAge = null;
            GrowlNode condition = null;

            if (Match(TokenType.LeftParen))
            {
                minAge = ParseExpression();
                Consume(TokenType.Comma, "Expected ',' in phase age range.");
                maxAge = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after phase age range.");
            }
            else if (Match(TokenType.When))
            {
                condition = ParseExpression();
            }
            else
            {
                AddError(Current(), "Expected '(min, max)' or 'when <condition>' after phase name.");
                throw new ParseAbortException();
            }

            Consume(TokenType.Colon, "Expected ':' after phase header.");
            List<GrowlNode> body = ParseIndentedBlock("phase");
            return new PhaseBlock(nameToken.Value, minAge, maxAge, condition, body, phaseToken.Line, phaseToken.Column);
        }

        private WhenBlock ParseWhenBlock(Token whenToken)
        {
            GrowlNode condition = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after when condition.");
            List<GrowlNode> body = ParseIndentedBlock("when");

            List<GrowlNode> thenBlock = null;
            if (Match(TokenType.Then))
            {
                Token chainedWhen = Consume(TokenType.When, "Expected 'when' after 'then'.");
                thenBlock = new List<GrowlNode> { ParseWhenBlock(chainedWhen) };
            }

            return new WhenBlock(condition, body, thenBlock, whenToken.Line, whenToken.Column);
        }

        private RespondBlock ParseRespondBlock(Token respondToken)
        {
            Consume(TokenType.To, "Expected 'to' after 'respond'.");
            Token eventToken = ConsumeStringLiteralToken("Expected event name string in respond block.");

            Token? binding = null;
            if (Match(TokenType.As))
                binding = ConsumeIdentifier("Expected binding name after 'as'.");

            Consume(TokenType.Colon, "Expected ':' after respond header.");
            List<GrowlNode> body = ParseIndentedBlock("respond");
            return new RespondBlock(eventToken.Value, binding, body, respondToken.Line, respondToken.Column);
        }

        private AdaptBlock ParseAdaptBlock(Token adaptToken)
        {
            GrowlNode subject = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after adapt subject.");
            Consume(TokenType.Newline, "Expected newline before adapt block.");
            Consume(TokenType.Indent, "Expected indented adapt block.");

            var rules = new List<AdaptRule>();
            GrowlNode budget = null;

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                int start = _current;

                if (Match(TokenType.Toward))
                {
                    GrowlNode action = ParseExpression();
                    GrowlNode ruleCondition;

                    if (Match(TokenType.When))
                    {
                        ruleCondition = ParseExpression();
                    }
                    else if (Match(TokenType.Otherwise))
                    {
                        Token otherwise = Previous();
                        ruleCondition = MakeNoneLiteral(otherwise.Line, otherwise.Column);
                    }
                    else
                    {
                        AddError(Current(), "Expected 'when' or 'otherwise' in adapt rule.");
                        throw new ParseAbortException();
                    }

                    rules.Add(new AdaptRule(ruleCondition, action));
                    ConsumeStatementTerminators();
                }
                else if (Match(TokenType.Rate))
                {
                    budget = ParseExpression();
                    ConsumeStatementTerminators();
                }
                else
                {
                    AddError(Current(), "Expected 'toward' rule or 'rate' expression in adapt block.");
                    throw new ParseAbortException();
                }

                if (_current == start)
                {
                    AddError(Current(), "Parser made no progress inside adapt block; skipping token.");
                    Advance();
                }
            }

            Consume(TokenType.Dedent, "Expected dedent to close adapt block.");
            if (budget == null)
            {
                AddError(adaptToken, "Adapt block requires a 'rate' expression.");
                budget = MakeNoneLiteral(adaptToken.Line, adaptToken.Column);
            }

            return new AdaptBlock(subject, rules, budget, adaptToken.Line, adaptToken.Column);
        }

        private CycleBlock ParseCycleBlock(Token cycleToken)
        {
            Token nameToken = ConsumeStringLiteralToken("Expected cycle name string after 'cycle'.");
            Consume(TokenType.Period, "Expected 'period' in cycle declaration.");
            GrowlNode period = ParseExpression();
            Consume(TokenType.Colon, "Expected ':' after cycle header.");
            Consume(TokenType.Newline, "Expected newline before cycle block.");
            Consume(TokenType.Indent, "Expected indented cycle block.");

            var points = new List<CyclePoint>();

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                ConsumeIgnorable();
                if (Check(TokenType.Dedent) || IsAtEnd())
                    break;

                Consume(TokenType.CycleAt, "Expected 'at' entry in cycle block.");
                GrowlNode at = ParseExpression();
                Consume(TokenType.Colon, "Expected ':' after cycle point expression.");
                List<GrowlNode> pointBody = ParseIndentedBlock("cycle point");
                points.Add(new CyclePoint(at, pointBody));

                ConsumeStatementTerminators();
            }

            Consume(TokenType.Dedent, "Expected dedent to close cycle block.");
            return new CycleBlock(nameToken.Value, period, points, cycleToken.Line, cycleToken.Column);
        }

        private TickerDecl ParseTickerDecl(Token tickerToken)
        {
            Token nameToken = ConsumeStringLiteralToken("Expected ticker name string after 'ticker'.");
            Consume(TokenType.Every, "Expected 'every' in ticker declaration.");
            GrowlNode interval = ParseExpression();
            if (CheckSoftKeyword("ticks"))
                Advance();

            Consume(TokenType.Colon, "Expected ':' after ticker header.");
            List<GrowlNode> body = ParseIndentedBlock("ticker");
            return new TickerDecl(nameToken.Value, interval, body, tickerToken.Line, tickerToken.Column);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Expressions
        // ─────────────────────────────────────────────────────────────────────

        private GrowlNode ParseExpression() => ParseTernaryExpression();

        private GrowlNode ParseTernaryExpression()
        {
            GrowlNode thenExpr = ParseOrExpression();
            if (Match(TokenType.If))
            {
                GrowlNode condition = ParseExpression();
                Consume(TokenType.Else, "Expected 'else' in ternary expression.");
                GrowlNode elseExpr = ParseExpression();
                return new TernaryExpr(thenExpr, condition, elseExpr);
            }

            return thenExpr;
        }

        private GrowlNode ParseOrExpression()
        {
            GrowlNode expr = ParseAndExpression();
            while (Match(TokenType.Or))
            {
                Token op = Previous();
                GrowlNode right = ParseAndExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseAndExpression()
        {
            GrowlNode expr = ParseNotExpression();
            while (Match(TokenType.And))
            {
                Token op = Previous();
                GrowlNode right = ParseNotExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseNotExpression()
        {
            if (Match(TokenType.Not))
            {
                Token op = Previous();
                GrowlNode operand = ParseNotExpression();
                return new UnaryExpr(op, operand);
            }

            return ParseComparisonExpression();
        }

        private GrowlNode ParseComparisonExpression()
        {
            GrowlNode expr = ParsePipeExpression();

            while (true)
            {
                if (MatchAny(TokenType.EqualEqual, TokenType.BangEqual,
                             TokenType.Less, TokenType.Greater,
                             TokenType.LessEqual, TokenType.GreaterEqual,
                             TokenType.In))
                {
                    Token op = Previous();
                    GrowlNode right = ParsePipeExpression();
                    expr = new BinaryExpr(expr, op, right);
                    continue;
                }

                if (Match(TokenType.Is))
                {
                    Token op = Previous();
                    if (Match(TokenType.Not))
                        op = new Token(TokenType.Is, "is not", op.Line, op.Column);

                    GrowlNode right = ParsePipeExpression();
                    expr = new BinaryExpr(expr, op, right);
                    continue;
                }

                if (Check(TokenType.Not) && CheckNext(TokenType.In))
                {
                    Token notToken = Advance();
                    Advance(); // consume 'in'
                    Token op = new Token(TokenType.In, "not in", notToken.Line, notToken.Column);
                    GrowlNode right = ParsePipeExpression();
                    expr = new BinaryExpr(expr, op, right);
                    continue;
                }

                break;
            }

            return expr;
        }

        private GrowlNode ParsePipeExpression()
        {
            GrowlNode expr = ParseNullishExpression();
            while (Match(TokenType.PipeGreater))
            {
                Token op = Previous();
                GrowlNode right = ParseCallExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseNullishExpression()
        {
            GrowlNode expr = ParseBitwiseOrExpression();
            while (Match(TokenType.QuestionQuestion))
            {
                Token op = Previous();
                GrowlNode right = ParseBitwiseOrExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseBitwiseOrExpression()
        {
            GrowlNode expr = ParseBitwiseXorExpression();
            while (Match(TokenType.Pipe))
            {
                Token op = Previous();
                GrowlNode right = ParseBitwiseXorExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseBitwiseXorExpression()
        {
            GrowlNode expr = ParseBitwiseAndExpression();
            while (Match(TokenType.Caret))
            {
                Token op = Previous();
                GrowlNode right = ParseBitwiseAndExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseBitwiseAndExpression()
        {
            GrowlNode expr = ParseShiftExpression();
            while (Match(TokenType.Ampersand))
            {
                Token op = Previous();
                GrowlNode right = ParseShiftExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseShiftExpression()
        {
            GrowlNode expr = ParseArithmeticExpression();
            while (MatchAny(TokenType.LessLess, TokenType.GreaterGreater))
            {
                Token op = Previous();
                GrowlNode right = ParseArithmeticExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseArithmeticExpression()
        {
            GrowlNode expr = ParseTermExpression();
            while (MatchAny(TokenType.Plus, TokenType.Minus))
            {
                Token op = Previous();
                GrowlNode right = ParseTermExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseTermExpression()
        {
            GrowlNode expr = ParsePowerExpression();
            while (MatchAny(TokenType.Star, TokenType.Slash, TokenType.SlashSlash, TokenType.Percent))
            {
                Token op = Previous();
                GrowlNode right = ParsePowerExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParsePowerExpression()
        {
            GrowlNode expr = ParseUnaryExpression();
            if (Match(TokenType.StarStar))
            {
                Token op = Previous();
                GrowlNode right = ParsePowerExpression();
                expr = new BinaryExpr(expr, op, right);
            }

            return expr;
        }

        private GrowlNode ParseUnaryExpression()
        {
            if (MatchAny(TokenType.Plus, TokenType.Minus, TokenType.Tilde, TokenType.Caret))
            {
                Token op = Previous();
                GrowlNode operand = ParseUnaryExpression();
                return new UnaryExpr(op, operand);
            }

            // Magnitude syntax: |expr|
            if (Match(TokenType.Pipe))
            {
                Token op = Previous();
                GrowlNode inner = ParseExpression();
                Consume(TokenType.Pipe, "Expected '|' to close magnitude expression.");
                return new UnaryExpr(op, inner);
            }

            return ParseCallExpression();
        }

        private GrowlNode ParseCallExpression()
        {
            GrowlNode expr = ParsePrimaryExpression();

            while (true)
            {
                if (Match(TokenType.LeftParen))
                {
                    expr = FinishCall(expr);
                    continue;
                }

                if (Match(TokenType.LeftBracket))
                {
                    GrowlNode index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after subscript.");
                    expr = new SubscriptExpr(expr, index);
                    continue;
                }

                if (Match(TokenType.Dot))
                {
                    Token field = ConsumeMemberName("Expected property name after '.'.");
                    expr = new AttributeExpr(expr, field);
                    continue;
                }

                break;
            }

            return expr;
        }

        private GrowlNode FinishCall(GrowlNode callee)
        {
            var args = new List<Argument>();

            if (!Check(TokenType.RightParen))
            {
                do
                {
                    if (Match(TokenType.DotDotDot))
                    {
                        Token spread = Previous();
                        GrowlNode spreadExpr = ParseExpression();
                        args.Add(new Argument(new SpreadExpr(spreadExpr, spread.Line, spread.Column)));
                        continue;
                    }

                    if (IsMemberNameToken(Current().Type) && CheckNext(TokenType.Colon))
                    {
                        Token name = Advance();
                        Consume(TokenType.Colon, "Expected ':' after named argument.");
                        GrowlNode value = ParseExpression();
                        args.Add(new Argument(value, name));
                        continue;
                    }

                    args.Add(new Argument(ParseExpression()));
                }
                while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after arguments.");
            return new CallExpr(callee, args, callee.Line, callee.Column);
        }

        private GrowlNode ParsePrimaryExpression()
        {
            if (Match(TokenType.Integer)) return new IntegerLiteralExpr(Previous());
            if (Match(TokenType.Float)) return new FloatLiteralExpr(Previous());
            if (Match(TokenType.String) || Match(TokenType.MultilineString) || Match(TokenType.RawString))
                return new StringLiteralExpr(Previous());
            if (Match(TokenType.True) || Match(TokenType.False)) return new BoolLiteralExpr(Previous());
            if (Match(TokenType.None)) return new NoneLiteralExpr(Previous());
            if (Match(TokenType.Color)) return new ColorLiteralExpr(Previous());
            if (Match(TokenType.InterpStrStart)) return ParseInterpolatedString(Previous());

            if (Match(TokenType.Identifier) || Match(TokenType.Self) ||
                Match(TokenType.Super) || Match(TokenType.Cls))
                return new NameExpr(Previous());

            if (Match(TokenType.LeftParen)) return ParseGroupedOrTuple(Previous());
            if (Match(TokenType.LeftBracket)) return ParseListExpression(Previous());
            if (Match(TokenType.LeftBrace)) return ParseDictOrSetExpression(Previous());
            if (Match(TokenType.Less)) return ParseVectorLiteral(Previous());
            if (Match(TokenType.Fn)) return ParseLambdaExpression(Previous());

            AddError(Current(), "Expected expression.");
            return MakeRecoveryExpression();
        }

        private GrowlNode ParseInterpolatedString(Token start)
        {
            var segments = new List<GrowlNode>();

            while (!Check(TokenType.InterpStrEnd) && !IsAtEnd())
            {
                if (Match(TokenType.InterpStrText))
                {
                    segments.Add(new StringLiteralExpr(Previous()));
                    continue;
                }

                if (Match(TokenType.InterpStart))
                {
                    GrowlNode expr = ParseExpression();
                    Consume(TokenType.InterpEnd, "Expected '}' after interpolation expression.");
                    segments.Add(expr);
                    continue;
                }

                AddError(Current(), "Unexpected token in interpolated string.");
                Advance();
            }

            Consume(TokenType.InterpStrEnd, "Unterminated interpolated string.");
            return new InterpolatedStringExpr(segments, start.Line, start.Column);
        }

        private GrowlNode ParseGroupedOrTuple(Token open)
        {
            if (Match(TokenType.RightParen))
                return new TupleExpr(new List<GrowlNode>(), open.Line, open.Column);

            GrowlNode first = ParseExpression();

            if (Match(TokenType.Comma))
            {
                var elements = new List<GrowlNode> { first };
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        elements.Add(ParseExpression());
                    }
                    while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expected ')' after tuple.");
                return new TupleExpr(elements, open.Line, open.Column);
            }

            Consume(TokenType.RightParen, "Expected ')' after grouped expression.");
            return first;
        }

        private GrowlNode ParseListExpression(Token open)
        {
            if (Match(TokenType.RightBracket))
                return new ListExpr(new List<GrowlNode>(), open.Line, open.Column);

            GrowlNode first = ParseExpression();

            if (Match(TokenType.For))
            {
                var clauses = new List<ComprehensionClause>
                {
                    ParseComprehensionClauseAfterFor()
                };

                while (Match(TokenType.For))
                    clauses.Add(ParseComprehensionClauseAfterFor());

                if (Match(TokenType.If))
                {
                    GrowlNode filter = ParseExpression();
                    int last = clauses.Count - 1;
                    clauses[last] = new ComprehensionClause(
                        clauses[last].Targets,
                        clauses[last].Iterable,
                        filter);
                }

                Consume(TokenType.RightBracket, "Expected ']' after list comprehension.");
                return new ListComprehensionExpr(first, clauses, open.Line, open.Column);
            }

            var elements = new List<GrowlNode> { first };
            while (Match(TokenType.Comma))
            {
                if (Check(TokenType.RightBracket)) break;
                elements.Add(ParseExpression());
            }

            Consume(TokenType.RightBracket, "Expected ']' after list.");
            return new ListExpr(elements, open.Line, open.Column);
        }

        private ComprehensionClause ParseComprehensionClauseAfterFor()
        {
            List<string> targets = ParseTargetList();
            Consume(TokenType.In, "Expected 'in' in comprehension.");
            GrowlNode iterable = ParseExpression();
            return new ComprehensionClause(targets, iterable, null);
        }

        private GrowlNode ParseDictOrSetExpression(Token open)
        {
            if (Match(TokenType.RightBrace))
                return new DictExpr(new List<DictEntry>(), open.Line, open.Column);

            GrowlNode first = ParseExpression();

            if (Match(TokenType.Colon))
            {
                GrowlNode firstValue = ParseExpression();

                if (Match(TokenType.For))
                {
                    var clauses = new List<ComprehensionClause>
                    {
                        ParseComprehensionClauseAfterFor()
                    };

                    while (Match(TokenType.For))
                        clauses.Add(ParseComprehensionClauseAfterFor());

                    if (Match(TokenType.If))
                    {
                        GrowlNode filter = ParseExpression();
                        int last = clauses.Count - 1;
                        clauses[last] = new ComprehensionClause(
                            clauses[last].Targets,
                            clauses[last].Iterable,
                            filter);
                    }

                    Consume(TokenType.RightBrace, "Expected '}' after dictionary comprehension.");
                    return new DictComprehensionExpr(first, firstValue, clauses, open.Line, open.Column);
                }

                var entries = new List<DictEntry> { new DictEntry(first, firstValue) };
                while (Match(TokenType.Comma))
                {
                    if (Check(TokenType.RightBrace)) break;

                    if (Match(TokenType.DotDotDot))
                    {
                        Token spread = Previous();
                        GrowlNode spreadExpr = ParseExpression();
                        entries.Add(new DictEntry(
                            new SpreadExpr(spreadExpr, spread.Line, spread.Column),
                            MakeNoneLiteral(spread.Line, spread.Column)));
                        continue;
                    }

                    if (Check(TokenType.Identifier) && !CheckNext(TokenType.Colon))
                    {
                        Token id = Advance();
                        var keyToken = new Token(TokenType.String, id.Value, id.Line, id.Column);
                        entries.Add(new DictEntry(new StringLiteralExpr(keyToken), new NameExpr(id)));
                        continue;
                    }

                    GrowlNode key = ParseExpression();
                    Consume(TokenType.Colon, "Expected ':' in dictionary entry.");
                    GrowlNode value = ParseExpression();
                    entries.Add(new DictEntry(key, value));
                }

                Consume(TokenType.RightBrace, "Expected '}' after dictionary.");
                return new DictExpr(entries, open.Line, open.Column);
            }

            var elements = new List<GrowlNode> { first };
            while (Match(TokenType.Comma))
            {
                if (Check(TokenType.RightBrace)) break;
                elements.Add(ParseExpression());
            }

            Consume(TokenType.RightBrace, "Expected '}' after set.");
            if (elements.Count < 2)
                AddError(open, "Set literals require at least two elements.");

            return new SetExpr(elements, open.Line, open.Column);
        }

        private GrowlNode ParseVectorLiteral(Token open)
        {
            GrowlNode x = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' after first vector component.");
            GrowlNode y = ParseExpression();

            GrowlNode z = null;
            if (Match(TokenType.Comma))
                z = ParseExpression();

            Consume(TokenType.Greater, "Expected '>' to close vector literal.");
            return new VectorExpr(x, y, z, open.Line, open.Column);
        }

        private GrowlNode ParseLambdaExpression(Token fnToken)
        {
            Consume(TokenType.LeftParen, "Expected '(' after lambda 'fn'.");

            var parameters = new List<Param>();
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    parameters.Add(ParseParam());
                }
                while (Match(TokenType.Comma));
            }

            Consume(TokenType.RightParen, "Expected ')' after lambda parameters.");

            if (Match(TokenType.Arrow))
            {
                GrowlNode bodyExpr = ParseExpression();
                return new LambdaExpr(parameters, bodyExpr, fnToken.Line, fnToken.Column);
            }

            if (Match(TokenType.Colon))
            {
                List<GrowlNode> body = ParseIndentedBlock("lambda");
                return new LambdaExpr(parameters, new ProgramNode(body), fnToken.Line, fnToken.Column);
            }

            AddError(Current(), "Expected '->' or ':' after lambda parameters.");
            return new LambdaExpr(parameters, MakeNoneLiteral(fnToken.Line, fnToken.Column), fnToken.Line, fnToken.Column);
        }

        private GrowlNode MakeRecoveryExpression()
        {
            Token here = Current();
            if (!IsAtEnd())
                Advance();
            return MakeNoneLiteral(here.Line, here.Column);
        }

        private NoneLiteralExpr MakeNoneLiteral(int line, int column)
            => new NoneLiteralExpr(new Token(TokenType.None, "none", line, column));

        // ─────────────────────────────────────────────────────────────────────
        // Recovery / synchronization
        // ─────────────────────────────────────────────────────────────────────

        private void SynchronizeToNextStatement()
        {
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.Newline)
                {
                    while (Match(TokenType.Indent) || Match(TokenType.Dedent))
                    {
                    }
                    return;
                }

                switch (Current().Type)
                {
                    case TokenType.At:
                    case TokenType.Abstract:
                    case TokenType.Fn:
                    case TokenType.Struct:
                    case TokenType.Enum:
                    case TokenType.Trait:
                    case TokenType.Mixin:
                    case TokenType.Phase:
                    case TokenType.When:
                    case TokenType.Respond:
                    case TokenType.Adapt:
                    case TokenType.Cycle:
                    case TokenType.Ticker:
                    case TokenType.Match:
                    case TokenType.Try:
                    case TokenType.If:
                    case TokenType.For:
                    case TokenType.While:
                    case TokenType.Loop:
                    case TokenType.Return:
                    case TokenType.Import:
                    case TokenType.From:
                    case TokenType.Module:
                    case TokenType.Defer:
                    case TokenType.Mutate:
                    case TokenType.Class:
                    case TokenType.Const:
                    case TokenType.Type:
                        return;
                }

                Advance();
            }
        }

        private void ConsumeTopLevelIgnorable()
        {
            while (Match(TokenType.Newline) ||
                   Match(TokenType.DocComment) ||
                   Match(TokenType.WarnComment) ||
                   Match(TokenType.Indent) ||
                   Match(TokenType.Dedent))
            {
            }
        }

        private void ConsumeIgnorable()
        {
            while (Match(TokenType.Newline) || Match(TokenType.DocComment) || Match(TokenType.WarnComment))
            {
            }
        }

        private void ConsumeStatementTerminators()
        {
            while (Match(TokenType.Newline) || Match(TokenType.DocComment) || Match(TokenType.WarnComment))
            {
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Token helpers
        // ─────────────────────────────────────────────────────────────────────

        private bool IsAtEnd() => Current().Type == TokenType.Eof;

        private Token Current() => _tokens[_current];

        private Token Previous() => _current == 0 ? _tokens[0] : _tokens[_current - 1];

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return type == TokenType.Eof;
            return Current().Type == type;
        }

        private bool CheckNext(TokenType type)
        {
            if (_current + 1 >= _tokens.Count) return false;
            return _tokens[_current + 1].Type == type;
        }

        private bool CheckSoftKeyword(string keyword)
            => Check(TokenType.Identifier) && Current().Value == keyword;

        private bool MatchSoftKeyword(string keyword)
        {
            if (!CheckSoftKeyword(keyword)) return false;
            Advance();
            return true;
        }

        private bool Match(TokenType type)
        {
            if (!Check(type)) return false;
            Advance();
            return true;
        }

        private bool MatchAny(params TokenType[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (Check(types[i]))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();

            AddError(Current(), message);
            throw new ParseAbortException();
        }

        private Token ConsumeIdentifier(string message)
        {
            if (Check(TokenType.Identifier)) return Advance();

            AddError(Current(), message);
            throw new ParseAbortException();
        }

        private Token ConsumeParameterName(string message)
        {
            if (Check(TokenType.Identifier) || Check(TokenType.Self) || Check(TokenType.Cls))
                return Advance();

            AddError(Current(), message);
            throw new ParseAbortException();
        }

        private Token ConsumeStringLiteralToken(string message)
        {
            if (Check(TokenType.String) || Check(TokenType.RawString) || Check(TokenType.MultilineString))
                return Advance();

            AddError(Current(), message);
            throw new ParseAbortException();
        }

        private Token ConsumeMemberName(string message)
        {
            if (IsMemberNameToken(Current().Type))
                return Advance();

            AddError(Current(), message);
            throw new ParseAbortException();
        }

        private static bool IsMemberNameToken(TokenType type)
        {
            switch (type)
            {
                case TokenType.Identifier:
                case TokenType.Self:
                case TokenType.Super:
                case TokenType.Cls:
                case TokenType.If:
                case TokenType.Elif:
                case TokenType.Else:
                case TokenType.For:
                case TokenType.In:
                case TokenType.While:
                case TokenType.Loop:
                case TokenType.Break:
                case TokenType.Continue:
                case TokenType.Return:
                case TokenType.Yield:
                case TokenType.Fn:
                case TokenType.Class:
                case TokenType.Struct:
                case TokenType.Enum:
                case TokenType.Trait:
                case TokenType.Mixin:
                case TokenType.Abstract:
                case TokenType.Static:
                case TokenType.Const:
                case TokenType.Type:
                case TokenType.Module:
                case TokenType.Import:
                case TokenType.From:
                case TokenType.As:
                case TokenType.Match:
                case TokenType.Case:
                case TokenType.Try:
                case TokenType.Recover:
                case TokenType.Always:
                case TokenType.And:
                case TokenType.Or:
                case TokenType.Not:
                case TokenType.Is:
                case TokenType.Phase:
                case TokenType.When:
                case TokenType.Then:
                case TokenType.Respond:
                case TokenType.To:
                case TokenType.Adapt:
                case TokenType.Toward:
                case TokenType.Rate:
                case TokenType.Otherwise:
                case TokenType.Cycle:
                case TokenType.CycleAt:
                case TokenType.Period:
                case TokenType.Ticker:
                case TokenType.Every:
                case TokenType.Wait:
                case TokenType.Defer:
                case TokenType.Until:
                case TokenType.Mutate:
                case TokenType.By:
                    return true;
                default:
                    return false;
            }
        }

        private void AddError(Token token, string message)
            => _errors.Add(new ParseError(message, token.Line, token.Column));
    }
}
