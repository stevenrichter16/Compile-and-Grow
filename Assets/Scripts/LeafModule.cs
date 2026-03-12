using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the leaf module from the API spec.
/// Handles surface organs: light-catching, gas exchange, orientation, properties, and absorption.
/// </summary>
public static class LeafModule
{
    private const string LeafType = "leaf";
    internal const float GrowthEnergyCostPerCm2 = 0.3f;
    internal const float GrowthGlucoseCostMultiplier = 0.4f;

    // ── Growth ──────────────────────────────────────────────────────

    /// <summary>leaf.grow(area, from_part) — create or expand leaf surface.</summary>
    public static object Grow(PlantBody body, OrganismEntity org, float area, string fromPart)
    {
        if (area <= 0f) return null;
        float energyCost = area * GrowthEnergyCostPerCm2;
        float glucoseCost = energyCost * GrowthGlucoseCostMultiplier;
        if (!TrySpendGrowthResources(org, energyCost, glucoseCost)) return null;

        // Inherit stomata from existing leaves before creating the new one
        float inheritedStomata = GetAverageStomataOpenness(body);
        if (body.FindPartsByType(LeafType).Count == 0)
            inheritedStomata = 0.5f;

        // Find parent branch/stem to attach to
        PlantPart parent = null;
        if (!string.IsNullOrEmpty(fromPart))
            parent = body.FindPart(fromPart);
        if (parent == null)
            parent = body.FindPart("stem_main");

        string name = "leaf_" + (body.CountPartsByType(LeafType) + 1);
        PlantPart leaf = body.CreatePart(name, LeafType, area, area * 0.02f);
        if (leaf == null) return null;

        leaf.TrySetProperty("shape", "flat");
        leaf.TrySetProperty("stomata_openness", (double)inheritedStomata);
        leaf.TrySetProperty("track_light", false);
        leaf.TrySetProperty("coating", "none");

        if (parent != null)
            body.AttachPart(name, parent.Name);

        return leaf.CreateSnapshot();
    }

    /// <summary>leaf.grow_count(number, size_each, from_part) — grow multiple leaves.</summary>
    public static List<Dictionary<string, object>> GrowCount(PlantBody body, OrganismEntity org, int number, float sizeEach, string fromPart)
    {
        if (number <= 0 || sizeEach <= 0f) return null;
        float totalEnergyCost = number * sizeEach * GrowthEnergyCostPerCm2;
        float totalGlucoseCost = totalEnergyCost * GrowthGlucoseCostMultiplier;
        if (!TrySpendGrowthResources(org, totalEnergyCost, totalGlucoseCost)) return null;

        PlantPart parent = null;
        if (!string.IsNullOrEmpty(fromPart))
            parent = body.FindPart(fromPart);
        if (parent == null)
            parent = body.FindPart("stem_main");

        // Inherit stomata from existing leaves before creating new ones
        float inheritedStomata = GetAverageStomataOpenness(body);
        if (body.FindPartsByType(LeafType).Count == 0)
            inheritedStomata = 0.5f;

        var results = new List<Dictionary<string, object>>(number);
        for (int i = 0; i < number; i++)
        {
            string name = "leaf_" + (body.CountPartsByType(LeafType) + 1);
            PlantPart leaf = body.CreatePart(name, LeafType, sizeEach, sizeEach * 0.02f);
            if (leaf == null) continue;

            leaf.TrySetProperty("shape", "flat");
            leaf.TrySetProperty("stomata_openness", (double)inheritedStomata);

            if (parent != null)
                body.AttachPart(name, parent.Name);

            results.Add(leaf.CreateSnapshot());
        }
        return results;
    }

    /// <summary>leaf.reshape(part_name, shape) — change leaf shape.</summary>
    public static bool Reshape(PlantBody body, string partName, string shape)
    {
        PlantPart leaf = body.FindPart(partName);
        if (leaf == null) return false;
        leaf.TrySetProperty("shape", shape ?? "flat");
        return true;
    }

    // ── Orientation ─────────────────────────────────────────────────

    /// <summary>leaf.orient(direction) — point leaves in a direction.</summary>
    public static bool Orient(PlantBody body, string direction)
    {
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("orientation", direction ?? "up");
        return leaves.Count > 0;
    }

    /// <summary>leaf.track_light(enabled) — auto-orient toward light every tick.</summary>
    public static bool TrackLight(PlantBody body, bool enabled)
    {
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("track_light", enabled);
        return leaves.Count > 0;
    }

    /// <summary>leaf.set_angle_range(min, max) — constrain tracking angles.</summary>
    public static bool SetAngleRange(PlantBody body, float minAngle, float maxAngle)
    {
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].TrySetProperty("angle_min", (double)minAngle);
            leaves[i].TrySetProperty("angle_max", (double)maxAngle);
        }
        return leaves.Count > 0;
    }

    // ── Gas Exchange ────────────────────────────────────────────────

    /// <summary>leaf.open_stomata(amount) — 0.0 to 1.0 openness.</summary>
    public static bool OpenStomata(PlantBody body, float amount)
    {
        amount = Mathf.Clamp01(amount);
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("stomata_openness", (double)amount);
        return leaves.Count > 0;
    }

    /// <summary>leaf.close_stomata() — fully close.</summary>
    public static bool CloseStomata(PlantBody body)
    {
        return OpenStomata(body, 0f);
    }

    /// <summary>leaf.set_stomata_schedule(schedule) — automate stomata based on conditions.</summary>
    public static bool SetStomataSchedule(PlantBody body, object schedule)
    {
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("stomata_schedule", schedule);
        return leaves.Count > 0;
    }

    /// <summary>leaf.filter_gas(gas, action) — control gas passage through stomata.</summary>
    public static bool FilterGas(PlantBody body, string gas, string action)
    {
        if (string.IsNullOrEmpty(gas)) return false;
        var leaves = body.FindPartsByType(LeafType);
        string key = "gas_filter_" + gas.Trim().ToLowerInvariant();
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty(key, action ?? "absorb");
        return leaves.Count > 0;
    }

    // ── Properties ──────────────────────────────────────────────────

    /// <summary>leaf.set_color(r, g, b) — affects light absorption spectrum.</summary>
    public static bool SetColor(PlantBody body, float r, float g, float b)
    {
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
        {
            leaves[i].TrySetProperty("color_r", (double)Mathf.Clamp01(r));
            leaves[i].TrySetProperty("color_g", (double)Mathf.Clamp01(g));
            leaves[i].TrySetProperty("color_b", (double)Mathf.Clamp01(b));
        }
        return leaves.Count > 0;
    }

    /// <summary>leaf.set_coating(type) — none, waxy, hairy, reflective, sticky, absorbent.</summary>
    public static bool SetCoating(PlantBody body, string type)
    {
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("coating", type ?? "none");
        return leaves.Count > 0;
    }

    /// <summary>leaf.set_lifespan(ticks) — how long each leaf lives.</summary>
    public static bool SetLifespan(PlantBody body, int ticks)
    {
        var leaves = body.FindPartsByType(LeafType);
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("lifespan", (long)ticks);
        return leaves.Count > 0;
    }

    /// <summary>leaf.shed(part_name) — drop leaves. If null, shed all.</summary>
    public static int Shed(PlantBody body, string partName)
    {
        if (!string.IsNullOrEmpty(partName))
        {
            return body.RemovePart(partName) ? 1 : 0;
        }

        var leaves = body.FindPartsByType(LeafType);
        int count = 0;
        for (int i = leaves.Count - 1; i >= 0; i--)
        {
            if (body.RemovePart(leaves[i].Name))
                count++;
        }
        return count;
    }

    /// <summary>leaf.regrow(part_name) — regrow a previously shed leaf.</summary>
    public static object Regrow(PlantBody body, OrganismEntity org, string partName)
    {
        // Regrow is cheaper than new growth
        float energyCost = 1f * GrowthEnergyCostPerCm2 * 0.5f;
        float glucoseCost = energyCost * GrowthGlucoseCostMultiplier;
        if (!TrySpendGrowthResources(org, energyCost, glucoseCost)) return null;

        PlantPart existing = body.FindPart(partName);
        if (existing != null) return existing.CreateSnapshot(); // already exists

        PlantPart parent = body.FindPart("stem_main");
        PlantPart leaf = body.CreatePart(partName, LeafType, 1f, 0.02f);
        if (leaf == null) return null;

        float inheritedStomata = GetAverageStomataOpenness(body);
        if (body.FindPartsByType(LeafType).Count <= 1) // only the one we just created
            inheritedStomata = 0.5f;

        leaf.TrySetProperty("shape", "flat");
        leaf.TrySetProperty("stomata_openness", (double)inheritedStomata);
        leaf.TrySetProperty("regrown", true);

        if (parent != null)
            body.AttachPart(partName, parent.Name);

        return leaf.CreateSnapshot();
    }

    // ── Absorption (non-photosynthetic) ─────────────────────────────

    /// <summary>leaf.absorb_moisture() — pull water from humid air.</summary>
    public static double AbsorbMoisture(PlantBody body, OrganismEntity org, ResourceGrid world)
    {
        float leafArea = GetTotalLeafArea(body);
        if (leafArea <= 0f) return 0d;

        double humidity = 0.5;
        if (world.TryGetWorldValue("moisture", out object hVal) && TryConvertToDouble(hVal, out double h))
            humidity = h;

        double absorbed = leafArea * 0.01 * humidity;
        org.TryAddState("water", absorbed, out _, out _);
        return absorbed;
    }

    /// <summary>leaf.absorb_nutrients(resource) — foliar feeding.</summary>
    public static double AbsorbNutrients(PlantBody body, OrganismEntity org, ResourceGrid world, string resource)
    {
        if (string.IsNullOrEmpty(resource)) return 0d;
        float leafArea = GetTotalLeafArea(body);
        if (leafArea <= 0f) return 0d;

        double absorbed = leafArea * 0.005; // less efficient than roots
        org.TryAddState(resource.Trim().ToLowerInvariant(), absorbed, out _, out _);
        return absorbed;
    }

    /// <summary>leaf.absorb_chemical(chemical) — absorb airborne chemicals.</summary>
    public static double AbsorbChemical(PlantBody body, OrganismEntity org, ResourceGrid world, string chemical)
    {
        if (string.IsNullOrEmpty(chemical)) return 0d;
        float leafArea = GetTotalLeafArea(body);
        if (leafArea <= 0f) return 0d;

        string key = "air_" + chemical.Trim().ToLowerInvariant();
        double concentration = 0.1;
        if (world.TryGetWorldValue(key, out object cVal) && TryConvertToDouble(cVal, out double c))
            concentration = c;

        float stomata = GetAverageStomataOpenness(body);
        double absorbed = leafArea * 1.0 * concentration * stomata;
        org.TryAddState(chemical.Trim().ToLowerInvariant(), absorbed, out _, out _);
        return absorbed;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>Get total leaf area across all leaf parts.</summary>
    public static float GetTotalLeafArea(PlantBody body)
    {
        var leaves = body.FindPartsByType(LeafType);
        float area = 0f;
        for (int i = 0; i < leaves.Count; i++)
            area += leaves[i].Size;
        return area;
    }

    /// <summary>Get average stomata openness across all leaves.</summary>
    public static float GetAverageStomataOpenness(PlantBody body)
    {
        var leaves = body.FindPartsByType(LeafType);
        if (leaves.Count == 0) return 0f;

        float total = 0f;
        for (int i = 0; i < leaves.Count; i++)
        {
            if (leaves[i].TryGetProperty("stomata_openness", out object val) && TryConvertToDouble(val, out double d))
                total += (float)d;
            else
                total += 0.5f;
        }
        return total / leaves.Count;
    }

    private static bool TrySpendEnergy(OrganismEntity org, float cost)
    {
        return TrySpendResource(org, "energy", cost);
    }

    private static bool TrySpendGrowthResources(OrganismEntity org, float energyCost, float glucoseCost)
    {
        if (!CanSpendResource(org, "energy", energyCost)) return false;
        if (!CanSpendResource(org, "glucose", glucoseCost)) return false;
        return TrySpendResource(org, "energy", energyCost) &&
               TrySpendResource(org, "glucose", glucoseCost);
    }

    private static bool CanSpendResource(OrganismEntity org, string key, float amount)
    {
        if (amount <= 0f) return true;
        if (!org.TryGetState(key, out object value)) return false;
        if (!TryConvertToDouble(value, out double current)) return false;
        return current >= amount;
    }

    private static bool TrySpendResource(OrganismEntity org, string key, float amount)
    {
        if (amount <= 0f) return true;
        if (!CanSpendResource(org, key, amount)) return false;
        org.TryAddState(key, -amount, out _, out _);
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
