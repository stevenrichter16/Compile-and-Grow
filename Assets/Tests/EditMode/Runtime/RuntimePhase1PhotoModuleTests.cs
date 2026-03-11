using System.Collections.Generic;
using NUnit.Framework;
using GrowlLanguage.Runtime;

namespace GrowlLanguage.Tests.Runtime
{
    [TestFixture]
    public class RuntimePhase1PhotoModuleTests
    {
        [Test]
        public void PhotoModule_Methods_DispatchToHost()
        {
            var host = new PhotoRuntimeHost();

            string src =
                "energy = photo.process()\n" +
                "limiting = photo.get_limiting_factor()\n" +
                "limiting\n";

            RuntimeResult result = GrowlRuntime.Execute(src, new RuntimeOptions
            {
                Host = host,
            });

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo("water"));
            Assert.That(host.Calls, Is.EqualTo(new[] { "photo_process", "photo_get_limiting_factor" }));
        }

        private sealed class PhotoRuntimeHost : IGrowlRuntimeHost
        {
            public List<string> Calls { get; } = new List<string>();

            public void PopulateGlobals(IDictionary<string, object> globals)
            {
            }

            public bool TryInvokeBuiltin(
                string builtinName,
                IReadOnlyList<RuntimeCallArgument> args,
                out object result,
                out string errorMessage)
            {
                Calls.Add(builtinName);
                errorMessage = null;

                switch (builtinName)
                {
                    case "photo_process":
                        result = 1.25d;
                        return true;
                    case "photo_get_limiting_factor":
                        result = "water";
                        return true;
                    default:
                        result = null;
                        errorMessage = "Unknown builtin.";
                        return false;
                }
            }
        }
    }
}
