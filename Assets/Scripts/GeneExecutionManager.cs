using UnityEngine;
using GrowlLanguage.Runtime;

[DisallowMultipleComponent]
public sealed class GeneExecutionManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string _entryFunctionName = "main";
    [SerializeField] private int _maxLoopIterations = 10000;

    private GrowthTickManager _tickManager;
    private GrowlGameStateBridge _bridge;

    public static GeneExecutionManager EnsureExists()
    {
        var existing = FindObjectOfType<GeneExecutionManager>();
        if (existing != null)
            return existing;

        var go = new GameObject("[GeneExecutionManager]");
        DontDestroyOnLoad(go);
        return go.AddComponent<GeneExecutionManager>();
    }

    private void Start()
    {
        _tickManager = FindObjectOfType<GrowthTickManager>();
        _bridge = FindObjectOfType<GrowlGameStateBridge>();

        if (_tickManager != null)
            _tickManager.OnTickAdvanced += OnTick;

        if (_tickManager == null)
            Debug.LogWarning("[GeneExec] No GrowthTickManager found.", this);
        if (_bridge == null)
            Debug.LogWarning("[GeneExec] No GrowlGameStateBridge found.", this);
    }

    private void OnDestroy()
    {
        if (_tickManager != null)
            _tickManager.OnTickAdvanced -= OnTick;
    }

    private void OnTick(long tick)
    {
        if (_bridge == null)
            return;

        var organisms = FindObjectsOfType<OrganismEntity>();

        for (int i = 0; i < organisms.Length; i++)
        {
            var org = organisms[i];

            if (!org.IsAlive || string.IsNullOrWhiteSpace(org.GrowlSource))
                continue;

            _bridge.SetOrganism(org);

            RuntimeResult result = GrowlRuntime.Execute(org.GrowlSource, new RuntimeOptions
            {
                AutoInvokeEntryFunction = true,
                EntryFunctionName = _entryFunctionName,
                MaxLoopIterations = _maxLoopIterations,
                Host = _bridge,
            });

            if (!result.Success)
            {
                for (int m = 0; m < result.Messages.Count; m++)
                    Debug.LogWarning($"[GeneExec] {org.OrganismName}: {result.Messages[m]}", org);
            }
        }
    }
}
