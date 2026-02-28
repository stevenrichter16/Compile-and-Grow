using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerReturnTypeTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void ReturnType_IntFunctionReturningInt_HasNoTypeMismatch()
        {
            string src =
                "fn score() -> int:\n" +
                "    return 1\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.TypeMismatch), Is.False);
        }

        [Test]
        public void ReturnType_IntFunctionReturningString_ReportsTypeMismatch()
        {
            string src =
                "fn score() -> int:\n" +
                "    return \"leaf\"\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.Contains("score")), Is.True);
        }

        [Test]
        public void ReturnType_IntFunctionBareReturn_ReportsTypeMismatch()
        {
            string src =
                "fn score() -> int:\n" +
                "    return\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.Contains("score")), Is.True);
        }

        [Test]
        public void ReturnType_NoneFunctionBareReturn_HasNoTypeMismatch()
        {
            string src =
                "fn done() -> none:\n" +
                "    return\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.TypeMismatch), Is.False);
        }

        [Test]
        public void ReturnType_FloatFunctionReturningInt_HasNoTypeMismatch()
        {
            string src =
                "fn measure() -> float:\n" +
                "    return 1\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.TypeMismatch), Is.False);
        }

        [Test]
        public void ReturnType_UnannotatedFunction_IsNotTypeChecked()
        {
            string src =
                "fn f():\n" +
                "    return \"anything\"\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.TypeMismatch), Is.False);
        }
    }
}
