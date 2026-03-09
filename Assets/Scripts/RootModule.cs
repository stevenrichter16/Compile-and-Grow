using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the root module from the API spec.
/// Handles subterranean growth, resource absorption, soil interaction, and sensing.
/// All operations go through PlantBody for part management and OrganismEntity for state.
/// </summary>
public static class RootModule
{
    private const string RootType = "root";
    private const float GrowthEnergyCostPerUnit = 0.5f;
    private const float BaseAbsorptionRate = 0.1f;

    // ── Growth ──────────────────────────────────────────────────────

    /// <summary>root.grow_down(distance) — extend root downward.</summary>
    public static bool GrowDown(PlantBody body, OrganismEntity org, float distance)
    {
        if (distance <= 0f) return false;
        float cost = distance * GrowthEnergyCostPerUnit;
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart main = FindOrCreateMainRoot(body);
        main.Size += distance;
        main.TrySetProperty("depth", GetPropertyFloat(main, "depth") + distance);
        return true;
    }

    /// <summary>root.grow_up(distance) — aerial roots.</summary>
    public static bool GrowUp(PlantBody body, OrganismEntity org, float distance)
    {
        if (distance <= 0f) return false;
        float cost = distance * GrowthEnergyCostPerUnit * 1.2f; // aerial roots cost more
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart main = FindOrCreateMainRoot(body);
        main.Size += distance;
        main.TrySetProperty("aerial_height", GetPropertyFloat(main, "aerial_height") + distance);
        return true;
    }

    /// <summary>root.grow_wide(distance) — lateral spread.</summary>
    public static bool GrowWide(PlantBody body, OrganismEntity org, float distance)
    {
        if (distance <= 0f) return false;
        float cost = distance * GrowthEnergyCostPerUnit * 0.8f; // lateral is cheaper
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart main = FindOrCreateMainRoot(body);
        main.Size += distance;
        main.TrySetProperty("spread", GetPropertyFloat(main, "spread") + distance);
        return true;
    }

    /// <summary>root.grow_toward(direction, distance) — directed growth.</summary>
    public static bool GrowToward(PlantBody body, OrganismEntity org, string direction, float distance)
    {
        if (distance <= 0f) return false;
        float cost = distance * GrowthEnergyCostPerUnit;
        if (!TrySpendEnergy(org, cost)) return false;

        PlantPart main = FindOrCreateMainRoot(body);
        main.Size += distance;
        main.TrySetProperty("grow_direction", direction ?? "down");
        return true;
    }

    /// <summary>root.branch(count, from_part) — create sub-roots.</summary>
    public static List<Dictionary<string, object>> Branch(PlantBody body, OrganismEntity org, int count, string fromPart)
    {
        if (count <= 0) count = 1;
        float cost = count * GrowthEnergyCostPerUnit * 0.3f;
        if (!TrySpendEnergy(org, cost)) return null;

        PlantPart parent = string.IsNullOrEmpty(fromPart) ? FindOrCreateMainRoot(body) : body.FindPart(fromPart);
        if (parent == null) return null;

        var results = new List<Dictionary<string, object>>(count);
        for (int i = 0; i < count; i++)
        {
            string name = "root_branch_" + (body.CountPartsByType(RootType) + 1);
            PlantPart branch = body.CreatePart(name, RootType, 0.5f, 0.05f);
            if (branch != null)
            {
                body.AttachPart(name, parent.Name);
                results.Add(branch.CreateSnapshot());
            }
        }
        return results;
    }

    /// <summary>root.thicken(part_name, amount) — increase root thickness.</summary>
    public static bool Thicken(PlantBody body, OrganismEntity org, string partName, float amount)
    {
        if (amount <= 0f) return false;
        PlantPart part = body.FindPart(partName);
        if (part == null) return false;

        float cost = amount * GrowthEnergyCostPerUnit * 0.5f;
        if (!TrySpendEnergy(org, cost)) return false;

        part.TrySetProperty("thickness", GetPropertyFloat(part, "thickness") + amount);
        return true;
    }

    // ── Absorption ──────────────────────────────────────────────────

    /// <summary>root.absorb(resource) — pull resource from soil.</summary>
    public static double Absorb(PlantBody body, OrganismEntity org, ResourceGrid world, string resource)
    {
        if (string.IsNullOrEmpty(resource)) return 0d;

        float rootArea = GetTotalRootArea(body);
        float rate = rootArea * BaseAbsorptionRate;

        // Get soil concentration from world
        string soilKey = "soil_" + resource.Trim().ToLowerInvariant();
        double soilConcentration = 0d;
        if (world.TryGetWorldValue(soilKey, out object soilVal) && TryConvertToDouble(soilVal, out double sv))
            soilConcentration = sv;

        double absorbed = rate * soilConcentration;

        // Apply to organism
        string normalized = resource.Trim().ToLowerInvariant();
        if (normalized == "water")
        {
            org.TryAddState("water", absorbed, out _, out _);
        }
        else
        {
            org.TryAddState(normalized, absorbed, out _, out _);
        }

        return absorbed;
    }

    /// <summary>root.absorb_all() — pull all resources at reduced efficiency.</summary>
    public static double AbsorbAll(PlantBody body, OrganismEntity org, ResourceGrid world)
    {
        double total = 0d;
        total += Absorb(body, org, world, "water") * 0.6;
        total += Absorb(body, org, world, "nitrogen") * 0.6;
        total += Absorb(body, org, world, "phosphorus") * 0.6;
        return total;
    }

    /// <summary>root.absorb_filtered(resources) — pull specific resources at full efficiency.</summary>
    public static double AbsorbFiltered(PlantBody body, OrganismEntity org, ResourceGrid world, List<object> resources)
    {
        double total = 0d;
        if (resources == null) return total;
        for (int i = 0; i < resources.Count; i++)
        {
            string res = resources[i]?.ToString();
            if (!string.IsNullOrEmpty(res))
                total += Absorb(body, org, world, res);
        }
        return total;
    }

    /// <summary>root.set_absorption_rate(resource, rate) — tune absorption aggressiveness.</summary>
    public static bool SetAbsorptionRate(PlantBody body, string resource, float rate)
    {
        PlantPart main = body.FindPart("root_main");
        if (main == null) return false;
        rate = Mathf.Clamp01(rate);
        main.TrySetProperty("absorption_rate_" + resource.Trim().ToLowerInvariant(), (double)rate);
        return true;
    }

    // ── Interaction ─────────────────────────────────────────────────

    /// <summary>root.deposit(resource, amount) — push resource into soil.</summary>
    public static bool Deposit(OrganismEntity org, ResourceGrid world, string resource, float amount)
    {
        if (string.IsNullOrEmpty(resource) || amount <= 0f) return false;
        string soilKey = "soil_" + resource.Trim().ToLowerInvariant();
        world.TryAddWorldValue(soilKey, amount, out _, out _);
        return true;
    }

    /// <summary>root.exude(chemical, amount) — release chemical into soil.</summary>
    public static bool Exude(OrganismEntity org, ResourceGrid world, string chemical, float amount)
    {
        if (string.IsNullOrEmpty(chemical) || amount <= 0f) return false;
        string key = "soil_exudate_" + chemical.Trim().ToLowerInvariant();
        world.TryAddWorldValue(key, amount, out _, out _);
        return true;
    }

    /// <summary>root.anchor(strength) — increase anchoring force.</summary>
    public static bool Anchor(PlantBody body, OrganismEntity org, float strength)
    {
        PlantPart main = FindOrCreateMainRoot(body);
        float cost = strength * 0.2f;
        if (!TrySpendEnergy(org, cost)) return false;
        main.TrySetProperty("anchor_strength", (double)Mathf.Clamp01(strength));
        return true;
    }

    /// <summary>root.connect_fungi(network) — connect to mycorrhizal network.</summary>
    public static bool ConnectFungi(PlantBody body, OrganismEntity org, string network)
    {
        PlantPart main = FindOrCreateMainRoot(body);
        main.TrySetProperty("fungi_connected", true);
        main.TrySetProperty("fungi_network", network ?? "default");
        return true;
    }

    // ── Sensing ─────────────────────────────────────────────────────

    /// <summary>root.sense_depth() — current max root depth.</summary>
    public static double SenseDepth(PlantBody body)
    {
        PlantPart main = body.FindPart("root_main");
        if (main == null) return 0d;
        return GetPropertyFloat(main, "depth");
    }

    /// <summary>root.sense_moisture(direction) — moisture at root tips.</summary>
    public static double SenseMoisture(ResourceGrid world)
    {
        if (world.TryGetWorldValue("moisture", out object val) && TryConvertToDouble(val, out double m))
            return m;
        return 0.5;
    }

    /// <summary>root.sense_obstacle(direction) — detect obstacles.</summary>
    public static object SenseObstacle(ResourceGrid world, string direction)
    {
        // Check if bedrock or obstacle is nearby
        if (world.TryGetWorldValue("terrain_bedrock_depth", out object bd) && TryConvertToDouble(bd, out double depth))
        {
            var info = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["type"] = "bedrock",
                ["distance"] = depth,
                ["direction"] = direction ?? "down",
            };
            return info;
        }
        return null;
    }

    /// <summary>root.sense_neighbors() — detect nearby root systems.</summary>
    public static List<object> SenseNeighbors()
    {
        // Returns empty list for now; populated by neighbor detection system later
        return new List<object>();
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PlantPart FindOrCreateMainRoot(PlantBody body)
    {
        PlantPart main = body.FindPart("root_main");
        if (main == null)
            main = body.CreatePart("root_main", RootType, 1f, 0.1f);
        return main;
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

    private static float GetTotalRootArea(PlantBody body)
    {
        var roots = body.FindPartsByType(RootType);
        float area = 0f;
        for (int i = 0; i < roots.Count; i++)
            area += roots[i].Size;
        return Mathf.Max(area, 0.1f);
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
