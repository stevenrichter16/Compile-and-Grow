using System.Collections.Generic;
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

        // Deliver signals emitted last tick to nearby organisms
        if (tick > 1)
            DeliverCrossOrganismSignals(tick - 1, organisms);

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
                EntryClassName = org.EntryClassName,
                MaxLoopIterations = _maxLoopIterations,
                Host = _bridge,
                BioContext = bioContext,
            });

            for (int j = 0; j < result.OutputLines.Count; j++)
                Debug.Log($"[{org.OrganismName}] {result.OutputLines[j]}", org);

            // Deduct memory maintenance cost: 0.1 energy per non-internal key per tick
            int memoryKeys = 0;
            foreach (string key in org.Memory.Keys)
            {
                if (!key.StartsWith("_"))
                    memoryKeys++;
            }
            if (memoryKeys > 0)
                org.TryAddState("energy", -0.1 * memoryKeys, out _, out _);

            if (!result.Success)
            {
                for (int m = 0; m < result.Messages.Count; m++)
                {
                    Debug.LogWarning($"[GeneExec] {org.OrganismName}: {result.Messages[m]}", org);
                    ErrorMutationClassifier.ApplyMutation(org, result.Messages[m]);
                }
            }
        }
    }

    private void DeliverCrossOrganismSignals(long emitTick, OrganismEntity[] organisms)
    {
        if (_tickManager == null)
            return;

        List<GrowthTickManager.SignalRecord> signals = _tickManager.GetSignalsFromTick(emitTick);
        if (signals.Count == 0)
            return;

        const string bioContextKey = "_bio_context";

        for (int s = 0; s < signals.Count; s++)
        {
            GrowthTickManager.SignalRecord sig = signals[s];

            for (int o = 0; o < organisms.Length; o++)
            {
                OrganismEntity org = organisms[o];
                if (!org.IsAlive)
                    continue;

                // Skip self — emitter already received its own signal at emit time
                if (string.Equals(org.OrganismName, sig.sender, System.StringComparison.Ordinal))
                    continue;

                float dist = Vector3.Distance(org.transform.position, sig.senderPosition);
                if (dist > sig.radius)
                    continue;

                // Get or create bio context for this organism
                BiologicalContext bioCtx;
                if (org.TryGetMemoryValue(bioContextKey, out object existing) && existing is BiologicalContext ctx)
                {
                    bioCtx = ctx;
                }
                else
                {
                    bioCtx = new BiologicalContext();
                    org.SetMemoryValue(bioContextKey, bioCtx);
                }

                Vector3 dir = dist > 0.001f ? (sig.senderPosition - org.transform.position).normalized : Vector3.zero;

                var eventData = new Dictionary<string, object>(System.StringComparer.Ordinal)
                {
                    ["type"] = sig.type,
                    ["intensity"] = (double)sig.intensity,
                    ["distance"] = (double)dist,
                    ["direction"] = new GrowlVector(dir.x, dir.y, dir.z),
                    ["sender"] = sig.sender,
                    ["data"] = null,
                };

                bioCtx.QueueEvent(sig.type, eventData);
            }
        }
    }
}
