using NUnit.Framework;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Parser;

namespace GrowlLanguage.Tests.Parser
{
    [TestFixture]
    public class ParserErrorRecoveryTests
    {
        private static ParseResult Parse(string src) => GrowlLanguage.Parser.Parser.Parse(src);

        private static bool ContainsCallTo(ParseResult result, string name)
            => result.Program.Statements
                .OfType<ExprStmt>()
                .Select(s => s.Expression as CallExpr)
                .Where(c => c != null)
                .Any(c => c.Callee is NameExpr n && n.Name == name);

        [Test]
        public void MissingColonInIf_ReportsSingleError_AndRecoversToNextTopLevelStatement()
        {
            string src =
                "if ready\n" +
                "    run()\n" +
                "grow()\n";

            var result = Parse(src);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Expected ':' after if condition.")), Is.True);
            Assert.That(ContainsCallTo(result, "grow"), Is.True, "Expected parser to recover and parse grow().");
            Assert.That(result.Errors.Count, Is.LessThanOrEqualTo(2), "Expected minimal error cascade.");
        }

        [Test]
        public void MissingByInMutate_ReportsSingleError_AndRecoversToNextStatement()
        {
            string src =
                "mutate stem.rigidity random(0.1)\n" +
                "pulse()\n";

            var result = Parse(src);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Expected 'by' in mutate statement.")), Is.True);
            Assert.That(ContainsCallTo(result, "pulse"), Is.True, "Expected parser to recover and parse pulse().");
            Assert.That(result.Errors.Count, Is.EqualTo(1), "Expected exactly one parser diagnostic.");
        }

        [Test]
        public void BrokenCaseHeader_RecoversWithinMatch_AndKeepsLaterCases()
        {
            string src =
                "match x:\n" +
                "    case 1\n" +
                "        a()\n" +
                "    case 2:\n" +
                "        b()\n";

            var result = Parse(src);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Expected ':' after case pattern.")), Is.True);

            var match = result.Program.Statements.OfType<MatchStmt>().FirstOrDefault();
            Assert.That(match, Is.Not.Null, "Expected parser to keep a partial match node.");
            Assert.That(match.Cases.Count, Is.EqualTo(1), "Expected parser to keep valid trailing case.");
            Assert.That(match.Cases[0].Pattern, Is.TypeOf<IntegerLiteralExpr>());
            Assert.That(((IntegerLiteralExpr)match.Cases[0].Pattern).Value, Is.EqualTo(2));
        }

        [Test]
        public void TryWithoutRecover_RecoversToFollowingStatement_WithoutCascade()
        {
            string src =
                "try:\n" +
                "    risky()\n" +
                "always:\n" +
                "    cleanup()\n" +
                "pulse()\n";

            var result = Parse(src);

            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Errors.Any(e => e.Message.Contains("Expected 'recover' after try block.")), Is.True);
            Assert.That(ContainsCallTo(result, "pulse"), Is.True, "Expected parser to recover and parse pulse().");
            Assert.That(result.Errors.Count, Is.EqualTo(1), "Expected exactly one parser diagnostic.");
        }
    }
}
