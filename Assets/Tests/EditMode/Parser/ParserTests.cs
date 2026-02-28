using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Tests.Parser
{
    [TestFixture]
    public class ParserTests
    {
        private static ParseResult Parse(string src) => GrowlLanguage.Parser.Parser.Parse(src);

        private static ProgramNode ParseProgramNoErrors(string src)
        {
            var result = Parse(src);
            Assert.That(
                result.HasErrors,
                Is.False,
                "Parser returned errors:\n" + string.Join("\n", result.Errors.Select(e => e.ToString())));
            return result.Program;
        }

        [Test]
        public void ExpressionStatement_IntegerLiteral()
        {
            var program = ParseProgramNoErrors("42\n");

            Assert.That(program.Statements.Count, Is.EqualTo(1));
            var stmt = program.Statements[0] as ExprStmt;
            Assert.That(stmt, Is.Not.Null);

            var lit = stmt.Expression as IntegerLiteralExpr;
            Assert.That(lit, Is.Not.Null);
            Assert.That(lit.Value, Is.EqualTo(42));
        }

        [Test]
        public void Expression_Precedence_MultiplicationBindsTighterThanAddition()
        {
            var program = ParseProgramNoErrors("1 + 2 * 3\n");
            var stmt = (ExprStmt)program.Statements[0];
            var expr = stmt.Expression as BinaryExpr;

            Assert.That(expr, Is.Not.Null);
            Assert.That(expr.Op.Type, Is.EqualTo(GrowlLanguage.Lexer.TokenType.Plus));

            var right = expr.Right as BinaryExpr;
            Assert.That(right, Is.Not.Null);
            Assert.That(right.Op.Type, Is.EqualTo(GrowlLanguage.Lexer.TokenType.Star));
        }

        [Test]
        public void Assignment_ParsesNameAndValue()
        {
            var program = ParseProgramNoErrors("x = 5\n");
            var assign = program.Statements[0] as AssignStmt;

            Assert.That(assign, Is.Not.Null);
            Assert.That(assign.Op.Type, Is.EqualTo(GrowlLanguage.Lexer.TokenType.Equal));
            Assert.That(((NameExpr)assign.Target).Name, Is.EqualTo("x"));
            Assert.That(((IntegerLiteralExpr)assign.Value).Value, Is.EqualTo(5));
        }

        [Test]
        public void FunctionDeclaration_ParsesParamsAndReturnStatement()
        {
            string src =
                "fn grow(depth):\n" +
                "    return depth\n";

            var program = ParseProgramNoErrors(src);
            var fn = program.Statements[0] as FnDecl;

            Assert.That(fn, Is.Not.Null);
            Assert.That(fn.Name, Is.EqualTo("grow"));
            Assert.That(fn.Params.Count, Is.EqualTo(1));
            Assert.That(fn.Params[0].Name, Is.EqualTo("depth"));
            Assert.That(fn.Body.Count, Is.EqualTo(1));
            Assert.That(fn.Body[0], Is.TypeOf<ReturnStmt>());
        }

        [Test]
        public void IfElifElse_ParsesAllBranches()
        {
            string src =
                "if ready:\n" +
                "    run()\n" +
                "elif fallback:\n" +
                "    fallback_action()\n" +
                "else:\n" +
                "    idle()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as IfStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.ThenBody.Count, Is.EqualTo(1));
            Assert.That(node.ElifClauses.Count, Is.EqualTo(1));
            Assert.That(node.ElseBody.Count, Is.EqualTo(1));
        }

        [Test]
        public void Decorator_AttachesToFollowingFunction()
        {
            string src =
                "@memoize\n" +
                "fn score():\n" +
                "    return 1\n";

            var program = ParseProgramNoErrors(src);
            var fn = program.Statements[0] as FnDecl;

            Assert.That(fn, Is.Not.Null);
            Assert.That(fn.Decorators.Count, Is.EqualTo(1));
            Assert.That(fn.Decorators[0].Name, Is.EqualTo("memoize"));
        }

        [Test]
        public void RoleDecorator_FollowedByFunction_ParsesGeneDecl()
        {
            string src =
                "@role(\"intake\")\n" +
                "fn intake(org, env):\n" +
                "    return none\n";

            var program = ParseProgramNoErrors(src);
            var gene = program.Statements[0] as GeneDecl;

            Assert.That(gene, Is.Not.Null);
            Assert.That(gene.IsRole, Is.True);
            Assert.That(gene.RoleName, Is.EqualTo("intake"));
            Assert.That(gene.Fn.Name, Is.EqualTo("intake"));
            Assert.That(gene.Fn.Params.Count, Is.EqualTo(2));
        }

        [Test]
        public void CallExpression_ParsesNamedArgument()
        {
            var program = ParseProgramNoErrors("grow(depth: 2)\n");
            var stmt = (ExprStmt)program.Statements[0];
            var call = stmt.Expression as CallExpr;

            Assert.That(call, Is.Not.Null);
            Assert.That(call.Args.Count, Is.EqualTo(1));
            Assert.That(call.Args[0].Name, Is.EqualTo("depth"));
            Assert.That(call.Args[0].Value, Is.TypeOf<IntegerLiteralExpr>());
        }

        [Test]
        public void InvalidSyntax_ReportsError()
        {
            var result = Parse("fn broken(:\n");

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Errors.Count, Is.GreaterThan(0));
        }
    }
}
