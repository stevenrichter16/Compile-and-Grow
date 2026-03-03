using System;
using System.Collections;
using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlStringMethods
    {
        internal static object Resolve(string str, string methodName, Interpreter interp, GrowlLanguage.AST.GrowlNode site)
        {
            switch (methodName)
            {
                case "split":      return MakeSplit(str);
                case "join":       return MakeJoin(str);
                case "upper":      return MakeUpper(str);
                case "lower":      return MakeLower(str);
                case "trim":       return MakeTrim(str);
                case "contains":   return MakeContains(str);
                case "startswith": return MakeStartsWith(str);
                case "endswith":   return MakeEndsWith(str);
                case "replace":    return MakeReplace(str);
                case "format":     return MakeFormat(str);
                case "indexOf":    return MakeIndexOf(str);
                default:           return null;
            }
        }

        private static RuntimeBuiltinFunction MakeSplit(string str)
        {
            return new RuntimeBuiltinFunction("split", (interp, args, site) =>
            {
                string[] parts;
                if (args.Count > 0 && args[0].Value is string sep)
                    parts = str.Split(new[] { sep }, StringSplitOptions.None);
                else
                    parts = str.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                var result = new List<object>(parts.Length);
                for (int i = 0; i < parts.Length; i++)
                    result.Add(parts[i]);
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeJoin(string str)
        {
            return new RuntimeBuiltinFunction("join", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("join() expects a list argument.", site.Line, site.Column);

                if (!(args[0].Value is IList list))
                    throw new RuntimeExecutionException("join() expects a list argument.", site.Line, site.Column);

                var parts = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                    parts[i] = list[i]?.ToString() ?? "";

                return string.Join(str, parts);
            });
        }

        private static RuntimeBuiltinFunction MakeUpper(string str)
        {
            return new RuntimeBuiltinFunction("upper", (interp, args, site) => str.ToUpperInvariant());
        }

        private static RuntimeBuiltinFunction MakeLower(string str)
        {
            return new RuntimeBuiltinFunction("lower", (interp, args, site) => str.ToLowerInvariant());
        }

        private static RuntimeBuiltinFunction MakeTrim(string str)
        {
            return new RuntimeBuiltinFunction("trim", (interp, args, site) => str.Trim());
        }

        private static RuntimeBuiltinFunction MakeContains(string str)
        {
            return new RuntimeBuiltinFunction("contains", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("contains() expects an argument.", site.Line, site.Column);
                string sub = args[0].Value?.ToString() ?? "";
                return str.IndexOf(sub, StringComparison.Ordinal) >= 0;
            });
        }

        private static RuntimeBuiltinFunction MakeStartsWith(string str)
        {
            return new RuntimeBuiltinFunction("startswith", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("startswith() expects an argument.", site.Line, site.Column);
                string prefix = args[0].Value?.ToString() ?? "";
                return str.StartsWith(prefix, StringComparison.Ordinal);
            });
        }

        private static RuntimeBuiltinFunction MakeEndsWith(string str)
        {
            return new RuntimeBuiltinFunction("endswith", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("endswith() expects an argument.", site.Line, site.Column);
                string suffix = args[0].Value?.ToString() ?? "";
                return str.EndsWith(suffix, StringComparison.Ordinal);
            });
        }

        private static RuntimeBuiltinFunction MakeReplace(string str)
        {
            return new RuntimeBuiltinFunction("replace", (interp, args, site) =>
            {
                if (args.Count < 2)
                    throw new RuntimeExecutionException("replace() expects two arguments (old, new).", site.Line, site.Column);
                string oldVal = args[0].Value?.ToString() ?? "";
                string newVal = args[1].Value?.ToString() ?? "";
                return str.Replace(oldVal, newVal);
            });
        }

        private static RuntimeBuiltinFunction MakeFormat(string str)
        {
            return new RuntimeBuiltinFunction("format", (interp, args, site) =>
            {
                string result = str;
                for (int i = 0; i < args.Count; i++)
                {
                    string placeholder = "{" + i + "}";
                    string value = args[i].Value is string s ? s : RuntimeValueFormatter.Format(args[i].Value);
                    result = result.Replace(placeholder, value);
                }
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeIndexOf(string str)
        {
            return new RuntimeBuiltinFunction("indexOf", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("indexOf() expects an argument.", site.Line, site.Column);
                string sub = args[0].Value?.ToString() ?? "";
                return (long)str.IndexOf(sub, StringComparison.Ordinal);
            });
        }
    }
}
