using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerReturnPathTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void ReturnPath_AnnotatedFunctionWithoutReturn_ReportsDiagnostic()
        {
            string src =
                "fn score() -> int:\n" +
                "    value = 1\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("score") &&
                e.Message.ToLowerInvariant().Contains("path")), Is.True);
        }

        [Test]
        public void ReturnPath_IfWithoutElse_ReportsDiagnostic()
        {
            string src =
                "fn score(flag: bool) -> int:\n" +
                "    if flag:\n" +
                "        return 1\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("score") &&
                e.Message.ToLowerInvariant().Contains("path")), Is.True);
        }

        [Test]
        public void ReturnPath_WhileOnlyReturn_ReportsDiagnostic()
        {
            string src =
                "fn score(flag: bool) -> int:\n" +
                "    while flag:\n" +
                "        return 1\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("score") &&
                e.Message.ToLowerInvariant().Contains("path")), Is.True);
        }

        [Test]
        public void ReturnPath_TryRecoverWithoutGuaranteedReturn_ReportsDiagnostic()
        {
            string src =
                "fn score() -> int:\n" +
                "    try:\n" +
                "        return 1\n" +
                "    recover err:\n" +
                "        value = 0\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("score") &&
                e.Message.ToLowerInvariant().Contains("path")), Is.True);
        }
    }
}
