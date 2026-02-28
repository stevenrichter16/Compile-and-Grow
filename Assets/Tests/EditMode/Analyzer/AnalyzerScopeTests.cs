using NUnit.Framework;
using System.Linq;
using GrowlLanguage.AST;
using GrowlLanguage.Analyzer;

namespace GrowlLanguage.Tests.Analyzer
{
    [TestFixture]
    public class AnalyzerScopeTests
    {
        private static AnalyzeResult Analyze(string src) => SemanticAnalyzer.Analyze(src);

        [Test]
        public void GlobalScope_BindsTypeAliasConstAndFunction()
        {
            string src =
                "type Energy = float\n" +
                "const max_energy: Energy = 100.0\n" +
                "fn grow(depth):\n" +
                "    return depth\n";

            var result = Analyze(src);

            Assert.That(result.HasErrors, Is.False);
            Assert.That(result.GlobalScope.TryResolve("Energy", out var energy), Is.True);
            Assert.That(energy.Kind, Is.EqualTo(SymbolKind.TypeAlias));

            Assert.That(result.GlobalScope.TryResolve("max_energy", out var budget), Is.True);
            Assert.That(budget.Kind, Is.EqualTo(SymbolKind.Const));

            Assert.That(result.GlobalScope.TryResolve("grow", out var grow), Is.True);
            Assert.That(grow.Kind, Is.EqualTo(SymbolKind.Function));
        }

        [Test]
        public void FunctionScope_BindsParameter_AndReturnReferenceResolvesToIt()
        {
            string src =
                "fn grow(depth):\n" +
                "    return depth\n";

            var result = Analyze(src);
            Assert.That(result.HasErrors, Is.False);

            var fn = result.Program.Statements.OfType<FnDecl>().Single();
            Assert.That(result.ScopeByNode.TryGetValue(fn, out var fnScope), Is.True);
            Assert.That(fnScope.TryResolve("depth", out var depth), Is.True);
            Assert.That(depth.Kind, Is.EqualTo(SymbolKind.Parameter));

            var ret = fn.Body.OfType<ReturnStmt>().Single();
            var name = ret.Value as NameExpr;
            Assert.That(name, Is.Not.Null);
            Assert.That(result.NameBindings.TryGetValue(name, out var bound), Is.True);
            Assert.That(bound.Kind, Is.EqualTo(SymbolKind.Parameter));
            Assert.That(bound.Name, Is.EqualTo("depth"));
        }

        [Test]
        public void UnresolvedName_ReportsDiagnosticWithLocation()
        {
            string src =
                "fn f():\n" +
                "    return missing\n";

            var result = Analyze(src);
            Assert.That(result.HasErrors, Is.True);

            var err = result.Errors.FirstOrDefault(e => e.Code == AnalyzeErrorCode.UnresolvedName);
            Assert.That(err.Code, Is.EqualTo(AnalyzeErrorCode.UnresolvedName));
            Assert.That(err.Message, Does.Contain("missing"));
            Assert.That(err.Line, Is.EqualTo(2));
            Assert.That(err.Column, Is.EqualTo(12));
        }

        [Test]
        public void DuplicateSymbol_InSameScope_ReportsDiagnostic()
        {
            string src =
                "const x = 1\n" +
                "const x = 2\n";

            var result = Analyze(src);
            Assert.That(result.HasErrors, Is.True);
            Assert.That(result.Errors.Any(e =>
                e.Code == AnalyzeErrorCode.DuplicateSymbol &&
                e.Message.Contains("x")), Is.True);
        }

        [Test]
        public void Shadowing_InInnerScope_IsAllowed_AndBindsNearestSymbol()
        {
            string src =
                "const x = 1\n" +
                "fn f(x):\n" +
                "    return x\n";

            var result = Analyze(src);
            Assert.That(result.Errors.Any(e => e.Code == AnalyzeErrorCode.DuplicateSymbol), Is.False);

            var fn = result.Program.Statements.OfType<FnDecl>().Single();
            var ret = fn.Body.OfType<ReturnStmt>().Single();
            var name = ret.Value as NameExpr;
            Assert.That(name, Is.Not.Null);
            Assert.That(result.NameBindings.TryGetValue(name, out var bound), Is.True);
            Assert.That(bound.Kind, Is.EqualTo(SymbolKind.Parameter));
            Assert.That(bound.Name, Is.EqualTo("x"));
        }
    }
}
