using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the photo module from the API spec.
/// Handles photosynthesis, alternative energy sources, and energy management.
/// </summary>
public static class PhotoModule
{
    private const float BasePhotosynthesisRate = 2f; // base energy per unit leaf area

    // ── Photosynthesis ──────────────────────────────────────────────

    /// <summary>
    /// photo.absorb_light() — stoichiometric photosynthesis.
    /// 6 CO₂ + 6 H₂O + light → C₆H₁₂O₆ + 6 O₂
    /// CO₂ enters from air via stomata (not consumed from storage).
    /// O₂ is produced from splitting water and emitted to air.
    /// </summary>
    public static double AbsorbLight(PlantBody body, OrganismEntity org, ResourceGrid world, double efficiencyOverride)
    {
        float leafArea = LeafModule.GetTotalLeafArea(body);
        if (leafArea <= 0f) return 0d;

        float stomata = LeafModule.GetAverageStomataOpenness(body);

        // ── Light capture (unchanged) ───────────────────────────────
        double lightIntensity = 0.7;
        if (world.TryGetWorldValue("power", out object lVal) && TryConvertToDouble(lVal, out double lv))
            lightIntensity = Clamp01(lv / 100.0);

        double lightCapture = lightIntensity;

        PlantPart anyLeaf = body.FindPartsByType("leaf").Count > 0 ? body.FindPartsByType("leaf")[0] : null;

        if (anyLeaf != null && anyLeaf.TryGetProperty("pigment", out object pig) && pig is string pigment)
            lightCapture *= GetPigmentMultiplier(pigment, lightIntensity);

        if (anyLeaf != null && anyLeaf.TryGetProperty("chlorophyll_boost", out object cb) && TryConvertToDouble(cb, out double boost))
            lightCapture *= boost;

        if (anyLeaf != null && anyLeaf.TryGetProperty("light_saturation", out object ls) && TryConvertToDouble(ls, out double sat))
        {
            if (lightIntensity > sat)
                lightCapture *= sat / lightIntensity;
        }

        lightCapture = Clamp01(lightCapture);

        // ── CO₂ from air (environmental constant, not consumed) ─────
        double airCo2 = 0.04;
        if (world.TryGetWorldValue("air_co2", out object co2Val) && TryConvertToDouble(co2Val, out double co2))
            airCo2 = co2;

        // ── Stoichiometric reaction scale ───────────────────────────
        double reactionScale = leafArea * stomata * airCo2 * lightCapture;

        if (efficiencyOverride >= 0d && efficiencyOverride <= 1d)
            reactionScale = leafArea * efficiencyOverride;

        // Scale down if water is insufficient
        double currentWater = 0d;
        if (org.TryGetState("water", out object wVal) && TryConvertToDouble(wVal, out double wv))
            currentWater = wv;
        if (currentWater < reactionScale)
            reactionScale = Math.Max(0d, currentWater);

        if (reactionScale <= 0d) return 0d;

        // ── Consume water (H₂O → split for O₂) ─────────────────────
        org.TryAddState("water", -reactionScale, out _, out _);

        // ── Produce energy (glucose → energy) ───────────────────────
        double energyProduced = reactionScale * BasePhotosynthesisRate;
        org.TryAddState("energy", energyProduced, out _, out _);

        // ── Emit O₂ to air ──────────────────────────────────────────
        world.TryAddWorldValue("air_oxygen", reactionScale, out _, out _);

        return energyProduced;
    }

    /// <summary>photo.set_pigment(type) — change photosynthetic pigment.</summary>
    public static bool SetPigment(PlantBody body, string type)
    {
        var leaves = body.FindPartsByType("leaf");
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("pigment", type ?? "chlorophyll_a");
        return leaves.Count > 0;
    }

    /// <summary>photo.boost_chlorophyll(factor) — increase chlorophyll density.</summary>
    public static bool BoostChlorophyll(PlantBody body, float factor)
    {
        if (factor <= 0f) factor = 1f;
        var leaves = body.FindPartsByType("leaf");
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("chlorophyll_boost", (double)factor);
        return leaves.Count > 0;
    }

    /// <summary>photo.set_light_saturation(threshold) — when plant is "full" of light.</summary>
    public static bool SetLightSaturation(PlantBody body, float threshold)
    {
        threshold = Mathf.Clamp01(threshold);
        var leaves = body.FindPartsByType("leaf");
        for (int i = 0; i < leaves.Count; i++)
            leaves[i].TrySetProperty("light_saturation", (double)threshold);
        return leaves.Count > 0;
    }

    // ── Alternative Energy Sources ──────────────────────────────────

    /// <summary>photo.chemosynthesis(source) — energy from chemical reactions.</summary>
    public static double Chemosynthesis(OrganismEntity org, ResourceGrid world, string source)
    {
        if (string.IsNullOrEmpty(source)) return 0d;

        string key = "soil_" + source.Trim().ToLowerInvariant();
        double concentration = 0.1;
        if (world.TryGetWorldValue(key, out object cVal) && TryConvertToDouble(cVal, out double c))
            concentration = c;

        double energyProduced = concentration * 3.0; // moderate energy
        org.TryAddState("energy", energyProduced, out _, out _);
        return energyProduced;
    }

    /// <summary>photo.thermosynthesis(source) — energy from temperature differentials.</summary>
    public static double Thermosynthesis(OrganismEntity org, ResourceGrid world, string source)
    {
        double temperature = 22.0;
        if (world.TryGetWorldValue("temperature", out object tVal) && TryConvertToDouble(tVal, out double t))
            temperature = t;

        // Energy from temperature differential; more extreme temps produce more
        double differential = Math.Abs(temperature - 20.0);
        double energyProduced = differential * 0.2; // low but reliable
        org.TryAddState("energy", energyProduced, out _, out _);
        return energyProduced;
    }

    /// <summary>photo.radiosynthesis() — energy from ambient radiation.</summary>
    public static double Radiosynthesis(OrganismEntity org, ResourceGrid world)
    {
        double radiation = 0.0;
        if (world.TryGetWorldValue("radiation", out object rVal) && TryConvertToDouble(rVal, out double r))
            radiation = r;

        double energyProduced = radiation * 5.0;
        org.TryAddState("energy", energyProduced, out _, out _);
        return energyProduced;
    }

    /// <summary>photo.parasitic(target) — drain energy from another organism.</summary>
    public static double Parasitic(OrganismEntity org)
    {
        // In a full implementation this targets another OrganismEntity.
        // For now, returns a small fixed amount as a placeholder.
        double drained = 1.0;
        org.TryAddState("energy", drained, out _, out _);
        org.TryAddState("stress", 0.05, out _, out _); // parasitism is stressful
        return drained;
    }

    /// <summary>photo.decompose(organic_matter) — break down dead material.</summary>
    public static double Decompose(OrganismEntity org, ResourceGrid world)
    {
        double organicMatter = 0.0;
        if (world.TryGetWorldValue("soil_organic_matter", out object omVal) && TryConvertToDouble(omVal, out double om))
            organicMatter = om;

        double energyProduced = organicMatter * 2.0;
        org.TryAddState("energy", energyProduced, out _, out _);

        // Reduce soil organic matter
        if (organicMatter > 0d)
            world.TryAddWorldValue("soil_organic_matter", -energyProduced * 0.5, out _, out _);

        return energyProduced;
    }

    // ── Energy Management ───────────────────────────────────────────

    /// <summary>photo.set_metabolism(rate) — overall metabolic rate multiplier.</summary>
    public static bool SetMetabolism(OrganismEntity org, float rate)
    {
        if (rate <= 0f) rate = 1f;
        org.TrySetState("metabolism", (double)rate, out _);
        return true;
    }

    /// <summary>photo.store_energy(amount, location) — store excess energy.</summary>
    public static double StoreEnergy(PlantBody body, OrganismEntity org, float amount, string location)
    {
        if (amount <= 0f) return 0d;
        if (!TrySpendEnergy(org, amount)) return 0d;

        PlantPart target;
        string loc = (location ?? "stem").Trim().ToLowerInvariant();

        switch (loc)
        {
            case "stem":
                target = body.FindPart("stem_main");
                break;
            case "root":
                target = body.FindPart("root_main");
                break;
            default:
                target = body.FindPart(location);
                break;
        }

        if (target == null)
        {
            target = body.FindPart("stem_main");
            if (target == null) return 0d;
        }

        float stored = GetPropertyFloat(target, "stored_energy");
        target.TrySetProperty("stored_energy", (double)(stored + amount));
        return amount;
    }

    /// <summary>photo.retrieve_energy(amount, location) — pull stored energy back.</summary>
    public static double RetrieveEnergy(PlantBody body, OrganismEntity org, float amount, string location)
    {
        if (amount <= 0f) return 0d;

        PlantPart target;
        string loc = (location ?? "stem").Trim().ToLowerInvariant();

        switch (loc)
        {
            case "stem":
                target = body.FindPart("stem_main");
                break;
            case "root":
                target = body.FindPart("root_main");
                break;
            default:
                target = body.FindPart(location);
                break;
        }

        if (target == null) return 0d;

        float stored = GetPropertyFloat(target, "stored_energy");
        float retrieved = Mathf.Min(amount, stored);
        if (retrieved <= 0f) return 0d;

        target.TrySetProperty("stored_energy", (double)(stored - retrieved));
        org.TryAddState("energy", retrieved, out _, out _);
        return retrieved;
    }

    /// <summary>photo.share_energy(target, amount) — send energy to another organism.</summary>
    public static double ShareEnergy(OrganismEntity org, float amount)
    {
        // Full implementation would target another OrganismEntity via fungi/contact.
        // For now, just deduct from self as a placeholder.
        if (amount <= 0f) return 0d;
        if (!TrySpendEnergy(org, amount)) return 0d;
        return amount;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static double GetPigmentMultiplier(string pigment, double lightIntensity)
    {
        switch (pigment)
        {
            case "chlorophyll_a": return 1.0;
            case "chlorophyll_b": return 1.1;
            case "carotenoid": return 0.8;
            case "phycocyanin": return lightIntensity < 0.3 ? 1.5 : 0.7; // great in low light
            case "bacterio": return lightIntensity < 0.1 ? 2.0 : 0.3; // infrared specialist
            default: return 1.0;
        }
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

    private static double Clamp01(double v)
    {
        return v < 0d ? 0d : v > 1d ? 1d : v;
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
