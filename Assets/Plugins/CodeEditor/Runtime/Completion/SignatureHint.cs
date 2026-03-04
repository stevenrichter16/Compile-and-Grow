using System.Collections.Generic;

namespace CodeEditor.Completion
{
    public readonly struct SignatureParameter
    {
        public readonly string Name;
        public readonly bool Optional;

        public SignatureParameter(string name, bool optional = false)
        {
            Name = name;
            Optional = optional;
        }
    }

    public sealed class SignatureHint
    {
        public readonly string FunctionName;
        public readonly IReadOnlyList<SignatureParameter> Parameters;

        public SignatureHint(string functionName, IReadOnlyList<SignatureParameter> parameters)
        {
            FunctionName = functionName;
            Parameters = parameters;
        }

        /// <summary>
        /// Formats the signature as "name(param1, param2, [optional?])" with
        /// the active parameter index highlighted via rich text bold tags.
        /// </summary>
        public string Format(int activeParameter = -1)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(FunctionName);
            sb.Append('(');
            for (int i = 0; i < Parameters.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var p = Parameters[i];
                bool isActive = (i == activeParameter);
                if (isActive) sb.Append("<b>");
                sb.Append(p.Name);
                if (p.Optional) sb.Append('?');
                if (isActive) sb.Append("</b>");
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}
