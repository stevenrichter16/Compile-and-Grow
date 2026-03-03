using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the defense module from the API spec.
/// Handles physical defense, chemical defense, immune system, and reactive defense.
/// All operations go through PlantBody for part management and OrganismEntity for state.
/// </summary>
public static class DefenseModule
{
    private const string DefenseType = "defense";
    private const float ThornEnergyCost = 0.8f;
    private const float ArmorEnergyCost = 1.5f;
    private const float CamouflageEnergyCost = 0.5f;
    private const float ToxinEnergyCost = 1.0f;
    private const float RepellentEnergyCost = 0.6f;
    private const float AttractantEnergyCost = 0.7f;
    private const float TrapEnergyCost = 0.4f;
    private const float DiseaseResistCostBase = 0.5f;
    private const float QuarantineEnergyCost = 0.3f;
    private const float FeverEnergyCostPerDegree = 0.4f;

    // ── Physical Defense ─────────────────────────────────────────────

    /// <summary>defense.grow_thorns(part_name, sharpness, density) — grow sharp protrusions.</summary>
    public static bool GrowThorns(PlantBody body, OrganismEntity org, string partName, float sharpness, float density)
    {
        sharpness = Mathf.Clamp01(sharpness);
        density = Mathf.Clamp01(density);
        float cost = ThornEnergyCost * (sharpness + density);
        if (!TrySpendEnergy(org, cost)) return false;

        if (string.IsNullOrEmpty(partName))
        {
            // Apply to whole organism via morphology
            body.TrySetMorphology("thorns_sharpness", (double)sharpness, out _);
            body.TrySetMorphology("thorns_density", (double)density, out _);
            body.TrySetMorphology("has_thorns", true, out _);
        }
        else
        {
            PlantPart part = body.FindPart(partName);
            if (part == null) return false;
            part.TrySetProperty("thorns_sharpness", (double)sharpness);
            part.TrySetProperty("thorns_density", (double)density);
            part.TrySetProperty("has_thorns", true);
        }

        return true;
    }

    /// <summary>defense.grow_armor(part_name, thickness) — harden outer layer.</summary>
    public static bool GrowArmor(PlantBody body, OrganismEntity org, string partName, float thickness)
    {
        if (string.IsNullOrEmpty(partName)) return false;
        thickness = Mathf.Max(0f, thickness);
        float cost = ArmorEnergyCost * thickness;
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart part = body.FindPart(partName);
        if (part == null) return false;

        float existing = GetPropertyFloat(part, "armor_thickness");
        part.TrySetProperty("armor_thickness", (double)(existing + thickness));
        part.TrySetProperty("has_armor", true);
        return true;
    }

    /// <summary>defense.grow_camouflage(environment_type) — match surroundings.</summary>
    public static bool GrowCamouflage(PlantBody body, OrganismEntity org, string environmentType)
    {
        if (!TrySpendEnergy(org, CamouflageEnergyCost)) return false;

        string env = string.IsNullOrEmpty(environmentType) ? "vegetation" : environmentType.Trim().ToLowerInvariant();
        body.TrySetMorphology("camouflage_type", env, out _);
        body.TrySetMorphology("has_camouflage", true, out _);

        // Adjust color based on environment
        switch (env)
        {
            case "soil":
                body.TrySetMorphology("color_r", 0.4, out _);
                body.TrySetMorphology("color_g", 0.3, out _);
                body.TrySetMorphology("color_b", 0.2, out _);
                break;
            case "rock":
                body.TrySetMorphology("color_r", 0.5, out _);
                body.TrySetMorphology("color_g", 0.5, out _);
                body.TrySetMorphology("color_b", 0.5, out _);
                break;
            case "dark":
                body.TrySetMorphology("color_r", 0.1, out _);
                body.TrySetMorphology("color_g", 0.1, out _);
                body.TrySetMorphology("color_b", 0.1, out _);
                break;
            case "bright":
                body.TrySetMorphology("color_r", 0.9, out _);
                body.TrySetMorphology("color_g", 0.9, out _);
                body.TrySetMorphology("color_b", 0.8, out _);
                break;
            default: // "vegetation"
                body.TrySetMorphology("color_r", 0.2, out _);
                body.TrySetMorphology("color_g", 0.6, out _);
                body.TrySetMorphology("color_b", 0.1, out _);
                break;
        }

        return true;
    }

    // ── Chemical Defense ──────────────────────────────────────────────

    /// <summary>defense.produce_toxin(type, potency, location) — synthesize toxic compound.</summary>
    public static bool ProduceToxin(PlantBody body, OrganismEntity org, string type, float potency, string location)
    {
        if (string.IsNullOrEmpty(type)) return false;
        potency = Mathf.Clamp01(potency);
        float cost = ToxinEnergyCost * (0.5f + potency);
        if (!TrySpendEnergy(org, cost)) return false;

        string normalizedType = type.Trim().ToLowerInvariant();
        string loc = string.IsNullOrEmpty(location) ? "all" : location.Trim().ToLowerInvariant();

        // Store toxin info on the organism
        org.SetMemoryValue("_defense_toxin_type", normalizedType);
        org.SetMemoryValue("_defense_toxin_potency", (double)potency);
        org.SetMemoryValue("_defense_toxin_location", loc);

        // If location targets a specific part, mark it
        if (loc != "all")
        {
            PlantPart part = body.FindPart(loc);
            if (part != null)
            {
                part.TrySetProperty("toxin_type", normalizedType);
                part.TrySetProperty("toxin_potency", (double)potency);
            }
            else
            {
                // Try finding parts by type matching location
                var parts = body.FindPartsByType(loc);
                for (int i = 0; i < parts.Count; i++)
                {
                    parts[i].TrySetProperty("toxin_type", normalizedType);
                    parts[i].TrySetProperty("toxin_potency", (double)potency);
                }
            }
        }

        return true;
    }

    /// <summary>defense.produce_repellent(type, radius) — emit pest-repelling compound.</summary>
    public static bool ProduceRepellent(PlantBody body, OrganismEntity org, ResourceGrid world, string type, int radius)
    {
        if (string.IsNullOrEmpty(type)) return false;
        radius = Mathf.Max(1, radius);
        float cost = RepellentEnergyCost * radius;
        if (!TrySpendEnergy(org, cost)) return false;

        string normalizedType = type.Trim().ToLowerInvariant();
        org.SetMemoryValue("_defense_repellent_type", normalizedType);
        org.SetMemoryValue("_defense_repellent_radius", (long)radius);

        // Register in world state
        world.TryAddWorldValue("repellent_" + normalizedType, radius, out _, out _);
        return true;
    }

    /// <summary>defense.produce_attractant(type, target, radius) — attract specific organism type.</summary>
    public static bool ProduceAttractant(PlantBody body, OrganismEntity org, ResourceGrid world, string type, string target, int radius)
    {
        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(target)) return false;
        radius = Mathf.Max(1, radius);
        float cost = AttractantEnergyCost * radius;
        if (!TrySpendEnergy(org, cost)) return false;

        string normalizedType = type.Trim().ToLowerInvariant();
        string normalizedTarget = target.Trim().ToLowerInvariant();
        org.SetMemoryValue("_defense_attractant_type", normalizedType);
        org.SetMemoryValue("_defense_attractant_target", normalizedTarget);
        org.SetMemoryValue("_defense_attractant_radius", (long)radius);

        // Register in world state
        world.TryAddWorldValue("attractant_" + normalizedTarget, radius, out _, out _);
        return true;
    }

    /// <summary>defense.sticky_trap(part_name, strength) — make surface sticky to trap organisms.</summary>
    public static bool StickyTrap(PlantBody body, OrganismEntity org, string partName, float strength)
    {
        if (string.IsNullOrEmpty(partName)) return false;
        strength = Mathf.Clamp01(strength);
        float cost = TrapEnergyCost * strength;
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart part = body.FindPart(partName);
        if (part == null) return false;

        part.TrySetProperty("sticky_trap", true);
        part.TrySetProperty("trap_strength", (double)strength);
        return true;
    }

    // ── Immune System ────────────────────────────────────────────────

    /// <summary>defense.resist_disease(type, strength) — allocate energy to resist disease.</summary>
    public static bool ResistDisease(PlantBody body, OrganismEntity org, string type, float strength)
    {
        if (string.IsNullOrEmpty(type)) return false;
        strength = Mathf.Clamp01(strength);

        string normalizedType = type.Trim().ToLowerInvariant();
        float costMultiplier = normalizedType == "all" ? 3f : 1f;
        float cost = DiseaseResistCostBase * strength * costMultiplier;
        if (!TrySpendEnergy(org, cost)) return false;

        org.SetMemoryValue("_defense_resist_type", normalizedType);
        org.SetMemoryValue("_defense_resist_strength", (double)strength);

        // Apply healing proportional to resistance strength
        org.ApplyHeal(strength * 0.05);
        return true;
    }

    /// <summary>defense.quarantine_part(part_name) — isolate diseased part.</summary>
    public static bool QuarantinePart(PlantBody body, string partName)
    {
        if (string.IsNullOrEmpty(partName)) return false;

        PlantPart part = body.FindPart(partName);
        if (part == null) return false;

        part.TrySetProperty("quarantined", true);
        part.TrySetProperty("receives_nutrients", false);
        return true;
    }

    /// <summary>defense.fever(amount) — raise internal temperature to fight infection.</summary>
    public static bool Fever(OrganismEntity org, float amount)
    {
        amount = Mathf.Max(0f, amount);
        float cost = FeverEnergyCostPerDegree * amount;
        if (!TrySpendEnergy(org, cost)) return false;

        // Store fever state
        org.SetMemoryValue("_defense_fever", (double)amount);

        // Fever fights infection but damages organism if too high
        if (amount > 5f)
        {
            float selfDamage = (amount - 5f) * 0.02f;
            org.ApplyDamage(selfDamage);
        }

        // Reduce stress from disease
        org.TryAddState("stress", -amount * 0.02, out _, out _);
        return true;
    }

    // ── Reactive Defense ─────────────────────────────────────────────

    /// <summary>defense.on_damage(callback) — register damage callback. Stored for future wiring.</summary>
    public static bool OnDamage(OrganismEntity org, object callback)
    {
        if (callback == null) return false;
        org.SetMemoryValue("_defense_on_damage", callback);
        return true;
    }

    /// <summary>defense.on_neighbor_distress(callback) — register distress callback. Stored for future wiring.</summary>
    public static bool OnNeighborDistress(OrganismEntity org, object callback)
    {
        if (callback == null) return false;
        org.SetMemoryValue("_defense_on_neighbor_distress", callback);
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static float GetPropertyFloat(PlantPart part, string key)
    {
        if (part.TryGetProperty(key, out object val) && TryConvertToDouble(val, out double d))
            return (float)d;
        return 0f;
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
