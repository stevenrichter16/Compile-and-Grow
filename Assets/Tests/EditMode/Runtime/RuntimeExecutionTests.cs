using System.Linq;
using NUnit.Framework;
using GrowlLanguage.Runtime;

namespace GrowlLanguage.Tests.Runtime
{
    [TestFixture]
    public class RuntimeExecutionTests
    {
        private static RuntimeResult Execute(string src, RuntimeOptions options = null)
            => GrowlRuntime.Execute(src, options);

        [Test]
        public void TopLevel_AssignmentAndExpression_ReturnsLastValue()
        {
            string src =
                "x = 2\n" +
                "x += 3\n" +
                "x\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo(5L));
        }

        [Test]
        public void FunctionCall_AndPrint_CaptureOutputAndReturnValue()
        {
            string src =
                "fn greet(name):\n" +
                "    print(\"hello\", name)\n" +
                "    return name\n" +
                "value = greet(\"seed\")\n" +
                "value\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.True);
            Assert.That(result.OutputLines.Count, Is.EqualTo(1));
            Assert.That(result.OutputLines[0], Is.EqualTo("hello seed"));
            Assert.That(result.LastValue, Is.EqualTo("seed"));
        }

        [Test]
        public void AutoInvokeEntryFunction_CallsMain()
        {
            string src =
                "fn main():\n" +
                "    return 42\n";

            RuntimeResult result = Execute(src, new RuntimeOptions
            {
                AutoInvokeEntryFunction = true,
                EntryFunctionName = "main",
            });

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo(42L));
        }

        [Test]
        public void MatchTuplePattern_ExecutesMatchingCase()
        {
            string src =
                "fn pick(pair):\n" +
                "    match pair:\n" +
                "        case (a, b):\n" +
                "            return b\n" +
                "        case _:\n" +
                "            return 0\n" +
                "result = pick((1, 9))\n" +
                "result\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo(9L));
        }

        [Test]
        public void TryRecoverAlways_HandlesRuntimeFailure()
        {
            string src =
                "fn run():\n" +
                "    x = 0\n" +
                "    try:\n" +
                "        y = 1 / 0\n" +
                "    recover err:\n" +
                "        x = 7\n" +
                "    always:\n" +
                "        x += 1\n" +
                "    return x\n" +
                "run()\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo(8L));
        }

        [Test]
        public void StructConstructor_SupportsNamedArguments_AndAttributeAccess()
        {
            string src =
                "struct Point:\n" +
                "    x: int = 1\n" +
                "    y: int = 2\n" +
                "p = Point(y: 9)\n" +
                "p.y\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo(9L));
        }

        [Test]
        public void ParseErrors_BlockRuntimeExecution()
        {
            string src =
                "if:\n" +
                "    x = 1\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Messages.Any(m => m.Kind == RuntimeMessageKind.ParseError), Is.True);
        }

        [Test]
        public void AnalyzeErrors_BlockRuntimeExecution()
        {
            string src =
                "fn f(x: int):\n" +
                "    return x\n" +
                "f(\"bad\")\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Messages.Any(m => m.Kind == RuntimeMessageKind.AnalyzeError), Is.True);
        }

        [Test]
        public void RuntimeErrors_AreReportedAfterSuccessfulParseAndAnalyze()
        {
            string src =
                "x = 1 / 0\n" +
                "x\n";

            RuntimeResult result = Execute(src);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Messages.Any(m =>
                m.Kind == RuntimeMessageKind.RuntimeError &&
                m.Message.ToLowerInvariant().Contains("division by zero")), Is.True);
        }
    }
}
