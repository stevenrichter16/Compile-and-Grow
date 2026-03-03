using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    internal static class GrowlBioBuiltins
    {
        internal static void Register(RuntimeEnvironment globals, BiologicalContext bioContext)
        {
            long tick = bioContext?.CurrentTick ?? 0L;

            globals.Define("every", new RuntimeBuiltinFunction("every", (_, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("every() expects a tick interval.", site.Line, site.Column);
                if (!Interpreter.TryGetDouble(args[0].Value, out double n) || n <= 0)
                    return true;
                long currentTick = bioContext?.CurrentTick ?? 0L;
                return currentTick % (long)n == 0;
            }));

            globals.Define("after", new RuntimeBuiltinFunction("after", (_, args, site) =>
            {
                if (args.Count < 1)
                    throw new RuntimeExecutionException("after() expects a tick count.", site.Line, site.Column);
                if (!Interpreter.TryGetDouble(args[0].Value, out double threshold))
                    throw new RuntimeExecutionException("after() expects a numeric argument.", site.Line, site.Column);
                long currentTick = bioContext?.CurrentTick ?? 0L;
                return currentTick >= (long)threshold;
            }));

            globals.Define("between", new RuntimeBuiltinFunction("between", (_, args, site) =>
            {
                if (args.Count < 2)
                    throw new RuntimeExecutionException("between() expects (start, end).", site.Line, site.Column);
                if (!Interpreter.TryGetDouble(args[0].Value, out double start) ||
                    !Interpreter.TryGetDouble(args[1].Value, out double end))
                    throw new RuntimeExecutionException("between() expects numeric arguments.", site.Line, site.Column);
                long currentTick = bioContext?.CurrentTick ?? 0L;
                return currentTick >= (long)start && currentTick <= (long)end;
            }));

            // Indoor facility — always "stable" season
            globals.Define("season", new RuntimeBuiltinFunction("season", (_, args, site) => "stable"));

            // Map tick into 6 time-of-day phases (2400 tick day cycle)
            globals.Define("time_of_day", new RuntimeBuiltinFunction("time_of_day", (_, args, site) =>
            {
                long currentTick = bioContext?.CurrentTick ?? 0L;
                long phase = currentTick % 2400L;
                if (phase < 400) return "dawn";
                if (phase < 800) return "morning";
                if (phase < 1200) return "noon";
                if (phase < 1600) return "afternoon";
                if (phase < 2000) return "dusk";
                return "night";
            }));
        }
    }
}
