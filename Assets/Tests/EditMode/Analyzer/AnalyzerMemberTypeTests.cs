using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerMemberTypeTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void MemberAccess_UnknownField_ReportsTypeMismatch()
        {
            string src =
                "struct Plant:\n" +
                "    energy: int\n" +
                "\n" +
                "fn f(p: Plant):\n" +
                "    return p.mass\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("mass")), Is.True);
        }

        [Test]
        public void MemberAssignment_FieldTypeMismatch_ReportsTypeMismatch()
        {
            string src =
                "struct Plant:\n" +
                "    energy: int\n" +
                "\n" +
                "fn f(p: Plant):\n" +
                "    p.energy = \"high\"\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("energy")), Is.True);
        }

        [Test]
        public void MemberCall_UnknownMethod_ReportsTypeMismatch()
        {
            string src =
                "struct Plant:\n" +
                "    fn grow(self):\n" +
                "        return none\n" +
                "\n" +
                "fn f(p: Plant):\n" +
                "    p.shrink()\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("shrink")), Is.True);
        }

        [Test]
        public void MemberCall_NonCallableField_ReportsTypeMismatch()
        {
            string src =
                "struct Plant:\n" +
                "    energy: int\n" +
                "\n" +
                "fn f(p: Plant):\n" +
                "    p.energy()\n";

            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.ToLowerInvariant().Contains("energy")), Is.True);
        }
    }
}
