using NUnit.Framework;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Tests.Parser
{
    [TestFixture]
    public class ParserControlTests
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
        public void MatchStatement_ParsesCasesAndGuard()
        {
            string src =
                "match env.soil.type:\n" +
                "    case \"loam\":\n" +
                "        root.grow_wide(4)\n" +
                "    case \"sand\" if dry:\n" +
                "        root.grow_down(6)\n" +
                "    case _:\n" +
                "        root.grow_down(3)\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as MatchStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Subject, Is.Not.Null);
            Assert.That(node.Cases.Count, Is.EqualTo(3));
            Assert.That(node.Cases[0].Pattern, Is.TypeOf<StringLiteralExpr>());
            Assert.That(node.Cases[1].Guard, Is.TypeOf<NameExpr>());
            Assert.That(node.Cases[2].Pattern, Is.TypeOf<NameExpr>());
            Assert.That(((NameExpr)node.Cases[2].Pattern).Name, Is.EqualTo("_"));
        }

        [Test]
        public void TryRecoverAlways_ParsesAllBlocks()
        {
            string src =
                "try:\n" +
                "    risky()\n" +
                "recover err:\n" +
                "    heal(err)\n" +
                "always:\n" +
                "    cleanup()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as TryStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.ErrorName, Is.EqualTo("err"));
            Assert.That(node.TryBody.Count, Is.EqualTo(1));
            Assert.That(node.RecoverBody.Count, Is.EqualTo(1));
            Assert.That(node.AlwaysBody.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryRecover_WithoutAlways_Parses()
        {
            string src =
                "try:\n" +
                "    risky()\n" +
                "recover err:\n" +
                "    heal(err)\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as TryStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.ErrorName, Is.EqualTo("err"));
            Assert.That(node.AlwaysBody, Is.Null);
        }

        [Test]
        public void DeferTicks_ParsesDurationAndBody()
        {
            string src =
                "defer 10 ticks:\n" +
                "    log(\"later\")\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as DeferStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.IsUntil, Is.False);
            Assert.That(node.Duration, Is.Not.Null);
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }

        [Test]
        public void DeferUntil_ParsesConditionForm()
        {
            string src =
                "defer until org.maturity > 0.5:\n" +
                "    start_fruiting()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as DeferStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.IsUntil, Is.True);
            Assert.That(node.Duration, Is.TypeOf<BinaryExpr>());
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }

        [Test]
        public void Mutate_ParsesTargetValueAndInterval()
        {
            string src = "mutate stem.rigidity by random(-0.1, 0.1) every 5\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as MutateStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Target, Is.TypeOf<AttributeExpr>());
            Assert.That(node.Value, Is.Not.Null);
            Assert.That(node.Interval, Is.Not.Null);
        }

        [Test]
        public void Mutate_WithoutInterval_Parses()
        {
            string src = "mutate org.morphology.color by noise(TICK * 0.1) * 0.2\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as MutateStmt;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Target, Is.TypeOf<AttributeExpr>());
            Assert.That(node.Interval, Is.Null);
        }
    }
}
