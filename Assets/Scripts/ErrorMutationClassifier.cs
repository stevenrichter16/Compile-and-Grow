using System.Collections.Generic;
using GrowlLanguage.Runtime;
using UnityEngine;

/// <summary>
/// Maps Growl runtime errors to biological mutations on the organism.
/// Core design principle: bad code makes weird organisms, not dead terminals.
/// </summary>
public static class ErrorMutationClassifier
{
    private static readonly System.Random Rng = new System.Random();

    public static void ApplyMutation(OrganismEntity org, RuntimeMessage msg)
    {
        if (org == null) return;

        string code = (msg.Code ?? msg.Kind.ToString()).ToLowerInvariant();

        if (code.Contains("type") || code.Contains("typemismatch"))
        {
            ApplyPropertyDrift(org);
        }
        else if (code.Contains("index") || code.Contains("bounds"))
        {
            ApplyStuntedGrowth(org);
        }
        else if (code.Contains("division") || code.Contains("dividebyzero"))
        {
            ApplyEnergySpike(org);
        }
        else if (code.Contains("none") || code.Contains("null"))
        {
            ApplyPhantomGrowth(org);
        }
        else if (code.Contains("infinite") || code.Contains("loop"))
        {
            ApplyCancerousGrowth(org);
        }
        else if (code.Contains("stackoverflow") || code.Contains("stack"))
        {
            // Organism split — not implemented yet, just stress
            org.TryAddState("stress", 0.2, out _, out _);
            Debug.Log($"[Mutation] {org.OrganismName}: StackOverflow — stress increased (split not yet implemented)", org);
        }
        else if (code.Contains("energy") || code.Contains("budget"))
        {
            ApplyPartShedding(org);
        }
        else if (code.Contains("key") || code.Contains("notfound"))
        {
            ApplySensoryConfusion(org);
        }
        else
        {
            // Default: small stress bump
            org.TryAddState("stress", 0.05, out _, out _);
        }
    }

    // TypeError → Random property drift (color shift on a random part, +/- 0.1)
    private static void ApplyPropertyDrift(OrganismEntity org)
    {
        var parts = org.Body.Parts;
        if (parts.Count == 0)
        {
            org.TryAddState("stress", 0.05, out _, out _);
            return;
        }

        PlantPart part = parts[Rng.Next(parts.Count)];
        float drift = (float)(Rng.NextDouble() * 0.2 - 0.1); // -0.1 to +0.1

        // Drift the color_r property (or create it)
        string colorProp = "color_r";
        if (part.TryGetProperty(colorProp, out object existing) && existing is double d)
            part.TrySetProperty(colorProp, d + drift);
        else
            part.TrySetProperty(colorProp, 0.5 + drift);

        Debug.Log($"[Mutation] {org.OrganismName}: TypeError — color drift on {part.Name}", org);
    }

    // IndexOutOfBounds → Stunted growth (shrink a random part by 10%)
    private static void ApplyStuntedGrowth(OrganismEntity org)
    {
        var parts = org.Body.Parts;
        if (parts.Count == 0)
        {
            org.TryAddState("stress", 0.05, out _, out _);
            return;
        }

        PlantPart part = parts[Rng.Next(parts.Count)];
        part.Size *= 0.9f;
        Debug.Log($"[Mutation] {org.OrganismName}: IndexOutOfBounds — stunted growth on {part.Name}", org);
    }

    // DivisionByZero → Energy spike then crash (energy +5, then health -0.1)
    private static void ApplyEnergySpike(OrganismEntity org)
    {
        org.TryAddState("energy", 5.0, out _, out _);
        org.TryAddState("health", -0.1, out _, out _);
        Debug.Log($"[Mutation] {org.OrganismName}: DivisionByZero — energy spike then wilt", org);
    }

    // NoneAccess → Phantom growth (create a tiny vestigial part)
    private static void ApplyPhantomGrowth(OrganismEntity org)
    {
        string name = "vestigial_" + Rng.Next(1000);
        org.Body.CreatePart(name, "vestigial", size: 0.1f, energyCost: 0.01f);
        Debug.Log($"[Mutation] {org.OrganismName}: NoneAccess — phantom growth '{name}'", org);
    }

    // InfiniteLoop → Cancerous growth (grow a random part by 50%)
    private static void ApplyCancerousGrowth(OrganismEntity org)
    {
        var parts = org.Body.Parts;
        if (parts.Count == 0)
        {
            ApplyPhantomGrowth(org);
            return;
        }

        PlantPart part = parts[Rng.Next(parts.Count)];
        part.Size *= 1.5f;

        // Mark in memory so it persists for 10 ticks
        org.SetMemoryValue("_cancer_part", part.Name);
        org.SetMemoryValue("_cancer_ticks", 10L);
        Debug.Log($"[Mutation] {org.OrganismName}: InfiniteLoop — cancerous growth on {part.Name}", org);
    }

    // EnergyOverBudget → Part shedding (remove smallest part)
    private static void ApplyPartShedding(OrganismEntity org)
    {
        var parts = org.Body.Parts;
        if (parts.Count == 0)
        {
            org.TryAddState("stress", 0.1, out _, out _);
            return;
        }

        PlantPart smallest = parts[0];
        for (int i = 1; i < parts.Count; i++)
        {
            if (parts[i].Size < smallest.Size)
                smallest = parts[i];
        }

        org.Body.RemovePart(smallest.Name);
        Debug.Log($"[Mutation] {org.OrganismName}: EnergyOverBudget — shed {smallest.Name}", org);
    }

    // KeyNotFound → Sensory confusion (stress +0.1)
    private static void ApplySensoryConfusion(OrganismEntity org)
    {
        org.TryAddState("stress", 0.1, out _, out _);
        Debug.Log($"[Mutation] {org.OrganismName}: KeyNotFound — sensory confusion", org);
    }
}
