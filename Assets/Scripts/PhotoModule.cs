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
    private const float MinViableLeafArea = 0.25f;
    private const double OptimalAirCo2 = 0.04d;
    private const double WaterDemandPerLeafArea = 0.6d;
    private const double GlucosePerEnergy = 0.5d;
    private const double TrackLightBonusMax = 0.25d;
    private const double RootSupplyLeafRatio = 0.75d;

    private struct Phase1ProcessResult
    {
        public PlantPart WaterStoragePart;
        public double StoredStemWater;
        public double LightCapture;
        public double RootSupplyRatio;
        public double WaterUsed;
        public double WaterFromState;
        public double WaterFromStem;
        public double EnergyProduced;
        public double GlucoseProduced;
        public double WaterEfficiency;
        public double NetEnergyPerTick;
        public string LimitingFactor;
    }

    // ── Photosynthesis ──────────────────────────────────────────────

    /// <summary>photo.process() — Phase 1 beginner-facing photosynthesis loop.</summary>
    public static double Process(PlantBody body, OrganismEntity org, ResourceGrid world)
    {
        Phase1ProcessResult result = EvaluatePhase1(body, org, world, efficiencyOverride: null);
        ApplyPhase1Result(org, world, result);
        return result.EnergyProduced;
    }

    /// <summary>photo.get_limiting_factor() — report the current limiting factor.</summary>
    public static string GetLimitingFactor(PlantBody body, OrganismEntity org, ResourceGrid world)
    {
        return EvaluatePhase1(body, org, world, efficiencyOverride: null).LimitingFactor;
    }

    /// <summary>
    /// photo.absorb_light() — stoichiometric photosynthesis.
    /// 6 CO₂ + 6 H₂O + light → C₆H₁₂O₆ + 6 O₂
    /// CO₂ enters from air via stomata (not consumed from storage).
    /// O₂ is produced from splitting water and emitted to air.
    /// </summary>
    public static double AbsorbLight(PlantBody body, OrganismEntity org, ResourceGrid world, double efficiencyOverride)
    {
        double? overrideValue = efficiencyOverride >= 0d && efficiencyOverride <= 1d
            ? (double?)efficiencyOverride
            : null;

        Phase1ProcessResult result = EvaluatePhase1(body, org, world, overrideValue);
        ApplyPhase1Result(org, world, result);
        return result.EnergyProduced;
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

    private static Phase1ProcessResult EvaluatePhase1(
        PlantBody body,
        OrganismEntity org,
        ResourceGrid world,
        double? efficiencyOverride)
    {
        var leaves = body.FindPartsByType("leaf");
        double leafArea = LeafModule.GetTotalLeafArea(body);
        double stomata = Clamp01(LeafModule.GetAverageStomataOpenness(body));
        double rootArea = RootModule.GetTotalRootArea(body);

        double lightIntensity = 0.7d;
        if (world.TryGetWorldValue("power", out object lightValue) && TryConvertToDouble(lightValue, out double rawLight))
            lightIntensity = Clamp01(rawLight / 100.0d);

        PlantPart anyLeaf = leaves.Count > 0 ? leaves[0] : null;
        double lightTrackingRatio = GetLightTrackingRatio(leaves);
        double lightCapture = efficiencyOverride ?? (lightIntensity + lightTrackingRatio * (1d - lightIntensity) * TrackLightBonusMax);

        if (anyLeaf != null && anyLeaf.TryGetProperty("pigment", out object pig) && pig is string pigment)
            lightCapture *= GetPigmentMultiplier(pigment, lightIntensity);

        if (anyLeaf != null && anyLeaf.TryGetProperty("chlorophyll_boost", out object cb) && TryConvertToDouble(cb, out double boost))
            lightCapture *= boost;

        if (anyLeaf != null && anyLeaf.TryGetProperty("light_saturation", out object ls) && TryConvertToDouble(ls, out double saturation) &&
            lightIntensity > saturation && saturation > 0d)
        {
            lightCapture *= saturation / lightIntensity;
        }

        lightCapture = Clamp01(lightCapture);

        if (leafArea < MinViableLeafArea)
        {
            return new Phase1ProcessResult
            {
                LightCapture = lightCapture,
                RootSupplyRatio = Clamp01((rootArea + 0.1d) / MinViableLeafArea),
                LimitingFactor = "surface_area",
            };
        }

        double airCo2 = OptimalAirCo2;
        if (world.TryGetWorldValue("air_co2", out object co2Value) && TryConvertToDouble(co2Value, out double rawCo2))
            airCo2 = Math.Max(0d, rawCo2);

        double carbonRatio = Clamp01(airCo2 / OptimalAirCo2);
        double currentWater = GetStateNumber(org, "water");
        PlantPart waterStoragePart = FindWaterStoragePart(body);
        double storedStemWater = waterStoragePart != null ? GetPropertyFloat(waterStoragePart, "stored_water") : 0d;
        double accessibleWater = currentWater + storedStemWater;
        double rootSupplyRatio = Clamp01((rootArea + 0.1d) / Math.Max(MinViableLeafArea, leafArea * RootSupplyLeafRatio));

        double lightLimitedEnergy = leafArea * BasePhotosynthesisRate * lightCapture;
        double carbonCeilingRatio = Clamp01(carbonRatio * stomata);
        double fullWaterDemand = leafArea * WaterDemandPerLeafArea * stomata;
        double waterAvailabilityRatio = fullWaterDemand <= 0.0001d
            ? 1d
            : Clamp01(accessibleWater / fullWaterDemand) * rootSupplyRatio;
        double resourceScale = Math.Min(1d, Math.Min(carbonCeilingRatio, waterAvailabilityRatio));

        double energyProduced = lightLimitedEnergy * resourceScale;
        double waterUsed = fullWaterDemand * resourceScale;
        double waterFromStem = Math.Min(storedStemWater, Math.Max(0d, waterUsed - currentWater));
        double waterFromState = Math.Max(0d, waterUsed - waterFromStem);
        double glucoseProduced = energyProduced * GlucosePerEnergy;

        return new Phase1ProcessResult
        {
            WaterStoragePart = waterStoragePart,
            StoredStemWater = storedStemWater,
            LightCapture = lightCapture,
            RootSupplyRatio = rootSupplyRatio,
            WaterUsed = waterUsed,
            WaterFromState = waterFromState,
            WaterFromStem = waterFromStem,
            EnergyProduced = energyProduced,
            GlucoseProduced = glucoseProduced,
            WaterEfficiency = waterUsed > 0.0001d ? glucoseProduced / waterUsed : 0d,
            NetEnergyPerTick = energyProduced,
            LimitingFactor = DetermineLimitingFactor(leafArea, lightCapture, waterAvailabilityRatio, carbonCeilingRatio),
        };
    }

    private static void ApplyPhase1Result(OrganismEntity org, ResourceGrid world, Phase1ProcessResult result)
    {
        SetMetric(org, "glucose_per_tick", result.GlucoseProduced);
        SetMetric(org, "net_energy_per_tick", result.NetEnergyPerTick);
        SetMetric(org, "water_efficiency", result.WaterEfficiency);
        SetMetric(org, "light_capture_pct", result.LightCapture * 100d);
        SetMetric(org, "root_supply_ratio", result.RootSupplyRatio);
        org.TrySetState("limiting_factor", result.LimitingFactor ?? "none", out _);

        if (result.WaterFromState > 0d)
            org.TryAddState("water", -result.WaterFromState, out _, out _);

        if (result.WaterStoragePart != null && result.WaterFromStem > 0d)
        {
            double remaining = Math.Max(0d, result.StoredStemWater - result.WaterFromStem);
            result.WaterStoragePart.TrySetProperty("stored_water", remaining);
        }

        if (result.EnergyProduced > 0d)
            org.TryAddState("energy", result.EnergyProduced, out _, out _);

        if (result.GlucoseProduced > 0d)
            org.TryAddState("glucose", result.GlucoseProduced, out _, out _);

        if (result.WaterUsed > 0d)
            world.TryAddWorldValue("air_oxygen", result.WaterUsed, out _, out _);
    }

    private static string DetermineLimitingFactor(
        double leafArea,
        double lightCapture,
        double waterAvailabilityRatio,
        double carbonCeilingRatio)
    {
        if (leafArea < MinViableLeafArea)
            return "surface_area";

        double lightRatio = Clamp01(lightCapture);
        double waterRatio = Clamp01(waterAvailabilityRatio);
        double carbonRatio = Clamp01(carbonCeilingRatio);

        if (lightRatio >= 0.95d && waterRatio >= 0.95d && carbonRatio >= 0.95d)
            return "none";

        double minimum = Math.Min(lightRatio, Math.Min(waterRatio, carbonRatio));
        if (minimum == waterRatio)
            return "water";
        if (minimum == carbonRatio)
            return "carbon";
        return "light";
    }

    private static PlantPart FindWaterStoragePart(PlantBody body)
    {
        PlantPart mainStem = body.FindPart("stem_main");
        if (mainStem != null)
            return mainStem;

        var stems = body.FindPartsByType("stem");
        return stems.Count > 0 ? stems[0] : null;
    }

    private static double GetLightTrackingRatio(List<PlantPart> leaves)
    {
        if (leaves == null || leaves.Count == 0)
            return 0d;

        double tracked = 0d;
        for (int i = 0; i < leaves.Count; i++)
        {
            if (leaves[i].TryGetProperty("track_light", out object value) && value is bool enabled && enabled)
                tracked += 1d;
        }

        return tracked / leaves.Count;
    }

    private static double GetStateNumber(OrganismEntity org, string key)
    {
        if (org.TryGetState(key, out object value) && TryConvertToDouble(value, out double number))
            return number;
        return 0d;
    }

    private static void SetMetric(OrganismEntity org, string key, double value)
    {
        org.TrySetState(key, value, out _);
    }

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
