using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    public readonly struct RuntimeCallArgument
    {
        public string Name { get; }
        public object Value { get; }

        public RuntimeCallArgument(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    public interface IGrowlRuntimeHost
    {
        void PopulateGlobals(IDictionary<string, object> globals);

        bool TryInvokeBuiltin(
            string builtinName,
            IReadOnlyList<RuntimeCallArgument> args,
            out object result,
            out string errorMessage);
    }
}
