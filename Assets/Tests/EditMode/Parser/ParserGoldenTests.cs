using NUnit.Framework;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Tests.Parser
{
    [TestFixture]
    public class ParserGoldenTests
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
        public void Golden_GrowthControlPipeline_Parses()
        {
            string src =
                "const max_energy: float = 100.0\n" +
                "type Energy = float\n" +
                "\n" +
                "fn manage(field):\n" +
                "    status = scan(field)\n" +
                "    if status.state == \"empty\":\n" +
                "        plant(field, \"wheat\")\n" +
                "    elif status.state == \"ready\":\n" +
                "        result = harvest(field)\n" +
                "        depot.emit(\"harvest.complete\", result)\n" +
                "    else:\n" +
                "        wait(1)\n" +
                "\n" +
                "match env.soil.type:\n" +
                "    case \"loam\":\n" +
                "        root.grow_wide(4)\n" +
                "    case _:\n" +
                "        root.grow_down(3)\n";

            var program = ParseProgramNoErrors(src);

            Assert.That(program.Statements.Count, Is.EqualTo(4));
            Assert.That(program.Statements[0], Is.TypeOf<ConstDecl>());
            Assert.That(program.Statements[1], Is.TypeOf<TypeAliasDecl>());
            Assert.That(program.Statements[2], Is.TypeOf<FnDecl>());
            Assert.That(program.Statements[3], Is.TypeOf<MatchStmt>());

            var fn = (FnDecl)program.Statements[2];
            Assert.That(fn.Body.OfType<IfStmt>().Count(), Is.EqualTo(1));

            var match = (MatchStmt)program.Statements[3];
            Assert.That(match.Cases.Count, Is.EqualTo(2));
        }

        [Test]
        public void Golden_BiologicalRuntimeBlocks_Parses()
        {
            string src =
                "phase \"sprout\"(0, 20):\n" +
                "    grow_root()\n" +
                "\n" +
                "phase \"fruit\" when org.maturity > 0.8:\n" +
                "    start_fruiting()\n" +
                "\n" +
                "when org.water < 0.2:\n" +
                "    root.absorb(\"water\")\n" +
                "then when org.energy < 10:\n" +
                "    photo.retrieve_energy(5)\n" +
                "\n" +
                "respond to \"depot:lighting.override\" as cmd:\n" +
                "    match cmd.action:\n" +
                "        case \"bright\":\n" +
                "            set_light(1.0)\n" +
                "        case _:\n" +
                "            set_light(0.5)\n" +
                "\n" +
                "adapt org.water:\n" +
                "    toward 0.8 when org.energy > 10\n" +
                "    toward 0.4 otherwise\n" +
                "    rate 0.1\n" +
                "\n" +
                "cycle \"seasonal\" period 100:\n" +
                "    at 0:\n" +
                "        spring()\n" +
                "    at 50:\n" +
                "        summer()\n" +
                "\n" +
                "ticker \"heartbeat\" every 10 ticks:\n" +
                "    depot.emit(\"heartbeat\", TICK)\n";

            var program = ParseProgramNoErrors(src);

            Assert.That(program.Statements.Count, Is.EqualTo(7));
            Assert.That(program.Statements[0], Is.TypeOf<PhaseBlock>());
            Assert.That(program.Statements[1], Is.TypeOf<PhaseBlock>());
            Assert.That(program.Statements[2], Is.TypeOf<WhenBlock>());
            Assert.That(program.Statements[3], Is.TypeOf<RespondBlock>());
            Assert.That(program.Statements[4], Is.TypeOf<AdaptBlock>());
            Assert.That(program.Statements[5], Is.TypeOf<CycleBlock>());
            Assert.That(program.Statements[6], Is.TypeOf<TickerDecl>());

            var when = (WhenBlock)program.Statements[2];
            Assert.That(when.ThenBlock, Is.Not.Null);
            Assert.That(when.ThenBlock.Count, Is.EqualTo(1));
            Assert.That(when.ThenBlock[0], Is.TypeOf<WhenBlock>());

            var adapt = (AdaptBlock)program.Statements[4];
            Assert.That(adapt.Rules.Count, Is.EqualTo(2));
            Assert.That(adapt.Budget, Is.Not.Null);
        }

        [Test]
        public void Golden_ResilienceFunction_ParsesNestedErrorAndTimingFlow()
        {
            string src =
                "fn resilience(org):\n" +
                "    try:\n" +
                "        risky_operation()\n" +
                "    recover err:\n" +
                "        match err:\n" +
                "            case \"blocked\":\n" +
                "                reroute()\n" +
                "            case _:\n" +
                "                warn(err)\n" +
                "    always:\n" +
                "        cleanup()\n" +
                "\n" +
                "    defer 10 ticks:\n" +
                "        check_growth()\n" +
                "\n" +
                "    defer until org.maturity > 0.5:\n" +
                "        start_fruiting()\n" +
                "\n" +
                "    mutate stem.rigidity by random(-0.1, 0.1) every 5 ticks\n" +
                "    return none\n";

            var program = ParseProgramNoErrors(src);
            Assert.That(program.Statements.Count, Is.EqualTo(1));
            Assert.That(program.Statements[0], Is.TypeOf<FnDecl>());

            var fn = (FnDecl)program.Statements[0];
            Assert.That(fn.Body.Count, Is.EqualTo(5));
            Assert.That(fn.Body[0], Is.TypeOf<TryStmt>());
            Assert.That(fn.Body[1], Is.TypeOf<DeferStmt>());
            Assert.That(fn.Body[2], Is.TypeOf<DeferStmt>());
            Assert.That(fn.Body[3], Is.TypeOf<MutateStmt>());
            Assert.That(fn.Body[4], Is.TypeOf<ReturnStmt>());

            var tryStmt = (TryStmt)fn.Body[0];
            Assert.That(tryStmt.TryBody.Count, Is.EqualTo(1));
            Assert.That(tryStmt.RecoverBody.Count, Is.EqualTo(1));
            Assert.That(tryStmt.AlwaysBody.Count, Is.EqualTo(1));
            Assert.That(tryStmt.RecoverBody[0], Is.TypeOf<MatchStmt>());
        }

        [Test]
        public void Golden_MixedDeclarations_Parses()
        {
            string src =
                "@sealed\n" +
                "abstract class RootSystem:\n" +
                "    fn tick(self):\n" +
                "        return none\n" +
                "\n" +
                "struct Genome:\n" +
                "    slots: int\n" +
                "    energy: float = 100.0\n" +
                "    fn cap(self):\n" +
                "        return self.energy\n" +
                "\n" +
                "enum Mode:\n" +
                "    IDLE\n" +
                "    ACTIVE = 2\n" +
                "    fn label(self):\n" +
                "        return \"mode\"\n" +
                "\n" +
                "trait GrowthPolicy:\n" +
                "    fn tick(self):\n" +
                "        return none\n" +
                "\n" +
                "mixin HydrationMixin:\n" +
                "    fn hydrate(self):\n" +
                "        return none\n";

            var program = ParseProgramNoErrors(src);

            Assert.That(program.Statements.Count, Is.EqualTo(5));
            Assert.That(program.Statements[0], Is.TypeOf<ClassDecl>());
            Assert.That(program.Statements[1], Is.TypeOf<StructDecl>());
            Assert.That(program.Statements[2], Is.TypeOf<EnumDecl>());
            Assert.That(program.Statements[3], Is.TypeOf<TraitDecl>());
            Assert.That(program.Statements[4], Is.TypeOf<MixinDecl>());

            var cls = (ClassDecl)program.Statements[0];
            Assert.That(cls.IsAbstract, Is.True);
            Assert.That(cls.Decorators.Count, Is.EqualTo(1));
            Assert.That(cls.Decorators[0].Name, Is.EqualTo("sealed"));
        }
    }
}
