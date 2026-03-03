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

        // Attach to the same host object as the other runtime systems
        var bridge = FindObjectOfType<GrowlGameStateBridge>();
        if (bridge != null)
            return bridge.gameObject.AddComponent<GeneExecutionManager>();

        var go = new GameObject("[GeneExecutionManager]");
        return go.AddComponent<GeneExecutionManager>();
    }

    private void Start()
    {
        _tickManager = GetComponent<GrowthTickManager>() ?? FindObjectOfType<GrowthTickManager>();
        _bridge = GetComponent<GrowlGameStateBridge>() ?? FindObjectOfType<GrowlGameStateBridge>();

        if (_tickManager != null)
            _tickManager.OnTickAdvanced += OnTick;
        else
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

            // Retrieve or create persistent biological context for this organism
            // and pass it to the bridge for event queueing
            BiologicalContext bioContext;
            const string bioContextKey = "_bio_context";
            if (org.TryGetMemoryValue(bioContextKey, out object existing) && existing is BiologicalContext ctx)
            {
                bioContext = ctx;
            }
            else
            {
                bioContext = new BiologicalContext();
                org.SetMemoryValue(bioContextKey, bioContext);
            }
            bioContext.CurrentTick = tick;
            _bridge.SetBioContext(bioContext);

            RuntimeResult result = GrowlRuntime.Execute(org.GrowlSource, new RuntimeOptions
            {
                AutoInvokeEntryFunction = true,
                EntryFunctionName = _entryFunctionName,
                MaxLoopIterations = _maxLoopIterations,
                Host = _bridge,
                BioContext = bioContext,
            });

            for (int j = 0; j < result.OutputLines.Count; j++)
                Debug.Log($"[{org.OrganismName}] {result.OutputLines[j]}", org);

            if (!result.Success)
            {
                for (int m = 0; m < result.Messages.Count; m++)
                    Debug.LogWarning($"[GeneExec] {org.OrganismName}: {result.Messages[m]}", org);
            }
        }
    }
}
