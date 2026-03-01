using UnityEngine;

public static class GrowlRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeObjects()
    {
        GrowlRuntimeHostResolver.GetOrCreateHostBridge();
        GeneExecutionManager.EnsureExists();
    }
}
