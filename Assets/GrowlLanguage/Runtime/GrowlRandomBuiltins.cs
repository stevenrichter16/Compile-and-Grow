using System;
using System.Collections;
using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlRandomBuiltins
    {
        private static readonly System.Random Rng = new System.Random();

        internal static void Register(RuntimeEnvironment globals)
        {
            globals.Define("random", new RuntimeBuiltinFunction("random", BuiltinRandom));
            globals.Define("random_int", new RuntimeBuiltinFunction("random_int", BuiltinRandomInt));
            globals.Define("random_choice", new RuntimeBuiltinFunction("random_choice", BuiltinRandomChoice));
            globals.Define("noise", new RuntimeBuiltinFunction("noise", BuiltinNoise));
            globals.Define("chance", new RuntimeBuiltinFunction("chance", BuiltinChance));
        }

        // random(low=0, high=1)
        private static object BuiltinRandom(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            double lo = 0.0, hi = 1.0;
            if (args.Count >= 1 && Interpreter.TryGetDouble(args[0].Value, out double a)) lo = a;
            if (args.Count >= 2 && Interpreter.TryGetDouble(args[1].Value, out double b)) hi = b;
            return lo + Rng.NextDouble() * (hi - lo);
        }

        // random_int(low, high)
        private static object BuiltinRandomInt(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 2)
                throw new RuntimeExecutionException("random_int() expects (low, high).", site.Line, site.Column);
            if (!Interpreter.TryGetDouble(args[0].Value, out double lo) || !Interpreter.TryGetDouble(args[1].Value, out double hi))
                throw new RuntimeExecutionException("random_int() expects numeric arguments.", site.Line, site.Column);
            int iLo = (int)lo;
            int iHi = (int)hi;
            return (long)Rng.Next(iLo, iHi + 1);
        }

        // random_choice(list)
        private static object BuiltinRandomChoice(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 1 || !(args[0].Value is IList list) || list.Count == 0)
                throw new RuntimeExecutionException("random_choice() expects a non-empty list.", site.Line, site.Column);
            return list[Rng.Next(list.Count)];
        }

        // chance(probability) — True with given probability
        private static object BuiltinChance(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 1)
                throw new RuntimeExecutionException("chance() expects a probability argument.", site.Line, site.Column);
            if (!Interpreter.TryGetDouble(args[0].Value, out double p))
                throw new RuntimeExecutionException("chance() expects a numeric probability.", site.Line, site.Column);
            return Rng.NextDouble() < p;
        }

        // noise(x, y=0, seed=0) — 2D Perlin noise
        private static object BuiltinNoise(Interpreter _, List<RuntimeArgument> args, GrowlLanguage.AST.GrowlNode site)
        {
            if (args.Count < 1)
                throw new RuntimeExecutionException("noise() expects at least one argument.", site.Line, site.Column);
            if (!Interpreter.TryGetDouble(args[0].Value, out double x))
                throw new RuntimeExecutionException("noise() expects numeric arguments.", site.Line, site.Column);
            double y = 0;
            if (args.Count >= 2 && Interpreter.TryGetDouble(args[1].Value, out double yVal)) y = yVal;
            int seed = 0;
            if (args.Count >= 3 && Interpreter.TryGetDouble(args[2].Value, out double sVal)) seed = (int)sVal;
            return PerlinNoise2D(x, y, seed);
        }

        // ── Perlin noise implementation ─────────────────────────────────

        private static readonly int[] Perm;

        static GrowlRandomBuiltins()
        {
            // Build permutation table
            Perm = new int[512];
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            var rng = new System.Random(42);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = p[i]; p[i] = p[j]; p[j] = tmp;
            }
            for (int i = 0; i < 512; i++) Perm[i] = p[i & 255];
        }

        private static double PerlinNoise2D(double x, double y, int seed)
        {
            x += seed * 17.1;
            y += seed * 31.7;

            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);

            double u = Fade(xf);
            double v = Fade(yf);

            int aa = Perm[Perm[xi] + yi];
            int ab = Perm[Perm[xi] + yi + 1];
            int ba = Perm[Perm[xi + 1] + yi];
            int bb = Perm[Perm[xi + 1] + yi + 1];

            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
            return Lerp(x1, x2, v);
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double a, double b, double t) => a + t * (b - a);

        private static double Grad(int hash, double x, double y)
        {
            switch (hash & 3)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                default: return -x - y;
            }
        }
    }
}
