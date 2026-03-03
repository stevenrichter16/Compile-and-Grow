using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlSetMethods
    {
        internal static object Resolve(GrowlSet set, string methodName, Interpreter interp, GrowlLanguage.AST.GrowlNode site)
        {
            switch (methodName)
            {
                case "add":      return MakeAdd(set);
                case "remove":   return MakeRemove(set);
                case "contains": return MakeContains(set);
                default:         return null;
            }
        }

        private static RuntimeBuiltinFunction MakeAdd(GrowlSet set)
        {
            return new RuntimeBuiltinFunction("add", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("add() expects an argument.", site.Line, site.Column);

                object value = args[0].Value;
                List<object> elements = set.MutableElements;
                for (int i = 0; i < elements.Count; i++)
                {
                    if (Interpreter.RuntimeEquals(elements[i], value))
                        return set;
                }
                elements.Add(value);
                return set;
            });
        }

        private static RuntimeBuiltinFunction MakeRemove(GrowlSet set)
        {
            return new RuntimeBuiltinFunction("remove", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("remove() expects an argument.", site.Line, site.Column);

                object value = args[0].Value;
                List<object> elements = set.MutableElements;
                for (int i = 0; i < elements.Count; i++)
                {
                    if (Interpreter.RuntimeEquals(elements[i], value))
                    {
                        elements.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            });
        }

        private static RuntimeBuiltinFunction MakeContains(GrowlSet set)
        {
            return new RuntimeBuiltinFunction("contains", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("contains() expects an argument.", site.Line, site.Column);

                object value = args[0].Value;
                List<object> elements = set.MutableElements;
                for (int i = 0; i < elements.Count; i++)
                {
                    if (Interpreter.RuntimeEquals(elements[i], value))
                        return true;
                }
                return false;
            });
        }
    }
}
