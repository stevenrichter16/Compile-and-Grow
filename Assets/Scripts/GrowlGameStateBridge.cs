using System;
using System.Collections;
using System.Collections.Generic;
using GrowlLanguage.Runtime;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GrowlGameStateBridge : MonoBehaviour, IGrowlRuntimeHost
{
    [Header("Gameplay Systems")]
    [SerializeField] private ResourceGrid resourceGrid;
    [SerializeField] private GrowthTickManager growthTickManager;
    [SerializeField] private OrganismEntity organismEntity;
    [SerializeField] private SeedInventory seedInventory;

    [SerializeField] private bool autoAttachMissingSystems = true;

    private StateBackedDictionary _worldProxy;
    private StateBackedDictionary _orgProxy;
    private StateBackedDictionary _seedProxy;

    private void Awake()
    {
        EnsureSystems();
        EnsureProxies();
    }

    public void SetOrganism(OrganismEntity target)
    {
        organismEntity = target;
    }

    public void PopulateGlobals(IDictionary<string, object> globals)
    {
        EnsureSystems();
        if (!TryValidateSystems(out string error))
        {
            Debug.LogError(error, this);
            return;
        }

        EnsureProxies();

        globals["world"] = _worldProxy;
        globals["org"] = _orgProxy;
        globals["seed"] = _seedProxy;
        globals["env"] = _worldProxy;
    }

    public bool TryInvokeBuiltin(
        string builtinName,
        IReadOnlyList<RuntimeCallArgument> args,
        out object result,
        out string errorMessage)
    {
        EnsureSystems();
        if (!TryValidateSystems(out errorMessage))
        {
            result = null;
            return false;
        }

        result = null;
        errorMessage = null;

        switch (builtinName)
        {
            case "world_get":
                return TryWorldGetBuiltin(args, out result, out errorMessage);

            case "world_set":
                return TryWorldSetBuiltin(args, out result, out errorMessage);

            case "world_add":
                return TryWorldAddBuiltin(args, out result, out errorMessage);

            case "org_get":
                return TryOrgGetBuiltin(args, out result, out errorMessage);

            case "org_set":
                return TryOrgSetBuiltin(args, out result, out errorMessage);

            case "org_add":
                return TryOrgAddBuiltin(args, out result, out errorMessage);

            case "org_damage":
                return TryOrgDamageBuiltin(args, out result, out errorMessage);

            case "org_heal":
                return TryOrgHealBuiltin(args, out result, out errorMessage);

            case "org_memory_get":
                return TryOrgMemoryGetBuiltin(args, out result, out errorMessage);

            case "org_memory_set":
                return TryOrgMemorySetBuiltin(args, out result, out errorMessage);

            case "seed_get":
                return TrySeedGetBuiltin(args, out result, out errorMessage);

            case "seed_set":
                return TrySeedSetBuiltin(args, out result, out errorMessage);

            case "seed_add":
                return TrySeedAddBuiltin(args, out result, out errorMessage);

            case "emit_signal":
                return TryEmitSignalBuiltin(args, out result, out errorMessage);

            case "spawn_seed":
                return TrySpawnSeedBuiltin(args, out result, out errorMessage);

            // ── morph module builtins ──
            case "morph_create_part":
                return TryMorphCreatePartBuiltin(args, out result, out errorMessage);

            case "morph_remove_part":
                return TryMorphRemovePartBuiltin(args, out result, out errorMessage);

            case "morph_attach":
                return TryMorphAttachBuiltin(args, out result, out errorMessage);

            case "morph_grow_part":
                return TryMorphGrowPartBuiltin(args, out result, out errorMessage);

            case "morph_shrink_part":
                return TryMorphShrinkPartBuiltin(args, out result, out errorMessage);

            case "morph_set_symmetry":
                return TryMorphSetSymmetryBuiltin(args, out result, out errorMessage);

            case "morph_set_growth_pattern":
                return TryMorphSetGrowthPatternBuiltin(args, out result, out errorMessage);

            case "morph_set_surface":
                return TryMorphSetSurfaceBuiltin(args, out result, out errorMessage);

            case "morph_emit_light":
                return TryMorphEmitLightBuiltin(args, out result, out errorMessage);

            // ── parts query builtins ──
            case "parts_find":
                return TryPartsFindBuiltin(args, out result, out errorMessage);

            case "parts_find_type":
                return TryPartsFindTypeBuiltin(args, out result, out errorMessage);

            case "parts_count":
                return TryPartsCountBuiltin(args, out result, out errorMessage);

            default:
                errorMessage = "Unknown game-state builtin '" + builtinName + "'.";
                return false;
        }
    }

    private bool TryWorldGetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (TryWorldGet(key, out result))
        {
            errorMessage = null;
            return true;
        }

        if (TryGetArg(args, index: 1, name: "default", out RuntimeCallArgument fallback))
        {
            result = fallback.Value;
            errorMessage = null;
            return true;
        }

        result = null;
        errorMessage = null;
        return true;
    }

    private bool TryWorldSetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryGetArg(args, index: 1, name: "value", out RuntimeCallArgument valueArg))
        {
            result = null;
            errorMessage = "Expected value argument.";
            return false;
        }

        if (!TryWorldSet(key, valueArg.Value, out errorMessage))
        {
            result = null;
            return false;
        }

        result = valueArg.Value;
        return true;
    }

    private bool TryWorldAddBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadNumber(args, index: 1, name: "delta", out double delta, out errorMessage))
        {
            result = null;
            return false;
        }

        return TryWorldAdd(key, delta, out result, out errorMessage);
    }

    private bool TryOrgGetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (organismEntity.TryGetState(key, out result))
        {
            errorMessage = null;
            return true;
        }

        if (TryGetArg(args, index: 1, name: "default", out RuntimeCallArgument fallback))
        {
            result = fallback.Value;
            errorMessage = null;
            return true;
        }

        result = null;
        errorMessage = null;
        return true;
    }

    private bool TryOrgSetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryGetArg(args, index: 1, name: "value", out RuntimeCallArgument valueArg))
        {
            result = null;
            errorMessage = "Expected value argument.";
            return false;
        }

        if (!organismEntity.TrySetState(key, valueArg.Value, out errorMessage))
        {
            result = null;
            return false;
        }

        result = valueArg.Value;
        return true;
    }

    private bool TryOrgAddBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadNumber(args, index: 1, name: "delta", out double delta, out errorMessage))
        {
            result = null;
            return false;
        }

        return organismEntity.TryAddState(key, delta, out result, out errorMessage);
    }

    private bool TryOrgDamageBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadNumber(args, index: 0, name: "amount", out double amount, out errorMessage))
        {
            result = null;
            return false;
        }

        result = organismEntity.ApplyDamage(amount);
        errorMessage = null;
        return true;
    }

    private bool TryOrgHealBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadNumber(args, index: 0, name: "amount", out double amount, out errorMessage))
        {
            result = null;
            return false;
        }

        result = organismEntity.ApplyHeal(amount);
        errorMessage = null;
        return true;
    }

    private bool TryOrgMemoryGetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (organismEntity.TryGetMemoryValue(key, out result))
        {
            errorMessage = null;
            return true;
        }

        if (TryGetArg(args, index: 1, name: "default", out RuntimeCallArgument fallback))
        {
            result = fallback.Value;
            errorMessage = null;
            return true;
        }

        result = null;
        errorMessage = null;
        return true;
    }

    private bool TryOrgMemorySetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryGetArg(args, index: 1, name: "value", out RuntimeCallArgument valueArg))
        {
            result = null;
            errorMessage = "Expected value argument.";
            return false;
        }

        organismEntity.SetMemoryValue(key, valueArg.Value);
        result = valueArg.Value;
        errorMessage = null;
        return true;
    }

    private bool TrySeedGetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (seedInventory.TryGetSeedValue(key, out result))
        {
            errorMessage = null;
            return true;
        }

        if (TryGetArg(args, index: 1, name: "default", out RuntimeCallArgument fallback))
        {
            result = fallback.Value;
            errorMessage = null;
            return true;
        }

        result = null;
        errorMessage = null;
        return true;
    }

    private bool TrySeedSetBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryGetArg(args, index: 1, name: "value", out RuntimeCallArgument valueArg))
        {
            result = null;
            errorMessage = "Expected value argument.";
            return false;
        }

        if (!seedInventory.TrySetSeedValue(key, valueArg.Value, out errorMessage))
        {
            result = null;
            return false;
        }

        result = valueArg.Value;
        return true;
    }

    private bool TrySeedAddBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadKey(args, out string key, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadNumber(args, index: 1, name: "delta", out double delta, out errorMessage))
        {
            result = null;
            return false;
        }

        return seedInventory.TryAddSeedValue(key, delta, out result, out errorMessage);
    }

    private bool TryEmitSignalBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "type", out string signalType, out errorMessage))
        {
            result = null;
            return false;
        }

        double intensity = TryReadNumberOptional(args, index: 1, name: "intensity", defaultValue: 1d);
        double radius = TryReadNumberOptional(args, index: 2, name: "radius", defaultValue: 1d);

        result = growthTickManager.EmitSignal(signalType, intensity, radius, organismEntity.OrganismName);
        errorMessage = null;
        return true;
    }

    private bool TrySpawnSeedBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        long count = TryReadIntegerOptional(args, index: 0, name: "count", defaultValue: 1L);
        if (count < 0)
            count = 0;

        result = seedInventory.SpawnSeeds(count);
        growthTickManager.AddSpawnedSeeds(count);
        errorMessage = null;
        return true;
    }

    // ── Morph module builtins ──────────────────────────────────────

    private PlantBody GetOrCreateBody()
    {
        return organismEntity.Body;
    }

    // morph_create_part(name, type, size=1, energy_cost=0.1)
    private bool TryMorphCreatePartBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "name", out string name, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadString(args, index: 1, name: "type", out string type, out errorMessage))
        {
            result = null;
            return false;
        }

        float size = (float)TryReadNumberOptional(args, index: 2, name: "size", defaultValue: 1d);
        float energyCost = (float)TryReadNumberOptional(args, index: 3, name: "energy_cost", defaultValue: 0.1d);

        PlantBody body = GetOrCreateBody();
        PlantPart part = body.CreatePart(name, type, size, energyCost);
        if (part == null)
        {
            errorMessage = "Failed to create part '" + name + "' (may already exist).";
            result = null;
            return false;
        }

        // Apply any additional properties passed via keyword args beyond the positional ones
        for (int i = 0; i < args.Count; i++)
        {
            string argName = args[i].Name;
            if (string.IsNullOrEmpty(argName)) continue;
            if (argName == "name" || argName == "type" || argName == "size" || argName == "energy_cost") continue;
            part.TrySetProperty(argName, args[i].Value);
        }

        result = part.CreateSnapshot();
        errorMessage = null;
        return true;
    }

    // morph_remove_part(name)
    private bool TryMorphRemovePartBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "name", out string name, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        bool removed = body.RemovePart(name);
        result = removed;
        errorMessage = null;
        return true;
    }

    // morph_attach(part_name, to_part, position="tip")
    private bool TryMorphAttachBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "part", out string partName, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadString(args, index: 1, name: "to_part", out string toPartName, out errorMessage))
        {
            result = null;
            return false;
        }

        string position = "tip";
        if (TryGetArg(args, index: 2, name: "position", out RuntimeCallArgument posArg) && posArg.Value != null)
            position = posArg.Value.ToString();

        PlantBody body = GetOrCreateBody();
        bool attached = body.AttachPart(partName, toPartName, position);
        result = attached;
        errorMessage = null;
        return true;
    }

    // morph_grow_part(part_name, property, amount)
    private bool TryMorphGrowPartBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "part_name", out string partName, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadString(args, index: 1, name: "property", out string property, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadNumber(args, index: 2, name: "amount", out double amount, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        bool grown = body.GrowPart(partName, property, amount);
        result = grown;
        errorMessage = null;
        return true;
    }

    // morph_shrink_part(part_name, property, amount)
    private bool TryMorphShrinkPartBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "part_name", out string partName, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadString(args, index: 1, name: "property", out string property, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryReadNumber(args, index: 2, name: "amount", out double amount, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        bool shrunk = body.ShrinkPart(partName, property, amount);
        result = shrunk;
        errorMessage = null;
        return true;
    }

    // morph_set_symmetry(type)
    private bool TryMorphSetSymmetryBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "type", out string symType, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        if (!body.TrySetMorphology("symmetry", symType, out errorMessage))
        {
            result = null;
            return false;
        }

        result = symType;
        return true;
    }

    // morph_set_growth_pattern(type)
    private bool TryMorphSetGrowthPatternBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "type", out string patternType, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        if (!body.TrySetMorphology("growth_pattern", patternType, out errorMessage))
        {
            result = null;
            return false;
        }

        result = patternType;
        return true;
    }

    // morph_set_surface(part_name, properties_dict)
    private bool TryMorphSetSurfaceBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "part_name", out string partName, out errorMessage))
        {
            result = null;
            return false;
        }

        if (!TryGetArg(args, index: 1, name: "properties", out RuntimeCallArgument propsArg))
        {
            result = null;
            errorMessage = "Expected properties argument.";
            return false;
        }

        PlantBody body = GetOrCreateBody();

        if (propsArg.Value is IDictionary dict)
        {
            body.SetSurface(partName, dict);
            result = true;
        }
        else
        {
            // If a single key-value was passed, apply keyword args as surface properties
            PlantPart part = body.FindPart(partName);
            if (part == null)
            {
                result = false;
                errorMessage = "Part '" + partName + "' not found.";
                return false;
            }

            for (int i = 1; i < args.Count; i++)
            {
                if (!string.IsNullOrEmpty(args[i].Name) && args[i].Name != "part_name")
                    part.TrySetProperty("surface_" + args[i].Name.Trim().ToLowerInvariant(), args[i].Value);
            }

            result = true;
        }

        errorMessage = null;
        return true;
    }

    // morph_emit_light(intensity, color_r, color_g, color_b, part_name=None)
    private bool TryMorphEmitLightBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadNumber(args, index: 0, name: "intensity", out double intensity, out errorMessage))
        {
            result = null;
            return false;
        }

        double r = TryReadNumberOptional(args, index: 1, name: "color_r", defaultValue: 255d);
        double g = TryReadNumberOptional(args, index: 2, name: "color_g", defaultValue: 255d);
        double b = TryReadNumberOptional(args, index: 3, name: "color_b", defaultValue: 255d);

        string partName = null;
        if (TryGetArg(args, index: 4, name: "part_name", out RuntimeCallArgument partArg) && partArg.Value != null)
            partName = partArg.Value.ToString();

        PlantBody body = GetOrCreateBody();

        if (!string.IsNullOrEmpty(partName))
        {
            PlantPart part = body.FindPart(partName);
            if (part != null)
            {
                part.TrySetProperty("bioluminescence", intensity);
                part.TrySetProperty("biolum_color_r", r);
                part.TrySetProperty("biolum_color_g", g);
                part.TrySetProperty("biolum_color_b", b);
            }
        }
        else
        {
            body.TrySetMorphology("opacity", 1.0, out _);
        }

        result = intensity;
        errorMessage = null;
        return true;
    }

    // ── Parts query builtins ────────────────────────────────────────

    // parts_find(name) -> part snapshot or null
    private bool TryPartsFindBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "name", out string name, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        PlantPart part = body.FindPart(name);
        result = part?.CreateSnapshot();
        errorMessage = null;
        return true;
    }

    // parts_find_type(type) -> list of part snapshots
    private bool TryPartsFindTypeBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "type", out string type, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        List<PlantPart> parts = body.FindPartsByType(type);
        var snapshots = new List<object>(parts.Count);
        for (int i = 0; i < parts.Count; i++)
            snapshots.Add(parts[i].CreateSnapshot());

        result = snapshots;
        errorMessage = null;
        return true;
    }

    // parts_count(type) -> long
    private bool TryPartsCountBuiltin(IReadOnlyList<RuntimeCallArgument> args, out object result, out string errorMessage)
    {
        if (!TryReadString(args, index: 0, name: "type", out string type, out errorMessage))
        {
            result = null;
            return false;
        }

        PlantBody body = GetOrCreateBody();
        result = (long)body.CountPartsByType(type);
        errorMessage = null;
        return true;
    }

    // ── World state helpers ─────────────────────────────────────────

    private bool TryWorldGet(string key, out object value)
    {
        if (IsTickManagedWorldKey(key))
        {
            if (growthTickManager.TryGetWorldValue(key, out value))
                return true;

            return resourceGrid.TryGetWorldValue(key, out value);
        }

        if (resourceGrid.TryGetWorldValue(key, out value))
            return true;

        return growthTickManager.TryGetWorldValue(key, out value);
    }

    private bool TryWorldSet(string key, object value, out string errorMessage)
    {
        if (IsTickManagedWorldKey(key))
            return growthTickManager.TrySetWorldValue(key, value, out errorMessage);

        return resourceGrid.TrySetWorldValue(key, value, out errorMessage);
    }

    private bool TryWorldAdd(string key, double delta, out object result, out string errorMessage)
    {
        if (IsTickManagedWorldKey(key))
            return growthTickManager.TryAddWorldValue(key, delta, out result, out errorMessage);

        return resourceGrid.TryAddWorldValue(key, delta, out result, out errorMessage);
    }

    private Dictionary<string, object> BuildWorldSnapshot()
    {
        var snapshot = resourceGrid.CreateWorldSnapshot();
        Dictionary<string, object> tickSnapshot = growthTickManager.CreateWorldSnapshot();
        foreach (KeyValuePair<string, object> pair in tickSnapshot)
            snapshot[pair.Key] = pair.Value;
        return snapshot;
    }

    private Dictionary<string, object> BuildOrgSnapshot()
    {
        return organismEntity.CreateStateSnapshot();
    }

    private Dictionary<string, object> BuildSeedSnapshot()
    {
        return seedInventory.CreateSeedSnapshot();
    }

    private object GetWorldProxyValue(string key)
    {
        TryWorldGet(key, out object value);
        return value;
    }

    private object GetOrgProxyValue(string key)
    {
        organismEntity.TryGetState(key, out object value);
        return value;
    }

    private object GetSeedProxyValue(string key)
    {
        seedInventory.TryGetSeedValue(key, out object value);
        return value;
    }

    private void SetWorldProxyValue(string key, object value)
    {
        if (!TryWorldSet(key, value, out string error) && !string.IsNullOrEmpty(error))
            Debug.LogWarning(error, this);
    }

    private void SetOrgProxyValue(string key, object value)
    {
        if (!organismEntity.TrySetState(key, value, out string error) && !string.IsNullOrEmpty(error))
            Debug.LogWarning(error, this);
    }

    private void SetSeedProxyValue(string key, object value)
    {
        if (!seedInventory.TrySetSeedValue(key, value, out string error) && !string.IsNullOrEmpty(error))
            Debug.LogWarning(error, this);
    }

    private void EnsureSystems()
    {
        resourceGrid = ResolveSystem(resourceGrid);
        growthTickManager = ResolveSystem(growthTickManager);
        organismEntity = ResolveSystem(organismEntity);
        seedInventory = ResolveSystem(seedInventory);
    }

    private bool TryValidateSystems(out string errorMessage)
    {
        if (resourceGrid == null)
        {
            errorMessage = "GrowlGameStateBridge requires ResourceGrid.";
            return false;
        }

        if (growthTickManager == null)
        {
            errorMessage = "GrowlGameStateBridge requires GrowthTickManager.";
            return false;
        }

        if (organismEntity == null)
        {
            errorMessage = "GrowlGameStateBridge requires OrganismEntity.";
            return false;
        }

        if (seedInventory == null)
        {
            errorMessage = "GrowlGameStateBridge requires SeedInventory.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private T ResolveSystem<T>(T current) where T : Component
    {
        if (current != null)
            return current;

        current = GetComponent<T>();
        if (current != null)
            return current;

        current = FindObjectOfType<T>();
        if (current != null)
            return current;

        if (autoAttachMissingSystems)
            return gameObject.AddComponent<T>();

        return null;
    }

    private void EnsureProxies()
    {
        if (_worldProxy == null)
        {
            _worldProxy = new StateBackedDictionary(
                BuildWorldSnapshot,
                GetWorldProxyValue,
                SetWorldProxyValue);
        }

        if (_orgProxy == null)
        {
            _orgProxy = new StateBackedDictionary(
                BuildOrgSnapshot,
                GetOrgProxyValue,
                SetOrgProxyValue);
        }

        if (_seedProxy == null)
        {
            _seedProxy = new StateBackedDictionary(
                BuildSeedSnapshot,
                GetSeedProxyValue,
                SetSeedProxyValue);
        }
    }

    private static bool IsTickManagedWorldKey(string key)
    {
        string normalized = NormalizeKey(key);
        return normalized == "tick" ||
               normalized == "signal_count" ||
               normalized == "last_signal" ||
               normalized == "total_spawned_seeds" ||
               normalized == "signals";
    }

    private static string NormalizeKey(string key)
    {
        return (key ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool TryReadKey(
        IReadOnlyList<RuntimeCallArgument> args,
        out string key,
        out string errorMessage)
    {
        if (!TryGetArg(args, index: 0, name: "key", out RuntimeCallArgument arg))
        {
            key = null;
            errorMessage = "Expected key argument.";
            return false;
        }

        if (arg.Value == null)
        {
            key = null;
            errorMessage = "Key cannot be none.";
            return false;
        }

        key = arg.Value.ToString();
        errorMessage = null;
        return true;
    }

    private static bool TryReadString(
        IReadOnlyList<RuntimeCallArgument> args,
        int index,
        string name,
        out string value,
        out string errorMessage)
    {
        if (!TryGetArg(args, index, name, out RuntimeCallArgument arg))
        {
            value = null;
            errorMessage = "Expected argument '" + name + "'.";
            return false;
        }

        value = arg.Value == null ? null : arg.Value.ToString();
        if (string.IsNullOrEmpty(value))
        {
            errorMessage = "Argument '" + name + "' cannot be empty.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool TryReadNumber(
        IReadOnlyList<RuntimeCallArgument> args,
        int index,
        string name,
        out double value,
        out string errorMessage)
    {
        if (!TryGetArg(args, index, name, out RuntimeCallArgument arg))
        {
            value = 0d;
            errorMessage = "Expected argument '" + name + "'.";
            return false;
        }

        if (!TryConvertToDouble(arg.Value, out value))
        {
            errorMessage = "Argument '" + name + "' must be numeric.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static double TryReadNumberOptional(
        IReadOnlyList<RuntimeCallArgument> args,
        int index,
        string name,
        double defaultValue)
    {
        if (!TryGetArg(args, index, name, out RuntimeCallArgument arg))
            return defaultValue;

        return TryConvertToDouble(arg.Value, out double value) ? value : defaultValue;
    }

    private static long TryReadIntegerOptional(
        IReadOnlyList<RuntimeCallArgument> args,
        int index,
        string name,
        long defaultValue)
    {
        if (!TryGetArg(args, index, name, out RuntimeCallArgument arg))
            return defaultValue;

        return TryConvertToDouble(arg.Value, out double value)
            ? (long)Math.Round(value)
            : defaultValue;
    }

    private static bool TryGetArg(
        IReadOnlyList<RuntimeCallArgument> args,
        int index,
        string name,
        out RuntimeCallArgument argument)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.IsNullOrEmpty(args[i].Name) && args[i].Name == name)
            {
                argument = args[i];
                return true;
            }
        }

        int positionalIndex = 0;
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.IsNullOrEmpty(args[i].Name))
                continue;

            if (positionalIndex == index)
            {
                argument = args[i];
                return true;
            }

            positionalIndex++;
        }

        argument = default;
        return false;
    }

    private static bool TryConvertToDouble(object value, out double number)
    {
        switch (value)
        {
            case sbyte v: number = v; return true;
            case byte v: number = v; return true;
            case short v: number = v; return true;
            case ushort v: number = v; return true;
            case int v: number = v; return true;
            case uint v: number = v; return true;
            case long v: number = v; return true;
            case ulong v: number = v; return true;
            case float v: number = v; return true;
            case double v: number = v; return true;
            case decimal v: number = (double)v; return true;
            default:
                number = 0d;
                return false;
        }
    }

    private sealed class StateBackedDictionary : IDictionary
    {
        private readonly Func<Dictionary<string, object>> _snapshotFactory;
        private readonly Func<string, object> _getter;
        private readonly Action<string, object> _setter;

        public StateBackedDictionary(
            Func<Dictionary<string, object>> snapshotFactory,
            Func<string, object> getter,
            Action<string, object> setter)
        {
            _snapshotFactory = snapshotFactory;
            _getter = getter;
            _setter = setter;
        }

        public object this[object key]
        {
            get
            {
                string normalized = KeyToString(key);
                return _getter(normalized);
            }
            set
            {
                string normalized = KeyToString(key);
                _setter(normalized, value);
            }
        }

        public ICollection Keys => ((IDictionary)Snapshot()).Keys;
        public ICollection Values => ((IDictionary)Snapshot()).Values;
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;
        public int Count => Snapshot().Count;
        public object SyncRoot => this;
        public bool IsSynchronized => false;

        public void Add(object key, object value)
        {
            this[key] = value;
        }

        public void Clear()
        {
            throw new NotSupportedException("Clear is not supported for state-backed dictionaries.");
        }

        public bool Contains(object key)
        {
            return Snapshot().ContainsKey(KeyToString(key));
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return ((IDictionary)Snapshot()).GetEnumerator();
        }

        public void Remove(object key)
        {
            string normalized = KeyToString(key);
            _setter(normalized, null);
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)Snapshot()).CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private Dictionary<string, object> Snapshot()
        {
            return _snapshotFactory != null
                ? _snapshotFactory()
                : new Dictionary<string, object>(StringComparer.Ordinal);
        }

        private static string KeyToString(object key)
        {
            return key == null ? string.Empty : key.ToString();
        }
    }
}
