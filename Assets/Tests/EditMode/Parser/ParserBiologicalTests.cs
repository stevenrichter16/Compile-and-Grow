using NUnit.Framework;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Tests.Parser
{
    [TestFixture]
    public class ParserBiologicalTests
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
        public void Phase_WithAgeRange_Parses()
        {
            string src =
                "phase \"sprout\"(0, 10):\n" +
                "    grow_root()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as PhaseBlock;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.PhaseName, Is.EqualTo("sprout"));
            Assert.That(node.MinAge, Is.TypeOf<IntegerLiteralExpr>());
            Assert.That(node.MaxAge, Is.TypeOf<IntegerLiteralExpr>());
            Assert.That(node.Condition, Is.Null);
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }

        [Test]
        public void Phase_WhenCondition_Parses()
        {
            string src =
                "phase \"fruit\" when org.maturity > 0.8:\n" +
                "    start_fruiting()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as PhaseBlock;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.PhaseName, Is.EqualTo("fruit"));
            Assert.That(node.MinAge, Is.Null);
            Assert.That(node.MaxAge, Is.Null);
            Assert.That(node.Condition, Is.TypeOf<BinaryExpr>());
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }

        [Test]
        public void WhenThenBlock_ParsesNestedThenWhen()
        {
            string src =
                "when org.water < 0.2:\n" +
                "    root.absorb(\"water\")\n" +
                "then when org.energy < 10:\n" +
                "    photo.retrieve_energy(5)\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as WhenBlock;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Condition, Is.TypeOf<BinaryExpr>());
            Assert.That(node.Body.Count, Is.EqualTo(1));
            Assert.That(node.ThenBlock, Is.Not.Null);
            Assert.That(node.ThenBlock.Count, Is.EqualTo(1));
            Assert.That(node.ThenBlock[0], Is.TypeOf<WhenBlock>());
        }

        [Test]
        public void RespondBlock_WithBinding_Parses()
        {
            string src =
                "respond to \"depot:lighting.override\" as cmd:\n" +
                "    log(cmd)\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as RespondBlock;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.EventName, Is.EqualTo("depot:lighting.override"));
            Assert.That(node.Binding, Is.EqualTo("cmd"));
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }

        [Test]
        public void RespondBlock_WithoutBinding_Parses()
        {
            string src =
                "respond to \"harvest.complete\":\n" +
                "    route()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as RespondBlock;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.EventName, Is.EqualTo("harvest.complete"));
            Assert.That(node.Binding, Is.Null);
        }

        [Test]
        public void AdaptBlock_ParsesRulesAndRate()
        {
            string src =
                "adapt org.water:\n" +
                "    toward 0.8 when org.energy > 10\n" +
                "    toward 0.4 otherwise\n" +
                "    rate 0.1\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as AdaptBlock;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Subject, Is.TypeOf<AttributeExpr>());
            Assert.That(node.Rules.Count, Is.EqualTo(2));
            Assert.That(node.Rules[0].Action, Is.TypeOf<FloatLiteralExpr>());
            Assert.That(node.Rules[0].Condition, Is.TypeOf<BinaryExpr>());
            Assert.That(node.Rules[1].Condition, Is.TypeOf<NoneLiteralExpr>());
            Assert.That(node.Budget, Is.TypeOf<FloatLiteralExpr>());
        }

        [Test]
        public void CycleBlock_ParsesPoints()
        {
            string src =
                "cycle \"seasonal\" period 100:\n" +
                "    at 0:\n" +
                "        spring()\n" +
                "    at 50:\n" +
                "        summer()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as CycleBlock;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("seasonal"));
            Assert.That(node.Period, Is.TypeOf<IntegerLiteralExpr>());
            Assert.That(node.Points.Count, Is.EqualTo(2));
            Assert.That(node.Points[0].At, Is.TypeOf<IntegerLiteralExpr>());
            Assert.That(node.Points[0].Body.Count, Is.EqualTo(1));
        }

        [Test]
        public void TickerDecl_ParsesNameIntervalAndBody()
        {
            string src =
                "ticker \"heartbeat\" every 10 ticks:\n" +
                "    pulse()\n";

            var program = ParseProgramNoErrors(src);
            var node = program.Statements[0] as TickerDecl;

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Name, Is.EqualTo("heartbeat"));
            Assert.That(node.Interval, Is.TypeOf<IntegerLiteralExpr>());
            Assert.That(node.Body.Count, Is.EqualTo(1));
        }
    }
}
