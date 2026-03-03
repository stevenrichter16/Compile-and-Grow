using System;
using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlMathBuiltins
    {
        internal static void Register(RuntimeEnvironment globals)
        {
            // ── Global convenience functions ────────────────────────────
            globals.Define("min", new RuntimeBuiltinFunction("min", BuiltinMin));
            globals.Define("max", new RuntimeBuiltinFunction("max", BuiltinMax));
            globals.Define("abs", new RuntimeBuiltinFunction("abs", BuiltinAbs));
            globals.Define("round", new RuntimeBuiltinFunction("round", BuiltinRound));
            globals.Define("sqrt", new RuntimeBuiltinFunction("sqrt", BuiltinSqrt));
            globals.Define("sin", new RuntimeBuiltinFunction("sin", BuiltinSin));
            globals.Define("cos", new RuntimeBuiltinFunction("cos", BuiltinCos));
            globals.Define("tan", new RuntimeBuiltinFunction("tan", BuiltinTan));
            globals.Define("clamp", new RuntimeBuiltinFunction("clamp", BuiltinClamp));
            globals.Define("lerp", new RuntimeBuiltinFunction("lerp", BuiltinLerp));
            globals.Define("remap", new RuntimeBuiltinFunction("remap", BuiltinRemap));
            globals.Define("floor", new RuntimeBuiltinFunction("floor", BuiltinFloor));
            globals.Define("ceil", new RuntimeBuiltinFunction("ceil", BuiltinCeil));
            globals.Define("pow", new RuntimeBuiltinFunction("pow", BuiltinPow));
            globals.Define("str", new RuntimeBuiltinFunction("str", BuiltinStr));

            // ── math namespace ──────────────────────────────────────────
            var mathDict = new Dictionary<object, object>
            {
                ["PI"] = Math.PI,
                ["E"] = Math.E,
                ["TAU"] = Math.PI * 2.0,
                ["INF"] = double.PositiveInfinity,
                ["sin"] = new RuntimeBuiltinFunction("sin", BuiltinSin),
                ["cos"] = new RuntimeBuiltinFunction("cos", BuiltinCos),
                ["tan"] = new RuntimeBuiltinFunction("tan", BuiltinTan),
                ["asin"] = new RuntimeBuiltinFunction("asin", (_, args, site) => Math.Asin(RequireDouble(args, 0, "asin", site))),
                ["acos"] = new RuntimeBuiltinFunction("acos", (_, args, site) => Math.Acos(RequireDouble(args, 0, "acos", site))),
                ["atan2"] = new RuntimeBuiltinFunction("atan2", (_, args, site) =>
                {
                    double y = RequireDouble(args, 0, "atan2", site);
                    double x = RequireDouble(args, 1, "atan2", site);
                    return Math.Atan2(y, x);
                }),
                ["sqrt"] = new RuntimeBuiltinFunction("sqrt", BuiltinSqrt),
                ["abs"] = new RuntimeBuiltinFunction("abs", BuiltinAbs),
                ["floor"] = new RuntimeBuiltinFunction("floor", BuiltinFloor),
                ["ceil"] = new RuntimeBuiltinFunction("ceil", BuiltinCeil),
                ["round"] = new RuntimeBuiltinFunction("round", BuiltinRound),
                ["log"] = new RuntimeBuiltinFunction("log", (_, args, site) =>
                {
                    double val = RequireDouble(args, 0, "log", site);
                    if (args.Count > 1)
                    {
                        double b = RequireDouble(args, 1, "log", site);
                        return Math.Log(val, b);
                    }
                    return Math.Log(val);
                }),
                ["log2"] = new RuntimeBuiltinFunction("log2", (_, args, site) => Math.Log(RequireDouble(args, 0, "log2", site), 2.0)),
                ["log10"] = new RuntimeBuiltinFunction("log10", (_, args, site) => Math.Log10(RequireDouble(args, 0, "log10", site))),
                ["pow"] = new RuntimeBuiltinFunction("pow", BuiltinPow),
                ["radians"] = new RuntimeBuiltinFunction("radians", (_, args, site) => RequireDouble(args, 0, "radians", site) * Math.PI / 180.0),
                ["degrees"] = new RuntimeBuiltinFunction("degrees", (_, args, site) => RequireDouble(args, 0, "degrees", site) * 180.0 / Math.PI),
                ["sigmoid"] = new RuntimeBuiltinFunction("sigmoid", (_, args, site) =>
                {
                    double x = RequireDouble(args, 0, "sigmoid", site);
                    return 1.0 / (1.0 + Math.Exp(-x));
                }),
                ["smoothstep"] = new RuntimeBuiltinFunction("smoothstep", (_, args, site) =>
                {
                    double edge0 = RequireDouble(args, 0, "smoothstep", site);
                    double edge1 = RequireDouble(args, 1, "smoothstep", site);
                    double x = RequireDouble(args, 2, "smoothstep", site);
                    double t = Math.Max(0, Math.Min(1, (x - edge0) / (edge1 - edge0)));
                    return t * t * (3.0 - 2.0 * t);
                }),
                ["map_range"] = new RuntimeBuiltinFunction("map_range", BuiltinRemap),
            };

            globals.Define("math", mathDict);
        }

        // ── Implementation ──────────────────────────────────────────────

        private static object BuiltinMin(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 2)
                throw new RuntimeExecutionException("min() expects at least two arguments.", site.Line, site.Column);
            double a = RequireDouble(args, 0, "min", site);
            double b = RequireDouble(args, 1, "min", site);
            return Math.Min(a, b);
        }

        private static object BuiltinMax(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 2)
                throw new RuntimeExecutionException("max() expects at least two arguments.", site.Line, site.Column);
            double a = RequireDouble(args, 0, "max", site);
            double b = RequireDouble(args, 1, "max", site);
            return Math.Max(a, b);
        }

        private static object BuiltinAbs(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 1)
                throw new RuntimeExecutionException("abs() expects one argument.", site.Line, site.Column);
            object val = args[0].Value;
            if (val is long l) return Math.Abs(l);
            if (Interpreter.TryGetDouble(val, out double d)) return Math.Abs(d);
            throw new RuntimeExecutionException("abs() expects a numeric argument.", site.Line, site.Column);
        }

        private static object BuiltinRound(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 1)
                throw new RuntimeExecutionException("round() expects at least one argument.", site.Line, site.Column);
            double val = RequireDouble(args, 0, "round", site);
            int places = 0;
            if (args.Count > 1 && Interpreter.TryGetDouble(args[1].Value, out double dPlaces))
                places = (int)dPlaces;

            if (places == 0)
                return (long)Math.Round(val, MidpointRounding.AwayFromZero);
            return Math.Round(val, places, MidpointRounding.AwayFromZero);
        }

        private static object BuiltinSqrt(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            return Math.Sqrt(RequireDouble(args, 0, "sqrt", site));
        }

        private static object BuiltinSin(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            return Math.Sin(RequireDouble(args, 0, "sin", site));
        }

        private static object BuiltinCos(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            return Math.Cos(RequireDouble(args, 0, "cos", site));
        }

        private static object BuiltinTan(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            return Math.Tan(RequireDouble(args, 0, "tan", site));
        }

        private static object BuiltinClamp(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 3)
                throw new RuntimeExecutionException("clamp() expects (value, low, high).", site.Line, site.Column);
            double val = RequireDouble(args, 0, "clamp", site);
            double lo = RequireDouble(args, 1, "clamp", site);
            double hi = RequireDouble(args, 2, "clamp", site);
            return Math.Max(lo, Math.Min(hi, val));
        }

        private static object BuiltinLerp(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 3)
                throw new RuntimeExecutionException("lerp() expects (a, b, t).", site.Line, site.Column);
            double a = RequireDouble(args, 0, "lerp", site);
            double b = RequireDouble(args, 1, "lerp", site);
            double t = RequireDouble(args, 2, "lerp", site);
            return a + (b - a) * t;
        }

        private static object BuiltinRemap(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 5)
                throw new RuntimeExecutionException("remap() expects (value, in_low, in_high, out_low, out_high).", site.Line, site.Column);
            double v = RequireDouble(args, 0, "remap", site);
            double iLo = RequireDouble(args, 1, "remap", site);
            double iHi = RequireDouble(args, 2, "remap", site);
            double oLo = RequireDouble(args, 3, "remap", site);
            double oHi = RequireDouble(args, 4, "remap", site);
            double range = iHi - iLo;
            if (Math.Abs(range) < double.Epsilon) return oLo;
            return oLo + (v - iLo) / range * (oHi - oLo);
        }

        private static object BuiltinFloor(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            return (long)Math.Floor(RequireDouble(args, 0, "floor", site));
        }

        private static object BuiltinCeil(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            return (long)Math.Ceiling(RequireDouble(args, 0, "ceil", site));
        }

        private static object BuiltinPow(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 2)
                throw new RuntimeExecutionException("pow() expects (base, exponent).", site.Line, site.Column);
            double b = RequireDouble(args, 0, "pow", site);
            double e = RequireDouble(args, 1, "pow", site);
            return Math.Pow(b, e);
        }

        private static object BuiltinStr(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 1) return "";
            object val = args[0].Value;
            return val is string s ? s : RuntimeValueFormatter.Format(val);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static double RequireDouble(List<RuntimeArgument> args, int index, string fnName, GrowlLanguage.AST.GrowlNode site)
        {
            if (index >= args.Count)
                throw new RuntimeExecutionException(fnName + "() missing argument at position " + index + ".", site.Line, site.Column);
            if (!Interpreter.TryGetDouble(args[index].Value, out double result))
                throw new RuntimeExecutionException(fnName + "() expects a numeric argument at position " + index + ".", site.Line, site.Column);
            return result;
        }
    }
}
