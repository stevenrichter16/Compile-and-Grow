using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerCallTypeTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void Call_TooFewArguments_ReportsDiagnostic()
        {
            string src =
                "fn add(a: int, b: int) -> int:\n" +
                "    return a + b\n" +
                "\n" +
                "fn main():\n" +
                "    x = add(1)\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Message.ToLowerInvariant().Contains("argument") &&
                e.Message.Contains("add")), Is.True);
        }

        [Test]
        public void Call_TooManyArguments_ReportsDiagnostic()
        {
            string src =
                "fn add(a: int, b: int) -> int:\n" +
                "    return a + b\n" +
                "\n" +
                "fn main():\n" +
                "    x = add(1, 2, 3)\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Message.ToLowerInvariant().Contains("argument") &&
                e.Message.Contains("add")), Is.True);
        }

        [Test]
        public void Call_ArgumentTypeMismatch_ReportsTypeMismatch()
        {
            string src =
                "fn heat(level: int):\n" +
                "    return level\n" +
                "\n" +
                "fn main():\n" +
                "    x = heat(\"high\")\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.Contains("heat")), Is.True);
        }

        [Test]
        public void Call_MultipleMismatchedArguments_ReportTypeMismatch()
        {
            string src =
                "fn mix(temp: float, label: string):\n" +
                "    return temp\n" +
                "\n" +
                "fn main():\n" +
                "    x = mix(\"hot\", 42)\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Count(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.Contains("mix")) >= 2, Is.True);
        }
    }
}
