using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerDefiniteAssignmentTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void ReadBeforeAssignment_SelfReferentialAssignment_ReportsDiagnostic()
        {
            string src =
                "fn f():\n" +
                "    x = x + 1\n" +
                "    return x\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.ReadBeforeAssignment &&
                e.Message.ToLowerInvariant().Contains("before assignment") &&
                e.Message.Contains("x")), Is.True);
        }

        [Test]
        public void ReadAfterIf_AssignedOnlyInThenBranch_ReportsDiagnostic()
        {
            string src =
                "fn f(flag: bool):\n" +
                "    if flag:\n" +
                "        x = 1\n" +
                "    return x\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.ReadBeforeAssignment &&
                e.Message.ToLowerInvariant().Contains("before assignment") &&
                e.Message.Contains("x")), Is.True);
        }

        [Test]
        public void ReadAfterLoop_AssignedOnlyInsideLoop_ReportsDiagnostic()
        {
            string src =
                "fn f(flag: bool):\n" +
                "    while flag:\n" +
                "        x = 1\n" +
                "        break\n" +
                "    return x\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.ReadBeforeAssignment &&
                e.Message.ToLowerInvariant().Contains("before assignment") &&
                e.Message.Contains("x")), Is.True);
        }

        [Test]
        public void ReadAfterTryRecover_AssignedOnlyInRecover_ReportsDiagnostic()
        {
            string src =
                "fn f():\n" +
                "    try:\n" +
                "        value = 1\n" +
                "    recover err:\n" +
                "        x = 2\n" +
                "    return x\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.ReadBeforeAssignment &&
                e.Message.ToLowerInvariant().Contains("before assignment") &&
                e.Message.Contains("x")), Is.True);
        }
    }
}
