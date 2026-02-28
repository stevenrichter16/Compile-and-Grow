using System.Linq;
using NUnit.Framework;
using GrowlLanguage.AST;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerTypeSkeletonTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        private static TypeSymbol RequireExprType(AnalyzeResult result, GrowlNode expr)
        {
            Assert.That(
                result.ExpressionTypes.TryGetValue(expr, out var type),
                Is.True,
                "Expected expression type to be recorded.");

            return type;
        }

        [Test]
        public void LiteralExpressions_AreTaggedWithBuiltinTypes()
        {
            string src =
                "const i = 1\n" +
                "const f = 1.5\n" +
                "const s = \"leaf\"\n" +
                "const b = true\n" +
                "const n = none\n";

            var result = Analyze(src);
            Assert.That(result.HasErrors, Is.False);

            var decls = result.Program.Statements.OfType<ConstDecl>().ToArray();
            Assert.That(RequireExprType(result, decls[0].Value).Name, Is.EqualTo("int"));
            Assert.That(RequireExprType(result, decls[1].Value).Name, Is.EqualTo("float"));
            Assert.That(RequireExprType(result, decls[2].Value).Name, Is.EqualTo("string"));
            Assert.That(RequireExprType(result, decls[3].Value).Name, Is.EqualTo("bool"));
            Assert.That(RequireExprType(result, decls[4].Value).Name, Is.EqualTo("none"));
        }

        [Test]
        public void ConstAnnotation_Mismatch_ReportsTypeMismatch()
        {
            string src = "const max_energy: int = \"leaf\"\n";
            var result = Analyze(src);

            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.Contains("max_energy")), Is.True);
        }

        [Test]
        public void AssignmentToTypedParameter_Mismatch_ReportsTypeMismatch()
        {
            string src =
                "fn f(count: int):\n" +
                "    count = \"oops\"\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.TypeMismatch &&
                e.Message.Contains("count")), Is.True);
        }

        [Test]
        public void InvalidBinaryOperands_ReportDiagnostic()
        {
            string src =
                "fn f():\n" +
                "    x = \"leaf\" - 1\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.InvalidBinaryOperands), Is.True);
        }

        [Test]
        public void StringConcatenation_HasStringType()
        {
            string src = "const label = \"a\" + \"b\"\n";
            var result = Analyze(src);

            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.InvalidBinaryOperands), Is.False);

            var decl = result.Program.Statements.OfType<ConstDecl>().Single();
            Assert.That(RequireExprType(result, decl.Value).Name, Is.EqualTo("string"));
        }

        [Test]
        public void FloatAnnotation_AcceptsIntLiteral()
        {
            string src = "const threshold: float = 1\n";
            var result = Analyze(src);

            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.TypeMismatch), Is.False);
        }
    }
}
