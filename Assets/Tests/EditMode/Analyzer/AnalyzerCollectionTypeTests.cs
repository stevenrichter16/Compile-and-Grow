using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerCollectionTypeTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void ListAssignment_TypedListElementMismatch_ReportsTypeMismatch()
        {
            string src =
                "fn f(items: list[int]):\n" +
                "    items = [1, \"x\"]\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("items")), Is.True);
        }

        [Test]
        public void DictAssignment_TypedDictValueMismatch_ReportsTypeMismatch()
        {
            string src =
                "fn f(m: dict[string, int]):\n" +
                "    m = {\"ok\": 1, \"bad\": \"x\"}\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("m")), Is.True);
        }

        [Test]
        public void Subscript_IndexOnNonIndexableType_ReportsTypeMismatch()
        {
            string src =
                "fn f(n: int):\n" +
                "    x = n[0]\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("subscript")), Is.True);
        }

        [Test]
        public void StringSubscript_NonIntIndex_ReportsTypeMismatch()
        {
            string src =
                "fn f(flag: bool):\n" +
                "    x = \"leaf\"[flag]\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("index")), Is.True);
        }
    }
}
