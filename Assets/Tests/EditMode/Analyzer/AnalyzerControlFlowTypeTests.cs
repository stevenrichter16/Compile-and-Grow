using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerControlFlowTypeTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void IfCondition_NonBoolCondition_ReportsTypeMismatch()
        {
            string src =
                "fn f():\n" +
                "    if 1:\n" +
                "        return 1\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("if") &&
                e.Message.ToLowerInvariant().Contains("condition")), Is.True);
        }

        [Test]
        public void WhileCondition_NonBoolCondition_ReportsTypeMismatch()
        {
            string src =
                "fn f():\n" +
                "    while \"loop\":\n" +
                "        break\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("while") &&
                e.Message.ToLowerInvariant().Contains("condition")), Is.True);
        }

        [Test]
        public void MatchGuard_NonBoolGuard_ReportsTypeMismatch()
        {
            string src =
                "fn f(x):\n" +
                "    match x:\n" +
                "        case 1 if 42:\n" +
                "            return 1\n" +
                "        case _:\n" +
                "            return 0\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("guard")), Is.True);
        }

        [Test]
        public void Ternary_MixedBranchTypes_AssignedToTypedValue_ReportsTypeMismatch()
        {
            string src =
                "fn f(flag: bool, value: int):\n" +
                "    value = 1 if flag else \"oops\"\n" +
                "    return value\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("value")), Is.True);
        }

        [Test]
        public void IfExpressionLikeFlow_MismatchedTypedAssignmentsAcrossBranches_ReportsTypeMismatch()
        {
            string src =
                "fn f(flag: bool, count: int):\n" +
                "    if flag:\n" +
                "        count = 1\n" +
                "    else:\n" +
                "        count = \"bad\"\n" +
                "    return count\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("count")), Is.True);
        }
    }
}
