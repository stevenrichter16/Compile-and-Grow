using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the stem module from the API spec.
/// Handles above-ground structural growth, branching, materials, storage, and morphology.
/// </summary>
public static class StemModule
{
    private const string StemType = "stem";
    private const string BranchType = "branch";
    private const float GrowthEnergyCostPerUnit = 0.6f;

    // ── Growth ──────────────────────────────────────────────────────

    /// <summary>stem.grow_up(distance) — extend upward.</summary>
    public static bool GrowUp(PlantBody body, OrganismEntity org, float distance)
    {
        if (distance <= 0f) return false;

        float materialMultiplier = GetMaterialCostMultiplier(body);
        float cost = distance * GrowthEnergyCostPerUnit * materialMultiplier;
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart main = FindOrCreateMainStem(body);
        main.Size += distance;
        main.TrySetProperty("height", GetPropertyFloat(main, "height") + distance);
        return true;
    }

    /// <summary>stem.grow_horizontal(distance, direction) — lateral structural growth.</summary>
    public static bool GrowHorizontal(PlantBody body, OrganismEntity org, float distance, string direction)
    {
        if (distance <= 0f) return false;
        float cost = distance * GrowthEnergyCostPerUnit * 0.8f;
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart main = FindOrCreateMainStem(body);
        main.Size += distance;
        main.TrySetProperty("horizontal_spread", GetPropertyFloat(main, "horizontal_spread") + distance);
        main.TrySetProperty("grow_direction", direction ?? "light");
        return true;
    }

    /// <summary>stem.grow_thick(amount) — increase diameter.</summary>
    public static bool GrowThick(PlantBody body, OrganismEntity org, float amount)
    {
        if (amount <= 0f) return false;
        float cost = amount * GrowthEnergyCostPerUnit * 1.5f; // thickening is expensive
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart main = FindOrCreateMainStem(body);
        main.TrySetProperty("thickness", GetPropertyFloat(main, "thickness") + amount);
        main.TrySetProperty("water_capacity", GetPropertyFloat(main, "water_capacity") + amount * 0.5f);
        return true;
    }

    /// <summary>stem.branch(count, height, angle) — create branches.</summary>
    public static List<Dictionary<string, object>> Branch(PlantBody body, OrganismEntity org, int count, float height, float angle)
    {
        if (count <= 0) count = 1;
        float cost = count * GrowthEnergyCostPerUnit * 0.5f;
        if (!TrySpendEnergy(org, cost)) return null;

        PlantPart main = FindOrCreateMainStem(body);
        if (height < 0f) height = GetPropertyFloat(main, "height"); // default: top

        var results = new List<Dictionary<string, object>>(count);
        for (int i = 0; i < count; i++)
        {
            string name = "branch_" + (body.CountPartsByType(BranchType) + 1);
            PlantPart branch = body.CreatePart(name, BranchType, 1f, 0.08f);
            if (branch != null)
            {
                body.AttachPart(name, main.Name);
                branch.TrySetProperty("branch_height", (double)height);
                branch.TrySetProperty("branch_angle", (double)angle);
                results.Add(branch.CreateSnapshot());
            }
        }
        return results;
    }

    /// <summary>stem.grow_segment(length, angle, from_part) — precise segment growth.</summary>
    public static object GrowSegment(PlantBody body, OrganismEntity org, float length, float angle, string fromPart)
    {
        if (length <= 0f) return null;
        float cost = length * GrowthEnergyCostPerUnit;
        if (!TrySpendEnergy(org, cost)) return null;

        string parentName = string.IsNullOrEmpty(fromPart) ? "stem_main" : fromPart;
        PlantPart parent = body.FindPart(parentName) ?? FindOrCreateMainStem(body);

        string name = "segment_" + (body.CountPartsByType(StemType) + body.CountPartsByType("segment") + 1);
        PlantPart segment = body.CreatePart(name, "segment", length, 0.05f);
        if (segment == null) return null;

        body.AttachPart(name, parent.Name);
        segment.TrySetProperty("angle", (double)angle);
        return segment.CreateSnapshot();
    }

    /// <summary>stem.split(count) — fork growing tip into equal co-dominant stems.</summary>
    public static List<Dictionary<string, object>> Split(PlantBody body, OrganismEntity org, int count)
    {
        if (count <= 1) count = 2;
        float cost = count * GrowthEnergyCostPerUnit * 0.4f;
        if (!TrySpendEnergy(org, cost)) return null;

        PlantPart main = FindOrCreateMainStem(body);
        var results = new List<Dictionary<string, object>>(count);

        for (int i = 0; i < count; i++)
        {
            string name = "stem_tip_" + (body.CountPartsByType(StemType) + 1);
            PlantPart tip = body.CreatePart(name, StemType, 0.5f, 0.06f);
            if (tip != null)
            {
                body.AttachPart(name, main.Name);
                tip.TrySetProperty("co_dominant", true);
                results.Add(tip.CreateSnapshot());
            }
        }
        return results;
    }

    // ── Properties ──────────────────────────────────────────────────

    /// <summary>stem.set_rigidity(value) — 0.0 flexible to 1.0 fully rigid.</summary>
    public static bool SetRigidity(PlantBody body, float value)
    {
        PlantPart main = FindOrCreateMainStem(body);
        main.TrySetProperty("rigidity", (double)Mathf.Clamp01(value));
        body.TrySetMorphology("rigidity", Mathf.Clamp01(value), out _);
        return true;
    }

    /// <summary>stem.set_material(type) — herbaceous, woody, fibrous, hollow, inflatable, crystalline.</summary>
    public static bool SetMaterial(PlantBody body, string type)
    {
        PlantPart main = FindOrCreateMainStem(body);
        main.TrySetProperty("material", type ?? "herbaceous");
        return true;
    }

    /// <summary>stem.store_water(amount) — use stem as water reservoir.</summary>
    public static double StoreWater(PlantBody body, OrganismEntity org, float amount)
    {
        if (amount <= 0f) return 0d;

        PlantPart main = FindOrCreateMainStem(body);
        float thickness = GetPropertyFloat(main, "thickness");
        float capacity = thickness * 2f; // thicker stems store more
        float stored = GetPropertyFloat(main, "stored_water");
        float canStore = Mathf.Min(amount, capacity - stored);
        if (canStore <= 0f) return 0d;

        // Deduct from org water
        org.TryAddState("water", -canStore, out _, out _);
        main.TrySetProperty("stored_water", (double)(stored + canStore));
        return canStore;
    }

    /// <summary>stem.store_energy(amount) — starch storage.</summary>
    public static double StoreEnergy(PlantBody body, OrganismEntity org, float amount)
    {
        if (amount <= 0f) return 0d;

        PlantPart main = FindOrCreateMainStem(body);
        if (!TrySpendEnergy(org, amount)) return 0d;

        float stored = GetPropertyFloat(main, "stored_energy");
        main.TrySetProperty("stored_energy", (double)(stored + amount));
        return amount;
    }

    // ── Support ─────────────────────────────────────────────────────

    /// <summary>stem.attach_to(target) — climbing vine attachment.</summary>
    public static bool AttachTo(PlantBody body, string target)
    {
        PlantPart main = FindOrCreateMainStem(body);
        main.TrySetProperty("attached_to", target ?? "support");
        main.TrySetProperty("climbing", true);
        return true;
    }

    /// <summary>stem.support_weight(part_name) — reinforce support.</summary>
    public static bool SupportWeight(PlantBody body, OrganismEntity org, string partName)
    {
        PlantPart part = body.FindPart(partName);
        if (part == null) return false;
        float cost = 0.3f;
        if (!TrySpendEnergy(org, cost)) return false;
        part.TrySetProperty("reinforced", true);
        return true;
    }

    /// <summary>stem.shed(part_name) — drop a branch or segment.</summary>
    public static bool Shed(PlantBody body, string partName)
    {
        return body.RemovePart(partName);
    }

    /// <summary>stem.heal(part_name, rate) — repair damage.</summary>
    public static bool Heal(PlantBody body, OrganismEntity org, string partName, float rate)
    {
        PlantPart part = body.FindPart(partName);
        if (part == null) return false;
        float cost = rate * 0.5f;
        if (!TrySpendEnergy(org, cost)) return false;
        part.Health = Mathf.Clamp01(part.Health + rate);
        return true;
    }

    // ── Morphology Control ──────────────────────────────────────────

    /// <summary>stem.set_color(r, g, b)</summary>
    public static bool SetColor(PlantBody body, float r, float g, float b)
    {
        PlantPart main = FindOrCreateMainStem(body);
        main.TrySetProperty("color_r", (double)Mathf.Clamp01(r));
        main.TrySetProperty("color_g", (double)Mathf.Clamp01(g));
        main.TrySetProperty("color_b", (double)Mathf.Clamp01(b));
        return true;
    }

    /// <summary>stem.set_texture(type)</summary>
    public static bool SetTexture(PlantBody body, string type)
    {
        PlantPart main = FindOrCreateMainStem(body);
        main.TrySetProperty("texture", type ?? "smooth");
        body.TrySetMorphology("texture", type ?? "smooth", out _);
        return true;
    }

    /// <summary>stem.produce_bark(thickness) — outer protective layer. Requires woody material.</summary>
    public static bool ProduceBark(PlantBody body, OrganismEntity org, float thickness)
    {
        PlantPart main = FindOrCreateMainStem(body);
        float cost = thickness * GrowthEnergyCostPerUnit;
        if (!TrySpendEnergy(org, cost)) return false;
        main.TrySetProperty("bark_thickness", (double)(GetPropertyFloat(main, "bark_thickness") + thickness));
        return true;
    }

    /// <summary>stem.produce_wax(thickness) — waterproof coating.</summary>
    public static bool ProduceWax(PlantBody body, OrganismEntity org, float thickness)
    {
        PlantPart main = FindOrCreateMainStem(body);
        float cost = thickness * GrowthEnergyCostPerUnit * 0.5f;
        if (!TrySpendEnergy(org, cost)) return false;
        main.TrySetProperty("wax_thickness", (double)(GetPropertyFloat(main, "wax_thickness") + thickness));
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PlantPart FindOrCreateMainStem(PlantBody body)
    {
        PlantPart main = body.FindPart("stem_main");
        if (main == null)
        {
            main = body.CreatePart("stem_main", StemType, 1f, 0.1f);
            main.TrySetProperty("material", "herbaceous");
            main.TrySetProperty("rigidity", 0.5);
        }
        return main;
    }

    private static float GetMaterialCostMultiplier(PlantBody body)
    {
        PlantPart main = body.FindPart("stem_main");
        if (main == null) return 1f;
        if (main.TryGetProperty("material", out object mat) && mat is string material)
        {
            switch (material)
            {
                case "herbaceous": return 0.7f;
                case "woody": return 2.0f;
                case "fibrous": return 1.0f;
                case "hollow": return 0.4f;
                case "inflatable": return 0.5f;
                case "crystalline": return 3.0f;
            }
        }
        return 1f;
    }

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
