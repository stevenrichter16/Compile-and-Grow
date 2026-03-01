using UnityEngine;

public static class GrowlRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeObjects()
    {
        // Ensure a host bridge exists (it will auto-attach required gameplay systems).
        GrowlRuntimeHostResolver.GetOrCreateHostBridge();
    }
}
