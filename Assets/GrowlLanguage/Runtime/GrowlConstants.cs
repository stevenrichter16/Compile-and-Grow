namespace GrowlLanguage.Runtime
{
    internal static class GrowlConstants
    {
        internal static void Register(RuntimeEnvironment globals, BiologicalContext bioContext)
        {
            globals.Define("UP", new GrowlVector(0, 1, 0));
            globals.Define("DOWN", new GrowlVector(0, -1, 0));
            globals.Define("LEFT", new GrowlVector(-1, 0, 0));
            globals.Define("RIGHT", new GrowlVector(1, 0, 0));
            globals.Define("NORTH", new GrowlVector(0, 1, 0));
            globals.Define("SOUTH", new GrowlVector(0, -1, 0));
            globals.Define("EAST", new GrowlVector(1, 0, 0));
            globals.Define("WEST", new GrowlVector(-1, 0, 0));
            globals.Define("NONE", null);
            globals.Define("TICK", bioContext?.CurrentTick ?? 0L);
            globals.Define("SELF", null);
        }
    }
}
