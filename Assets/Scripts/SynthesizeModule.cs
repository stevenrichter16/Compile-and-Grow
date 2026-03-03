using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements the synthesize module from the API spec.
/// Creates novel materials (Products) from nutrients and energy, and handles output
/// via produce() and emit(). Products are represented as Dictionary&lt;string,object&gt;.
/// </summary>
public static class SynthesizeModule
{
    private const float SynthesizeBaseEnergyCost = 1.5f;
    private const float ProduceEnergyCost = 0.5f;
    private const float EmitEnergyCost = 0.3f;
    private const float EnrichEnergyCost = 0.2f;
    private const float FortifyEnergyCost = 0.1f;

    // ── Core Synthesis ───────────────────────────────────────────────

    /// <summary>synthesize(base, density, water_content, growth_rate, **kwargs) — create a Product.</summary>
    public static Dictionary<string, object> Synthesize(
        OrganismEntity org, string baseName, float density, float waterContent, float growthRate,
        Dictionary<string, object> kwargs)
    {
        if (string.IsNullOrEmpty(baseName)) return null;

        string normalized = baseName.Trim().ToLowerInvariant();
        float costMultiplier = GetBaseCostMultiplier(normalized);
        float cost = SynthesizeBaseEnergyCost * costMultiplier * (0.5f + density);
        if (!TrySpendEnergy(org, cost)) return null;

        var product = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["_is_product"] = true,
            ["base"] = normalized,
            ["density"] = (double)Mathf.Clamp01(density),
            ["water_content"] = (double)Mathf.Clamp01(waterContent),
            ["growth_rate"] = (double)Mathf.Clamp01(growthRate),
            ["quality"] = 0.5,
            ["enrichments"] = new Dictionary<string, object>(StringComparer.Ordinal),
            ["fortifications"] = new Dictionary<string, object>(StringComparer.Ordinal),
            ["coating"] = "none",
            ["form"] = "grain",
        };

        // Apply base-type-specific kwargs
        if (kwargs != null)
        {
            foreach (var kvp in kwargs)
                product["kwarg_" + kvp.Key] = kvp.Value;
        }

        return product;
    }

    // ── Product Methods ──────────────────────────────────────────────

    /// <summary>product.enrich(nutrient, amount) — add nutrient to product.</summary>
    public static Dictionary<string, object> ProductEnrich(
        OrganismEntity org, Dictionary<string, object> product, string nutrient, float amount)
    {
        if (product == null || string.IsNullOrEmpty(nutrient)) return product;
        amount = Mathf.Clamp01(amount);
        if (!TrySpendEnergy(org, EnrichEnergyCost * amount)) return product;

        if (product.TryGetValue("enrichments", out object enrichObj) &&
            enrichObj is Dictionary<string, object> enrichments)
        {
            enrichments[nutrient.Trim().ToLowerInvariant()] = (double)amount;
        }

        // Improve quality
        if (product.TryGetValue("quality", out object qVal) && TryConvertToDouble(qVal, out double quality))
            product["quality"] = Math.Min(1.0, quality + amount * 0.1);

        return product;
    }

    /// <summary>product.fortify(property, value) — enhance non-nutritional property.</summary>
    public static Dictionary<string, object> ProductFortify(
        Dictionary<string, object> product, string property, object value)
    {
        if (product == null || string.IsNullOrEmpty(property)) return product;

        if (product.TryGetValue("fortifications", out object fortObj) &&
            fortObj is Dictionary<string, object> fortifications)
        {
            fortifications[property.Trim().ToLowerInvariant()] = value;
        }

        return product;
    }

    /// <summary>product.set_coating(type) — set outer coating.</summary>
    public static Dictionary<string, object> ProductSetCoating(Dictionary<string, object> product, string type)
    {
        if (product == null) return product;

        string normalized = string.IsNullOrEmpty(type) ? "none" : type.Trim().ToLowerInvariant();
        switch (normalized)
        {
            case "none":
            case "waxy":
            case "shell":
            case "husk":
            case "pulp":
                product["coating"] = normalized;
                break;
            default:
                product["coating"] = normalized; // allow custom coatings
                break;
        }

        return product;
    }

    /// <summary>product.set_form(shape) — set physical shape.</summary>
    public static Dictionary<string, object> ProductSetForm(Dictionary<string, object> product, string shape)
    {
        if (product == null) return product;

        string normalized = string.IsNullOrEmpty(shape) ? "grain" : shape.Trim().ToLowerInvariant();
        product["form"] = normalized;
        return product;
    }

    // ── Output Functions ─────────────────────────────────────────────

    /// <summary>produce(product, location, rate) — grow product on the organism.</summary>
    public static bool Produce(PlantBody body, OrganismEntity org, Dictionary<string, object> product,
        string location, float rate)
    {
        if (product == null) return false;
        if (!TrySpendEnergy(org, ProduceEnergyCost)) return false;

        string loc = string.IsNullOrEmpty(location) ? "tips" : location.Trim().ToLowerInvariant();
        if (rate <= 0f)
        {
            if (product.TryGetValue("growth_rate", out object grVal) && TryConvertToDouble(grVal, out double gr))
                rate = (float)gr;
            else
                rate = 0.5f;
        }

        // Get product base name for part naming
        string baseName = "product";
        if (product.TryGetValue("base", out object baseVal) && baseVal != null)
            baseName = baseVal.ToString();

        // Create a product part on the organism at the specified location
        string partName = "product_" + baseName + "_" + (body.CountPartsByType("product") + 1);
        PlantPart productPart = body.CreatePart(partName, "product", rate, 0.05f);
        if (productPart == null) return false;

        // Store product data on the part
        productPart.TrySetProperty("product_base", baseName);
        productPart.TrySetProperty("product_location", loc);
        productPart.TrySetProperty("product_rate", (double)rate);

        if (product.TryGetValue("coating", out object coating))
            productPart.TrySetProperty("coating", coating);
        if (product.TryGetValue("form", out object form))
            productPart.TrySetProperty("form", form);
        if (product.TryGetValue("quality", out object quality))
            productPart.TrySetProperty("quality", quality);
        if (product.TryGetValue("density", out object density))
            productPart.TrySetProperty("density", density);

        // Attach to appropriate parent based on location
        AttachProductToLocation(body, partName, loc);

        return true;
    }

    /// <summary>emit(product, rate) — release product into environment.</summary>
    public static bool Emit(OrganismEntity org, ResourceGrid world, Dictionary<string, object> product, float rate)
    {
        if (product == null) return false;
        if (rate <= 0f)
        {
            if (product.TryGetValue("growth_rate", out object grVal) && TryConvertToDouble(grVal, out double gr))
                rate = (float)gr;
            else
                rate = 0.5f;
        }

        if (!TrySpendEnergy(org, EmitEnergyCost * rate)) return false;

        string baseName = "unknown";
        if (product.TryGetValue("base", out object baseVal) && baseVal != null)
            baseName = baseVal.ToString();

        // Add emission to world state
        string emissionKey = "emission_" + baseName;
        world.TryAddWorldValue(emissionKey, rate, out _, out _);

        // Store emission record on organism
        org.SetMemoryValue("_last_emission_type", baseName);
        org.SetMemoryValue("_last_emission_rate", (double)rate);

        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static float GetBaseCostMultiplier(string baseName)
    {
        switch (baseName)
        {
            // Food bases — moderate cost
            case "carbohydrate": return 1.0f;
            case "protein": return 1.5f;
            case "lipid": return 1.3f;

            // Material bases — higher cost
            case "fiber": return 1.2f;
            case "resin": return 1.8f;
            case "rubber": return 2.0f;
            case "ite": return 2.5f;

            // Chemical bases — varies
            case "chemical": return 1.5f;

            // Exotic bases — expensive
            case "bioelectric": return 4.0f;
            case "magnetic": return 4.5f;
            case "piezoelectric": return 5.0f;

            default: return 1.0f;
        }
    }

    private static void AttachProductToLocation(PlantBody body, string productPartName, string location)
    {
        string parentName = null;
        switch (location)
        {
            case "tips":
                // Find any stem branch or leaf to attach to
                var stems = body.FindPartsByType("stem");
                if (stems.Count > 0) parentName = stems[stems.Count - 1].Name;
                break;
            case "roots":
                parentName = "root_main";
                break;
            case "stem":
                parentName = "stem_main";
                break;
            case "surface":
            case "internal":
                // No specific parent; exists at organism level
                break;
            default:
                // Treat as specific part name
                parentName = location;
                break;
        }

        if (!string.IsNullOrEmpty(parentName) && body.FindPart(parentName) != null)
            body.AttachPart(productPartName, parentName);
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
