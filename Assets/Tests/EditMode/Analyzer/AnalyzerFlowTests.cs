using NUnit.Framework;
using System.Linq;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerFlowTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        private static int CountUnresolved(AnalyzeResult result, string name)
            => result.Errors.Count(e =>
                e.Code == AnalyzeErrorCode.UnresolvedName &&
                e.Message.Contains("'" + name + "'"));

        [Test]
        public void ForLoop_Targets_BindInsideLoop_AndDoNotLeakOutside()
        {
            string src =
                "fn f(items):\n" +
                "    for i, x in items:\n" +
                "        a = x\n" +
                "    b = i\n" +
                "    return b\n";

            var result = Analyze(src);

            Assert.That(CountUnresolved(result, "x"), Is.EqualTo(0), "x should resolve inside loop body.");
            Assert.That(CountUnresolved(result, "i"), Is.EqualTo(1), "i should be unresolved outside loop scope.");
        }

        [Test]
        public void MatchTuplePattern_BindsPatternVariablesInCaseBody_ButNotAfterMatch()
        {
            string src =
                "fn f(pair):\n" +
                "    match pair:\n" +
                "        case (w, e):\n" +
                "            a = w\n" +
                "        case _:\n" +
                "            a = 0\n" +
                "    b = w\n" +
                "    return b\n";

            var result = Analyze(src);

            // Desired behavior:
            // - w/e are bound for this case body
            // - w is unresolved after match (single unresolved occurrence)
            Assert.That(CountUnresolved(result, "e"), Is.EqualTo(0), "Pattern variable e should resolve in case scope.");
            Assert.That(CountUnresolved(result, "w"), Is.EqualTo(1), "w should be unresolved only after the match.");
        }

        [Test]
        public void MatchDictPattern_BindsPatternVariablesInCaseBody_ButNotAfterMatch()
        {
            string src =
                "fn f(scan_result):\n" +
                "    match scan_result:\n" +
                "        case {\"type\": kind, \"distance\": d}:\n" +
                "            a = d\n" +
                "        case _:\n" +
                "            a = 0\n" +
                "    return d\n";

            var result = Analyze(src);

            Assert.That(CountUnresolved(result, "kind"), Is.EqualTo(0), "Pattern variable kind should resolve in case scope.");
            Assert.That(CountUnresolved(result, "d"), Is.EqualTo(1), "d should be unresolved only after the match.");
        }

        [Test]
        public void RecoverBinding_IsRecoverScopeOnly()
        {
            string src =
                "fn risky():\n" +
                "    return none\n" +
                "\n" +
                "fn f():\n" +
                "    try:\n" +
                "        risky()\n" +
                "    recover err:\n" +
                "        a = err\n" +
                "    always:\n" +
                "        b = err\n" +
                "    c = err\n" +
                "    return c\n";

            var result = Analyze(src);

            Assert.That(CountUnresolved(result, "err"), Is.EqualTo(2),
                "err should resolve in recover block, but not in always or after try.");
        }

        [Test]
        public void RespondBinding_IsRespondScopeOnly()
        {
            string src =
                "fn handle():\n" +
                "    respond to \"ev\" as cmd:\n" +
                "        x = cmd\n" +
                "    y = cmd\n" +
                "    return y\n";

            var result = Analyze(src);

            Assert.That(CountUnresolved(result, "cmd"), Is.EqualTo(1),
                "cmd should resolve inside respond body and be unresolved outside.");
        }

        [Test]
        public void DuplicateFunctionParameterNames_ReportDuplicateSymbol()
        {
            string src =
                "fn f(x, x):\n" +
                "    return x\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.DuplicateSymbol &&
                e.Message.Contains("'x'")), Is.True);
        }
    }
}
