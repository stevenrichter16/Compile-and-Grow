using UnityEngine;

public static class GrowlRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeObjects()
    {
        // Ensure a host bridge exists (it will auto-attach required gameplay systems).
        GrowlRuntimeHostResolver.GetOrCreateHostBridge();

        // Ensure an overlay terminal exists so pressing T always works in play mode.
        if (Object.FindObjectOfType<GrowlTerminalOverlay>() == null)
        {
            var terminalObject = new GameObject("GrowlTerminal");
            terminalObject.AddComponent<GrowlTerminalOverlay>();
        }
    }
}
