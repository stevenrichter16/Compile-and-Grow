using NUnit.Framework;
using System.Collections.Generic;
using GrowlLanguage.Lexer;
using GrowlLanguage.AST;

namespace GrowlLanguage.Tests.AST
{
    // ─────────────────────────────────────────────────────────────────────────
    // Statement and declaration node construction tests.
    // ─────────────────────────────────────────────────────────────────────────

    [TestFixture]
    public class StatementNodeTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static Token T(TokenType t, string v, int ln = 1, int col = 1)
            => new Token(t, v, ln, col);

        private static Token IdTok(string v) => T(TokenType.Identifier, v);
        private static Token IntTok(string v) => T(TokenType.Integer, v);

        private static IntegerLiteralExpr Int(string v)
            => new IntegerLiteralExpr(IntTok(v));

        private static NameExpr Name(string v)
            => new NameExpr(IdTok(v));

        private static List<GrowlNode> Block(params GrowlNode[] stmts)
            => new List<GrowlNode>(stmts);

        // ── Assign statement ──────────────────────────────────────────────────

        [Test] public void AssignStmt_StoresTargetOpValue()
        {
            var target = Name("x");
            var value  = Int("5");
            var op     = T(TokenType.Equal, "=");
            var node   = new AssignStmt(target, op, value);

            Assert.That(node.Target, Is.SameAs(target));
            Assert.That(node.Value,  Is.SameAs(value));
            Assert.That(node.Op.Type, Is.EqualTo(TokenType.Equal));
        }

        [Test] public void AssignStmt_AugmentedOp_Plus()
        {
            var node = new AssignStmt(Name("x"), T(TokenType.PlusEqual, "+="), Int("1"));
            Assert.That(node.Op.Type, Is.EqualTo(TokenType.PlusEqual));
        }

        // ── Expression statement ───────────────────────────────────────────────

        [Test] public void ExprStmt_WrapsExpression()
        {
            var expr = new CallExpr(Name("foo"), new List<Argument>(), 1, 1);
            var node = new ExprStmt(expr);
            Assert.That(node.Expression, Is.SameAs(expr));
        }

        // ── Return statement ──────────────────────────────────────────────────

        [Test] public void ReturnStmt_WithValue()
        {
            var value = Int("42");
            var node  = new ReturnStmt(T(TokenType.Return, "return"), value);
            Assert.That(node.Value, Is.SameAs(value));
        }

        [Test] public void ReturnStmt_NoValue_IsNull()
        {
            var node = new ReturnStmt(T(TokenType.Return, "return"), value: null);
            Assert.That(node.Value, Is.Null);
        }

        // ── Break / Continue ──────────────────────────────────────────────────

        [Test] public void BreakStmt_HasCorrectPosition()
        {
            var tok  = T(TokenType.Break, "break", ln: 5, col: 9);
            var node = new BreakStmt(tok);
            Assert.That(node.Line,   Is.EqualTo(5));
            Assert.That(node.Column, Is.EqualTo(9));
        }

        [Test] public void ContinueStmt_HasCorrectPosition()
        {
            var tok  = T(TokenType.Continue, "continue", ln: 7, col: 1);
            var node = new ContinueStmt(tok);
            Assert.That(node.Line, Is.EqualTo(7));
        }

        // ── Yield ─────────────────────────────────────────────────────────────

        [Test] public void YieldStmt_StoresValue()
        {
            var value = Int("1");
            var node  = new YieldStmt(T(TokenType.Yield, "yield"), value);
            Assert.That(node.Value, Is.SameAs(value));
        }

        // ── Wait ──────────────────────────────────────────────────────────────

        [Test] public void WaitStmt_StoresTicks()
        {
            var ticks = Int("5");
            var node  = new WaitStmt(T(TokenType.Wait, "wait"), ticks);
            Assert.That(node.Ticks, Is.SameAs(ticks));
        }

        // ── If statement ──────────────────────────────────────────────────────

        [Test] public void IfStmt_NoElif_NoElse()
        {
            var cond = Name("x");
            var body = Block(new ExprStmt(new CallExpr(Name("foo"), new List<Argument>(), 1, 1)));
            var node = new IfStmt(cond, body,
                                  elifClauses: new List<ElifClause>(),
                                  elseBody: null,
                                  line: 1, col: 1);

            Assert.That(node.Condition,    Is.SameAs(cond));
            Assert.That(node.ThenBody.Count, Is.EqualTo(1));
            Assert.That(node.ElifClauses.Count, Is.EqualTo(0));
            Assert.That(node.ElseBody,     Is.Null);
        }

        [Test] public void IfStmt_WithElif_AndElse()
        {
            var elif = new ElifClause(condition: Name("y"),
                                      body: Block(new ExprStmt(Name("z"))));
            var node = new IfStmt(Name("a"), Block(new ExprStmt(Name("b"))),
                                  new List<ElifClause> { elif },
                                  elseBody: Block(new ExprStmt(Name("c"))),
                                  line: 1, col: 1);

            Assert.That(node.ElifClauses.Count, Is.EqualTo(1));
            Assert.That(node.ElseBody,          Is.Not.Null);
        }

        // ── For statement ──────────────────────────────────────────────────────

        [Test] public void ForStmt_StoresTargetsAndIterable()
        {
            var targets  = new List<string> { "item" };
            var iterable = Name("collection");
            var body     = Block(new ExprStmt(Name("item")));
            var node     = new ForStmt(targets, iterable, body,
                                       elseBody: null, line: 1, col: 1);

            Assert.That(node.Targets[0],  Is.EqualTo("item"));
            Assert.That(node.Iterable,    Is.SameAs(iterable));
            Assert.That(node.Body.Count,  Is.EqualTo(1));
        }

        [Test] public void ForStmt_MultipleTargets_Destructuring()
        {
            var targets  = new List<string> { "k", "v" };
            var iterable = new CallExpr(
                new AttributeExpr(Name("data"), IdTok("entries")),
                new List<Argument>(), 1, 1);
            var node = new ForStmt(targets, iterable, Block(), null, 1, 1);
            Assert.That(node.Targets.Count, Is.EqualTo(2));
        }

        // ── While loop ────────────────────────────────────────────────────────

        [Test] public void WhileStmt_StoresConditionAndBody()
        {
            var cond = Name("alive");
            var body = Block(new ExprStmt(new CallExpr(Name("tick"), new List<Argument>(), 1, 1)));
            var node = new WhileStmt(cond, body, line: 1, col: 1);

            Assert.That(node.Condition, Is.SameAs(cond));
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }

        // ── Loop (infinite) ───────────────────────────────────────────────────

        [Test] public void LoopStmt_StoresBody()
        {
            var body = Block(new ExprStmt(Name("tick")));
            var node = new LoopStmt(body, line: 1, col: 1);
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }

        // ── Match statement ────────────────────────────────────────────────────

        [Test] public void MatchStmt_StoresSubjectAndCases()
        {
            var subject = Name("x");
            var clause  = new CaseClause(
                pattern: new StringLiteralExpr(T(TokenType.String, "loam")),
                guard:   null,
                body:    Block(new ExprStmt(Name("grow"))));

            var node = new MatchStmt(subject, new List<CaseClause> { clause }, line: 1, col: 1);

            Assert.That(node.Subject,    Is.SameAs(subject));
            Assert.That(node.Cases.Count, Is.EqualTo(1));
        }

        // ── Try / Recover ──────────────────────────────────────────────────────

        [Test] public void TryStmt_StoresAllThreeBlocks()
        {
            var tryBody     = Block(new ExprStmt(Name("risky")));
            var recoverBody = Block(new ExprStmt(Name("handle")));
            var alwaysBody  = Block(new ExprStmt(Name("cleanup")));
            var errName     = IdTok("err");

            var node = new TryStmt(tryBody, errName, recoverBody, alwaysBody, line: 1, col: 1);

            Assert.That(node.TryBody.Count,     Is.EqualTo(1));
            Assert.That(node.ErrorName,         Is.EqualTo("err"));
            Assert.That(node.RecoverBody.Count, Is.EqualTo(1));
            Assert.That(node.AlwaysBody,        Is.Not.Null);
        }

        [Test] public void TryStmt_AlwaysIsOptional()
        {
            var node = new TryStmt(Block(), IdTok("e"), Block(), alwaysBody: null, line: 1, col: 1);
            Assert.That(node.AlwaysBody, Is.Null);
        }

        // ── Function declaration ───────────────────────────────────────────────

        [Test] public void FnDecl_StoresNameParamsBody()
        {
            var name   = IdTok("grow_root");
            var param  = new Param(IdTok("depth"));
            var body   = Block(new ExprStmt(Name("root")));
            var node   = new FnDecl(name,
                                    new List<Param> { param },
                                    returnType: null,
                                    body: body,
                                    decorators: new List<Decorator>(),
                                    line: 1, col: 1);

            Assert.That(node.Name,         Is.EqualTo("grow_root"));
            Assert.That(node.Params.Count, Is.EqualTo(1));
            Assert.That(node.Body.Count,   Is.EqualTo(1));
        }

        [Test] public void FnDecl_WithDecorator()
        {
            var dec  = new Decorator(IdTok("memoize"), new List<GrowlNode>());
            var node = new FnDecl(IdTok("f"), new List<Param>(), null,
                                  Block(), new List<Decorator> { dec }, 1, 1);
            Assert.That(node.Decorators.Count, Is.EqualTo(1));
            Assert.That(node.Decorators[0].Name, Is.EqualTo("memoize"));
        }

        [Test] public void Param_DefaultValue_IsStored()
        {
            var param = new Param(IdTok("depth"), typeAnnotation: null,
                                  defaultValue: Int("3"), isVariadic: false, isKeyword: false);
            Assert.That(param.DefaultValue, Is.Not.Null);
        }

        // ── Class declaration ──────────────────────────────────────────────────

        [Test] public void ClassDecl_StoresNameAndMembers()
        {
            var method = new FnDecl(IdTok("tick"), new List<Param>(), null,
                                    Block(), new List<Decorator>(), 2, 5);
            var node   = new ClassDecl(
                name:       IdTok("GrowthStrategy"),
                superclass: null,
                traits:     new List<TypeRef>(),
                mixins:     new List<TypeRef>(),
                members:    new List<GrowlNode> { method },
                isAbstract: false,
                decorators: new List<Decorator>(),
                line: 1, col: 1);

            Assert.That(node.Name,           Is.EqualTo("GrowthStrategy"));
            Assert.That(node.Members.Count,  Is.EqualTo(1));
            Assert.That(node.IsAbstract,     Is.False);
        }

        // ── Const declaration ──────────────────────────────────────────────────

        [Test] public void ConstDecl_StoresNameAndValue()
        {
            var node = new ConstDecl(IdTok("MAX_HEIGHT"), typeAnnotation: null,
                                     value: Int("100"), line: 1, col: 1);
            Assert.That(node.Name,  Is.EqualTo("MAX_HEIGHT"));
            Assert.That(node.Value, Is.Not.Null);
        }

        // ── Import statement ───────────────────────────────────────────────────

        [Test] public void ImportStmt_ModuleImport()
        {
            var node = new ImportStmt(
                modulePath: new List<string> { "drought" },
                alias: null,
                names: null,
                line: 1, col: 1);

            Assert.That(node.ModulePath[0], Is.EqualTo("drought"));
            Assert.That(node.Alias,         Is.Null);
        }

        [Test] public void ImportStmt_FromImport_WithNames()
        {
            var node = new ImportStmt(
                modulePath: new List<string> { "drought" },
                alias: null,
                names: new List<string> { "DroughtStrategy", "CRITICAL_WATER" },
                line: 1, col: 1);

            Assert.That(node.Names.Count, Is.EqualTo(2));
        }

        // ── Program (root) ────────────────────────────────────────────────────

        [Test] public void ProgramNode_StoresStatements()
        {
            var stmts = new List<GrowlNode>
            {
                new ConstDecl(IdTok("X"), null, Int("1"), 1, 1),
                new ExprStmt(Name("foo")),
            };
            var node = new ProgramNode(stmts);
            Assert.That(node.Statements.Count, Is.EqualTo(2));
        }
    }
}
