using UnityEngine;

public static class GrowlRuntimeHostResolver
{
    private static GrowlGameStateBridge _cachedBridge;

    public static GrowlGameStateBridge GetOrCreateHostBridge()
    {
        if (_cachedBridge != null)
            return _cachedBridge;

        _cachedBridge = Object.FindObjectOfType<GrowlGameStateBridge>();
        if (_cachedBridge != null)
            return _cachedBridge;

        var hostObject = new GameObject("GrowlRuntimeHost");
        _cachedBridge = hostObject.AddComponent<GrowlGameStateBridge>();
        return _cachedBridge;
    }
}
