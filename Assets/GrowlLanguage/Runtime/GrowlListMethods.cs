using System;
using System.Collections;
using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlListMethods
    {
        private static readonly System.Random Rng = new System.Random();

        internal static object Resolve(IList list, string methodName, Interpreter interp, GrowlLanguage.AST.GrowlNode site)
        {
            switch (methodName)
            {
                case "push":      return MakePush(list);
                case "pop":       return MakePop(list);
                case "insert":    return MakeInsert(list);
                case "remove":    return MakeRemove(list);
                case "contains":  return MakeContains(list);
                case "sort":      return MakeSort(list);
                case "reverse":   return MakeReverse(list);
                case "map":       return MakeMap(list);
                case "filter":    return MakeFilter(list);
                case "reduce":    return MakeReduce(list);
                case "each":      return MakeEach(list);
                case "any":       return MakeAny(list);
                case "all":       return MakeAll(list);
                case "find":      return MakeFind(list);
                case "flatten":   return MakeFlatten(list);
                case "zip":       return MakeZip(list);
                case "unique":    return MakeUnique(list);
                case "count":     return MakeCount(list);
                case "min":       return MakeMin(list);
                case "max":       return MakeMax(list);
                case "sum":       return MakeSum(list);
                case "avg":       return MakeAvg(list);
                case "sample":    return MakeSample(list);
                case "shuffle":   return MakeShuffle(list);
                case "enumerate": return MakeEnumerate(list);
                case "indexOf":   return MakeIndexOf(list);
                default:          return null;
            }
        }

        // ── Mutation ────────────────────────────────────────────────────

        private static RuntimeBuiltinFunction MakePush(IList list)
        {
            return new RuntimeBuiltinFunction("push", (interp, args, site) =>
            {
                for (int i = 0; i < args.Count; i++)
                    list.Add(args[i].Value);
                return list;
            });
        }

        private static RuntimeBuiltinFunction MakePop(IList list)
        {
            return new RuntimeBuiltinFunction("pop", (interp, args, site) =>
            {
                if (list.Count == 0)
                    throw new RuntimeExecutionException("pop() on empty list.", site.Line, site.Column);
                int idx = list.Count - 1;
                object value = list[idx];
                list.RemoveAt(idx);
                return value;
            });
        }

        private static RuntimeBuiltinFunction MakeInsert(IList list)
        {
            return new RuntimeBuiltinFunction("insert", (interp, args, site) =>
            {
                if (args.Count < 2)
                    throw new RuntimeExecutionException("insert() expects (index, value).", site.Line, site.Column);
                if (!Interpreter.TryGetDouble(args[0].Value, out double dIdx))
                    throw new RuntimeExecutionException("insert() index must be a number.", site.Line, site.Column);
                int idx = (int)dIdx;
                if (idx < 0) idx = Math.Max(0, list.Count + idx);
                if (idx > list.Count) idx = list.Count;
                list.Insert(idx, args[1].Value);
                return list;
            });
        }

        private static RuntimeBuiltinFunction MakeRemove(IList list)
        {
            return new RuntimeBuiltinFunction("remove", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("remove() expects an argument.", site.Line, site.Column);
                object target = args[0].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (Interpreter.RuntimeEquals(list[i], target))
                    {
                        list.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            });
        }

        private static RuntimeBuiltinFunction MakeReverse(IList list)
        {
            return new RuntimeBuiltinFunction("reverse", (interp, args, site) =>
            {
                int n = list.Count;
                for (int i = 0; i < n / 2; i++)
                {
                    int j = n - 1 - i;
                    object tmp = list[i];
                    list[i] = list[j];
                    list[j] = tmp;
                }
                return list;
            });
        }

        private static RuntimeBuiltinFunction MakeShuffle(IList list)
        {
            return new RuntimeBuiltinFunction("shuffle", (interp, args, site) =>
            {
                // Fisher-Yates shuffle
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = Rng.Next(i + 1);
                    object tmp = list[i];
                    list[i] = list[j];
                    list[j] = tmp;
                }
                return list;
            });
        }

        private static RuntimeBuiltinFunction MakeSort(IList list)
        {
            return new RuntimeBuiltinFunction("sort", (interp, args, site) =>
            {
                var items = new List<object>(list.Count);
                for (int i = 0; i < list.Count; i++) items.Add(list[i]);

                object byFn = null;
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i].Name == "by")
                        byFn = args[i].Value;
                    else if (i == 0 && args[i].Value is IRuntimeCallable)
                        byFn = args[i].Value;
                }

                if (byFn != null)
                {
                    items.Sort((a, b) =>
                    {
                        var callArgs = new List<RuntimeArgument>
                        {
                            new RuntimeArgument(null, a),
                            new RuntimeArgument(null, b),
                        };
                        object result = interp.InvokeCallable(byFn, callArgs, site);
                        if (result is bool boolResult) return boolResult ? -1 : 1;
                        if (Interpreter.TryGetDouble(result, out double d)) return d.CompareTo(0);
                        return 0;
                    });
                }
                else
                {
                    items.Sort(DefaultCompare);
                }

                list.Clear();
                for (int i = 0; i < items.Count; i++) list.Add(items[i]);
                return list;
            });
        }

        // ── Query ───────────────────────────────────────────────────────

        private static RuntimeBuiltinFunction MakeContains(IList list)
        {
            return new RuntimeBuiltinFunction("contains", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("contains() expects an argument.", site.Line, site.Column);
                object target = args[0].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (Interpreter.RuntimeEquals(list[i], target))
                        return true;
                }
                return false;
            });
        }

        private static RuntimeBuiltinFunction MakeIndexOf(IList list)
        {
            return new RuntimeBuiltinFunction("indexOf", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("indexOf() expects an argument.", site.Line, site.Column);
                object target = args[0].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (Interpreter.RuntimeEquals(list[i], target))
                        return (long)i;
                }
                return -1L;
            });
        }

        // ── Higher-order ────────────────────────────────────────────────

        private static RuntimeBuiltinFunction MakeMap(IList list)
        {
            return new RuntimeBuiltinFunction("map", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("map() expects a function argument.", site.Line, site.Column);
                object fn = args[0].Value;
                var result = new List<object>(list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument> { new RuntimeArgument(null, list[i]) };
                    result.Add(interp.InvokeCallable(fn, callArgs, site));
                }
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeFilter(IList list)
        {
            return new RuntimeBuiltinFunction("filter", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("filter() expects a function argument.", site.Line, site.Column);
                object fn = args[0].Value;
                var result = new List<object>();
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument> { new RuntimeArgument(null, list[i]) };
                    object val = interp.InvokeCallable(fn, callArgs, site);
                    if (Interpreter.IsTruthy(val))
                        result.Add(list[i]);
                }
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeReduce(IList list)
        {
            return new RuntimeBuiltinFunction("reduce", (interp, args, site) =>
            {
                if (args.Count < 2)
                    throw new RuntimeExecutionException("reduce() expects (initial, fn).", site.Line, site.Column);
                object accumulator = args[0].Value;
                object fn = args[1].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument>
                    {
                        new RuntimeArgument(null, accumulator),
                        new RuntimeArgument(null, list[i]),
                    };
                    accumulator = interp.InvokeCallable(fn, callArgs, site);
                }
                return accumulator;
            });
        }

        private static RuntimeBuiltinFunction MakeEach(IList list)
        {
            return new RuntimeBuiltinFunction("each", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("each() expects a function argument.", site.Line, site.Column);
                object fn = args[0].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument> { new RuntimeArgument(null, list[i]) };
                    interp.InvokeCallable(fn, callArgs, site);
                }
                return null;
            });
        }

        private static RuntimeBuiltinFunction MakeAny(IList list)
        {
            return new RuntimeBuiltinFunction("any", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("any() expects a function argument.", site.Line, site.Column);
                object fn = args[0].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument> { new RuntimeArgument(null, list[i]) };
                    if (Interpreter.IsTruthy(interp.InvokeCallable(fn, callArgs, site)))
                        return true;
                }
                return false;
            });
        }

        private static RuntimeBuiltinFunction MakeAll(IList list)
        {
            return new RuntimeBuiltinFunction("all", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("all() expects a function argument.", site.Line, site.Column);
                object fn = args[0].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument> { new RuntimeArgument(null, list[i]) };
                    if (!Interpreter.IsTruthy(interp.InvokeCallable(fn, callArgs, site)))
                        return false;
                }
                return true;
            });
        }

        private static RuntimeBuiltinFunction MakeFind(IList list)
        {
            return new RuntimeBuiltinFunction("find", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("find() expects a function argument.", site.Line, site.Column);
                object fn = args[0].Value;
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument> { new RuntimeArgument(null, list[i]) };
                    if (Interpreter.IsTruthy(interp.InvokeCallable(fn, callArgs, site)))
                        return list[i];
                }
                return null;
            });
        }

        private static RuntimeBuiltinFunction MakeCount(IList list)
        {
            return new RuntimeBuiltinFunction("count", (interp, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("count() expects a function argument.", site.Line, site.Column);
                object fn = args[0].Value;
                long count = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    var callArgs = new List<RuntimeArgument> { new RuntimeArgument(null, list[i]) };
                    if (Interpreter.IsTruthy(interp.InvokeCallable(fn, callArgs, site)))
                        count++;
                }
                return count;
            });
        }

        // ── Aggregation ─────────────────────────────────────────────────

        private static RuntimeBuiltinFunction MakeMin(IList list)
        {
            return new RuntimeBuiltinFunction("min", (interp, args, site) =>
            {
                if (list.Count == 0) return null;
                object best = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    if (DefaultCompare(list[i], best) < 0)
                        best = list[i];
                }
                return best;
            });
        }

        private static RuntimeBuiltinFunction MakeMax(IList list)
        {
            return new RuntimeBuiltinFunction("max", (interp, args, site) =>
            {
                if (list.Count == 0) return null;
                object best = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    if (DefaultCompare(list[i], best) > 0)
                        best = list[i];
                }
                return best;
            });
        }

        private static RuntimeBuiltinFunction MakeSum(IList list)
        {
            return new RuntimeBuiltinFunction("sum", (interp, args, site) =>
            {
                double total = 0d;
                for (int i = 0; i < list.Count; i++)
                {
                    if (Interpreter.TryGetDouble(list[i], out double v))
                        total += v;
                }
                return total;
            });
        }

        private static RuntimeBuiltinFunction MakeAvg(IList list)
        {
            return new RuntimeBuiltinFunction("avg", (interp, args, site) =>
            {
                if (list.Count == 0) return 0.0;
                double total = 0d;
                int numericCount = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (Interpreter.TryGetDouble(list[i], out double v))
                    {
                        total += v;
                        numericCount++;
                    }
                }
                return numericCount > 0 ? total / numericCount : 0.0;
            });
        }

        // ── Transformation ──────────────────────────────────────────────

        private static RuntimeBuiltinFunction MakeFlatten(IList list)
        {
            return new RuntimeBuiltinFunction("flatten", (interp, args, site) =>
            {
                var result = new List<object>();
                FlattenRecursive(list, result);
                return result;
            });
        }

        private static void FlattenRecursive(IList source, List<object> target)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] is IList nested)
                    FlattenRecursive(nested, target);
                else
                    target.Add(source[i]);
            }
        }

        private static RuntimeBuiltinFunction MakeZip(IList list)
        {
            return new RuntimeBuiltinFunction("zip", (interp, args, site) =>
            {
                if (args.Count < 1 || !(args[0].Value is IList other))
                    throw new RuntimeExecutionException("zip() expects a list argument.", site.Line, site.Column);

                int len = Math.Min(list.Count, other.Count);
                var result = new List<object>(len);
                for (int i = 0; i < len; i++)
                    result.Add(new GrowlTuple(new List<object> { list[i], other[i] }));
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeUnique(IList list)
        {
            return new RuntimeBuiltinFunction("unique", (interp, args, site) =>
            {
                var result = new List<object>();
                for (int i = 0; i < list.Count; i++)
                {
                    bool found = false;
                    for (int j = 0; j < result.Count; j++)
                    {
                        if (Interpreter.RuntimeEquals(list[i], result[j]))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        result.Add(list[i]);
                }
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeEnumerate(IList list)
        {
            return new RuntimeBuiltinFunction("enumerate", (interp, args, site) =>
            {
                var result = new List<object>(list.Count);
                for (int i = 0; i < list.Count; i++)
                    result.Add(new GrowlTuple(new List<object> { (long)i, list[i] }));
                return result;
            });
        }

        private static RuntimeBuiltinFunction MakeSample(IList list)
        {
            return new RuntimeBuiltinFunction("sample", (interp, args, site) =>
            {
                if (list.Count == 0) return null;

                if (args.Count > 0 && Interpreter.TryGetDouble(args[0].Value, out double dN))
                {
                    int n = Math.Min((int)dN, list.Count);
                    // Sample without replacement using Fisher-Yates on a copy
                    var pool = new List<object>(list.Count);
                    for (int i = 0; i < list.Count; i++) pool.Add(list[i]);
                    var result = new List<object>(n);
                    for (int i = 0; i < n; i++)
                    {
                        int j = Rng.Next(pool.Count);
                        result.Add(pool[j]);
                        pool[j] = pool[pool.Count - 1];
                        pool.RemoveAt(pool.Count - 1);
                    }
                    return result;
                }

                return list[Rng.Next(list.Count)];
            });
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static int DefaultCompare(object a, object b)
        {
            if (Interpreter.TryGetDouble(a, out double da) && Interpreter.TryGetDouble(b, out double db))
                return da.CompareTo(db);
            if (a is string sa && b is string sb)
                return string.Compare(sa, sb, StringComparison.Ordinal);
            return 0;
        }
    }
}
