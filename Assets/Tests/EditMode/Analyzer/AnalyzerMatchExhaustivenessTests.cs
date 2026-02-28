using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerMatchExhaustivenessTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void Match_MissingDefaultCase_ReportsDiagnostic()
        {
            string src =
                "fn f(x: int) -> int:\n" +
                "    match x:\n" +
                "        case 1:\n" +
                "            return 1\n" +
                "        case 2:\n" +
                "            return 2\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.NonExhaustiveMatch &&
                e.Message.ToLowerInvariant().Contains("match") &&
                e.Message.ToLowerInvariant().Contains("exhaust")), Is.True);
        }

        [Test]
        public void Match_CaseAfterCatchAll_IsUnreachable_ReportsDiagnostic()
        {
            string src =
                "fn f(x: int) -> int:\n" +
                "    match x:\n" +
                "        case _:\n" +
                "            return 0\n" +
                "        case 1:\n" +
                "            return 1\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.UnreachableCase &&
                e.Message.ToLowerInvariant().Contains("unreachable") &&
                e.Message.ToLowerInvariant().Contains("case")), Is.True);
        }

        [Test]
        public void Match_DuplicateLiteralCase_ReportsDiagnostic()
        {
            string src =
                "fn f(x: int) -> int:\n" +
                "    match x:\n" +
                "        case 1:\n" +
                "            return 1\n" +
                "        case 1:\n" +
                "            return 2\n" +
                "        case _:\n" +
                "            return 0\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.DuplicateMatchCase &&
                e.Message.ToLowerInvariant().Contains("duplicate") &&
                e.Message.ToLowerInvariant().Contains("case")), Is.True);
        }
    }
}
