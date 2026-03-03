using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the reproduce module from the API spec.
/// Handles seed production, mutation, vegetative reproduction, and lifecycle.
/// All operations go through OrganismEntity for state and SeedInventory for seeds.
/// </summary>
public static class ReproduceModule
{
    private const float SeedBaseEnergyCost = 2f;
    private const float MutationEnergyCost = 0.3f;
    private const float CrossbreedEnergyCost = 3f;
    private const float CloneEnergyCost = 5f;
    private const float FragmentEnergyCost = 2f;

    // ── Seed Production ──────────────────────────────────────────────

    /// <summary>reproduce.generate_seeds(count, energy_per_seed) — produce seeds from genome.</summary>
    public static List<Dictionary<string, object>> GenerateSeeds(
        OrganismEntity org, SeedInventory seedInventory, int count, float energyPerSeed)
    {
        if (count <= 0) count = 1;
        energyPerSeed = Mathf.Max(0.5f, energyPerSeed);

        float totalCost = count * (SeedBaseEnergyCost + energyPerSeed);
        if (!TrySpendEnergy(org, totalCost)) return null;

        var seeds = new List<Dictionary<string, object>>(count);
        for (int i = 0; i < count; i++)
        {
            var seed = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["parent"] = org.OrganismName,
                ["energy"] = (double)energyPerSeed,
                ["genome"] = org.GrowlSource,
                ["generation"] = GetGeneration(org) + 1L,
                ["viable"] = true,
            };

            // Copy mutation settings if any
            if (org.TryGetMemoryValue("_reproduce_variance", out object variance))
                seed["variance"] = variance;

            seeds.Add(seed);

            // Register with inventory
            if (seedInventory != null)
                seedInventory.TryAddSeedValue("count", 1, out _, out _);
        }

        return seeds;
    }

    /// <summary>reproduce.set_dispersal(method, params) — set seed dispersal method.</summary>
    public static bool SetDispersal(OrganismEntity org, string method, object dispersalParams)
    {
        if (string.IsNullOrEmpty(method)) return false;

        string normalized = method.Trim().ToLowerInvariant();
        org.SetMemoryValue("_reproduce_dispersal_method", normalized);

        if (dispersalParams != null)
            org.SetMemoryValue("_reproduce_dispersal_params", dispersalParams);

        return true;
    }

    /// <summary>reproduce.set_germination(conditions) — define seed germination requirements.</summary>
    public static bool SetGermination(OrganismEntity org, object conditions)
    {
        if (conditions == null) return false;
        org.SetMemoryValue("_reproduce_germination", conditions);
        return true;
    }

    // ── Mutation ─────────────────────────────────────────────────────

    /// <summary>reproduce.mutate(variance) — child seeds get random variations.</summary>
    public static bool Mutate(OrganismEntity org, float variance)
    {
        variance = Mathf.Clamp01(variance);
        if (!TrySpendEnergy(org, MutationEnergyCost)) return false;

        org.SetMemoryValue("_reproduce_variance", (double)variance);
        org.SetMemoryValue("_reproduce_mutate_all", true);
        return true;
    }

    /// <summary>reproduce.mutate_gene(slot_name, variance) — mutate only a specific gene.</summary>
    public static bool MutateGene(OrganismEntity org, string slotName, float variance)
    {
        if (string.IsNullOrEmpty(slotName)) return false;
        variance = Mathf.Clamp01(variance);
        if (!TrySpendEnergy(org, MutationEnergyCost * 0.5f)) return false;

        string key = "_reproduce_mutate_gene_" + slotName.Trim().ToLowerInvariant();
        org.SetMemoryValue(key, (double)variance);
        return true;
    }

    /// <summary>reproduce.crossbreed(other_org) — combine genomes with neighbor.</summary>
    public static Dictionary<string, object> Crossbreed(OrganismEntity org, string otherOrgName)
    {
        if (string.IsNullOrEmpty(otherOrgName)) return null;
        if (!TrySpendEnergy(org, CrossbreedEnergyCost)) return null;

        // Check maturity
        if (!org.TryGetState("maturity", out object matVal) || !TryConvertToDouble(matVal, out double maturity))
            maturity = 0d;
        if (maturity < 0.5) return null;

        var hybridSeed = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["parent_a"] = org.OrganismName,
            ["parent_b"] = otherOrgName,
            ["genome"] = org.GrowlSource, // base genome from this organism
            ["hybrid"] = true,
            ["generation"] = GetGeneration(org) + 1L,
            ["energy"] = 3.0,
            ["viable"] = true,
        };

        return hybridSeed;
    }

    // ── Vegetative Reproduction ──────────────────────────────────────

    /// <summary>reproduce.clone(direction) — grow a genetic copy.</summary>
    public static object Clone(OrganismEntity org, string direction)
    {
        if (!TrySpendEnergy(org, CloneEnergyCost)) return null;

        // Check maturity
        if (!org.TryGetState("maturity", out object matVal) || !TryConvertToDouble(matVal, out double maturity))
            maturity = 0d;
        if (maturity < 0.3) return null;

        var cloneInfo = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["parent"] = org.OrganismName,
            ["type"] = "clone",
            ["direction"] = direction ?? "auto",
            ["genome"] = org.GrowlSource,
            ["generation"] = GetGeneration(org),
            ["connected"] = true, // shares parent root network
        };

        return cloneInfo;
    }

    /// <summary>reproduce.fragment(part_name) — detach body part as new organism.</summary>
    public static object Fragment(PlantBody body, OrganismEntity org, string partName)
    {
        if (string.IsNullOrEmpty(partName)) return null;

        PlantPart part = body.FindPart(partName);
        if (part == null) return null;
        if (!TrySpendEnergy(org, FragmentEnergyCost)) return null;

        var fragmentInfo = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["parent"] = org.OrganismName,
            ["type"] = "fragment",
            ["source_part"] = partName,
            ["size"] = (double)part.Size,
            ["genome"] = org.GrowlSource,
            ["generation"] = GetGeneration(org),
        };

        // Remove the part from the parent
        body.RemovePart(partName);

        return fragmentInfo;
    }

    // ── Lifecycle ────────────────────────────────────────────────────

    /// <summary>reproduce.set_lifecycle(type) — set organism lifecycle type.</summary>
    public static bool SetLifecycle(OrganismEntity org, string type)
    {
        if (string.IsNullOrEmpty(type)) return false;

        string normalized = type.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "annual":
            case "perennial":
            case "ephemeral":
            case "immortal":
                org.SetMemoryValue("_reproduce_lifecycle", normalized);
                return true;
            default:
                return false;
        }
    }

    /// <summary>reproduce.set_maturity_age(ticks) — ticks before organism can reproduce.</summary>
    public static bool SetMaturityAge(OrganismEntity org, int ticks)
    {
        if (ticks < 0) ticks = 0;
        org.SetMemoryValue("_reproduce_maturity_age", (long)ticks);
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static long GetGeneration(OrganismEntity org)
    {
        if (org.TryGetMemoryValue("_generation", out object gen) && TryConvertToDouble(gen, out double g))
            return (long)Math.Round(g);
        return 0L;
    }

    private static bool TrySpendEnergy(OrganismEntity org, float cost)
    {
        if (!org.TryGetState("energy", out object eVal)) return false;
        if (!TryConvertToDouble(eVal, out double energy)) return false;
        if (energy < cost) return false;
        org.TryAddState("energy", -cost, out _, out _);
        return true;
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
}
