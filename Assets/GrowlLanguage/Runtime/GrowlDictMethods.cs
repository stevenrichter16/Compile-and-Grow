using System;
using System.Collections;
using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlDictMethods
    {
        internal static object Resolve(IDictionary dict, string methodName, Interpreter interp, GrowlLanguage.AST.GrowlNode site)
        {
            switch (methodName)
            {
                case "keys":    return MakeKeys(dict);
                case "values":  return MakeValues(dict);
                case "entries": return MakeEntries(dict);
                case "has":     return MakeHas(dict);
                case "remove":  return MakeRemove(dict);
                case "merge":   return MakeMerge(dict);
                case "get":     return MakeGet(dict);
                default:        return null;
            }
        }

        private static RuntimeBuiltinFunction MakeKeys(IDictionary dict)
        {
            return new RuntimeBuiltinFunction("keys", (interp, args, site) =>
            {
                var result = new List<object>(dict.Count);
                foreach (DictionaryEntry entry in dict)
                    result.Add(entry.Key);
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeValues(IDictionary dict)
        {
            return new RuntimeBuiltinFunction("values", (interp, args, site) =>
            {
                var result = new List<object>(dict.Count);
                foreach (DictionaryEntry entry in dict)
                    result.Add(entry.Value);
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeEntries(IDictionary dict)
        {
            return new RuntimeBuiltinFunction("entries", (interp, args, site) =>
            {
                var result = new List<object>(dict.Count);
                foreach (DictionaryEntry entry in dict)
                    result.Add(new GrowlTuple(new List<object> { entry.Key, entry.Value }));
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeHas(IDictionary dict)
        {
            return new RuntimeBuiltinFunction("has", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("has() expects a key argument.", site.Line, site.Column);

                object key = args[0].Value;
                foreach (DictionaryEntry entry in dict)
                {
                    if (Interpreter.RuntimeEquals(entry.Key, key))
                        return true;
                }
                return false;
            });
        }

        private static RuntimeBuiltinFunction MakeRemove(IDictionary dict)
        {
            return new RuntimeBuiltinFunction("remove", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("remove() expects a key argument.", site.Line, site.Column);

                object key = args[0].Value;
                object matchingKey = null;
                foreach (DictionaryEntry entry in dict)
                {
                    if (Interpreter.RuntimeEquals(entry.Key, key))
                    {
                        matchingKey = entry.Key;
                        break;
                    }
                }

                if (matchingKey != null)
                {
                    dict.Remove(matchingKey);
                    return true;
                }
                return false;
            });
        }

        private static RuntimeBuiltinFunction MakeMerge(IDictionary dict)
        {
            return new RuntimeBuiltinFunction("merge", (interp, args, site) =>
            {
                if (args.Count < 1 || !(args[0].Value is IDictionary other))
                    throw new RuntimeExecutionException("merge() expects a dictionary argument.", site.Line, site.Column);

                var result = new Dictionary<object, object>();
                foreach (DictionaryEntry entry in dict)
                    result[entry.Key] = entry.Value;
                foreach (DictionaryEntry entry in other)
                    result[entry.Key] = entry.Value;
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeGet(IDictionary dict)
        {
            return new RuntimeBuiltinFunction("get", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("get() expects a key argument.", site.Line, site.Column);

                object key = args[0].Value;

                // Check for default value (positional arg 2 or named "default")
                object defaultVal = null;
                if (args.Count > 1)
                    defaultVal = args[1].Value;
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i].Name == "default")
                    {
                        defaultVal = args[i].Value;
                        break;
                    }
                }

                foreach (DictionaryEntry entry in dict)
                {
                    if (Interpreter.RuntimeEquals(entry.Key, key))
                        return entry.Value;
                }
                return defaultVal;
            });
        }
    }
}
