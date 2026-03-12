using GrowlLanguage.Runtime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[TestFixture]
public class Phase2GrowthResourceTests
{
    private readonly List<GameObject> _objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            if (_objects[i] != null)
                UnityEngine.Object.DestroyImmediate(_objects[i]);
        }

        _objects.Clear();
    }

    [Test]
    public void RootGrowth_FailsWithoutGlucose_AndDoesNotSpendEnergy()
    {
        var harness = CreateHarness();
        harness.Org.TrySetState("energy", 2.0d, out _);
        harness.Org.TrySetState("glucose", 0.0d, out _);

        double energyBefore = GetStateNumber(harness.Org, "energy");
        bool grew = RootModule.GrowDown(harness.Body, harness.Org, 1.0f);

        Assert.That(grew, Is.False);
        Assert.That(GetStateNumber(harness.Org, "energy"), Is.EqualTo(energyBefore).Within(0.0001d));
        Assert.That(GetStateNumber(harness.Org, "glucose"), Is.EqualTo(0d).Within(0.0001d));
    }

    [Test]
    public void RootGrowth_SucceedsWithEnergyAndGlucose_AndUsesLowerGlucoseCostThanLeaves()
    {
        var harness = CreateHarness();
        harness.Org.TrySetState("energy", 2.0d, out _);
        harness.Org.TrySetState("glucose", 1.0d, out _);

        double energyBefore = GetStateNumber(harness.Org, "energy");
        double glucoseBefore = GetStateNumber(harness.Org, "glucose");

        bool grew = RootModule.GrowDown(harness.Body, harness.Org, 1.0f);

        Assert.That(grew, Is.True);
        Assert.That(GetStateNumber(harness.Org, "energy"), Is.EqualTo(energyBefore - 0.5d).Within(0.0001d));
        Assert.That(GetStateNumber(harness.Org, "glucose"), Is.EqualTo(glucoseBefore - 0.15d).Within(0.0001d));
    }

    [Test]
    public void LeafGrowth_SucceedsWithEnergyAndGlucose_AndSpendsBoth()
    {
        var harness = CreateHarness();
        harness.Body.CreatePart("stem_main", "stem", 1.0f, 0.1f);
        harness.Org.TrySetState("energy", 2.0d, out _);
        harness.Org.TrySetState("glucose", 1.0d, out _);

        double energyBefore = GetStateNumber(harness.Org, "energy");
        double glucoseBefore = GetStateNumber(harness.Org, "glucose");

        object result = LeafModule.Grow(harness.Body, harness.Org, 1.0f, "stem_main");

        Assert.That(result, Is.Not.Null);
        Assert.That(GetStateNumber(harness.Org, "energy"), Is.EqualTo(energyBefore - 0.3d).Within(0.0001d));
        Assert.That(GetStateNumber(harness.Org, "glucose"), Is.EqualTo(glucoseBefore - 0.12d).Within(0.0001d));
        Assert.That(harness.Body.CountPartsByType("leaf"), Is.EqualTo(1));
    }

    [Test]
    public void StemGrowth_SucceedsWithEnergyAndGlucose_AndSpendsBoth()
    {
        var harness = CreateHarness();
        var stem = harness.Body.CreatePart("stem_main", "stem", 1.0f, 0.1f);
        stem.TrySetProperty("material", "fibrous");
        harness.Org.TrySetState("energy", 2.0d, out _);
        harness.Org.TrySetState("glucose", 1.0d, out _);

        double energyBefore = GetStateNumber(harness.Org, "energy");
        double glucoseBefore = GetStateNumber(harness.Org, "glucose");

        bool grew = StemModule.GrowUp(harness.Body, harness.Org, 1.0f);

        Assert.That(grew, Is.True);
        Assert.That(GetStateNumber(harness.Org, "energy"), Is.EqualTo(energyBefore - 0.6d).Within(0.0001d));
        Assert.That(GetStateNumber(harness.Org, "glucose"), Is.EqualTo(glucoseBefore - 0.36d).Within(0.0001d));
    }

    [Test]
    public void Growth_DoesNotConsumeStoredGlucose_WhenOrganismGlucoseIsMissing()
    {
        var harness = CreateHarness();
        var stem = harness.Body.CreatePart("stem_main", "stem", 1.0f, 0.1f);
        stem.TrySetProperty("material", "fibrous");
        stem.TrySetProperty("stored_glucose", 1.0d);
        StemModule.RefreshStorageMetrics(harness.Body, harness.Org);

        harness.Org.TrySetState("energy", 2.0d, out _);
        harness.Org.TrySetState("glucose", 0.0d, out _);

        double energyBefore = GetStateNumber(harness.Org, "energy");
        bool grew = StemModule.GrowUp(harness.Body, harness.Org, 1.0f);

        Assert.That(grew, Is.False);
        Assert.That(GetStateNumber(harness.Org, "energy"), Is.EqualTo(energyBefore).Within(0.0001d));
        Assert.That(GetStateNumber(harness.Org, "stored_glucose"), Is.EqualTo(1.0d).Within(0.0001d));
    }

    [Test]
    public void MorphCreatePart_SpendsEnergy_WithoutRequiringGlucose()
    {
        var harness = CreateHarness();
        harness.Org.TrySetState("energy", 1.0d, out _);
        harness.Org.TrySetState("glucose", 0.0d, out _);

        bool ok = harness.Bridge.TryInvokeBuiltin(
            "morph_create_part",
            new[]
            {
                new RuntimeCallArgument("name", "stem_main"),
                new RuntimeCallArgument("type", "stem"),
                new RuntimeCallArgument("size", 1.0d),
                new RuntimeCallArgument("energy_cost", 0.25d),
            },
            out object result,
            out string errorMessage);

        Assert.That(ok, Is.True, errorMessage);
        Assert.That(result, Is.Not.Null);
        Assert.That(GetStateNumber(harness.Org, "energy"), Is.EqualTo(0.75d).Within(0.0001d));
        Assert.That(GetStateNumber(harness.Org, "glucose"), Is.EqualTo(0.0d).Within(0.0001d));
        Assert.That(harness.Body.FindPart("stem_main"), Is.Not.Null);
    }

    [Test]
    public void GrowlBridge_LogsLeafGrowthFailure_WithEnergyAndGlucoseRequirements()
    {
        var harness = CreateHarness();
        harness.Body.CreatePart("stem_main", "stem", 1.0f, 0.1f);
        harness.Org.TrySetState("energy", 1.0d, out _);
        harness.Org.TrySetState("glucose", 0.0d, out _);
        harness.Bridge.ClearActionLog();

        bool ok = harness.Bridge.TryInvokeBuiltin(
            "leaf_grow",
            new[]
            {
                new RuntimeCallArgument("area", 1.0d),
                new RuntimeCallArgument("from_part", "stem_main"),
            },
            out object result,
            out string errorMessage);

        Assert.That(ok, Is.True, errorMessage);
        Assert.That(result, Is.Null);

        string log = string.Join("\n", harness.Bridge.ActionLog);
        Assert.That(log, Does.Contain("leaf.grow failed"));
        Assert.That(log, Does.Contain("needs 0.30 energy and 0.12 glucose"));
        Assert.That(log, Does.Contain("have 1.00 energy and 0.00 glucose"));
    }

    [Test]
    public void GrowlBridge_LogsStemGrowthFailure_WithEnergyAndGlucoseRequirements()
    {
        var harness = CreateHarness();
        harness.Org.TrySetState("energy", 0.2d, out _);
        harness.Org.TrySetState("glucose", 0.1d, out _);
        harness.Bridge.ClearActionLog();

        bool ok = harness.Bridge.TryInvokeBuiltin(
            "stem_grow_up",
            new[]
            {
                new RuntimeCallArgument("distance", 1.0d),
            },
            out object result,
            out string errorMessage);

        Assert.That(ok, Is.True, errorMessage);
        Assert.That(result, Is.EqualTo(false));

        string log = string.Join("\n", harness.Bridge.ActionLog);
        Assert.That(log, Does.Contain("stem.grow.up failed"));
        Assert.That(log, Does.Contain("needs 0.60 energy and 0.36 glucose"));
        Assert.That(log, Does.Contain("have 0.20 energy and 0.10 glucose"));
    }

    private Harness CreateHarness()
    {
        var go = new GameObject("Phase2GrowthHarness");
        _objects.Add(go);

        var org = go.AddComponent<OrganismEntity>();
        var grid = go.AddComponent<ResourceGrid>();
        var bridge = go.AddComponent<GrowlGameStateBridge>();
        bridge.SetOrganism(org);

        return new Harness
        {
            Org = org,
            Body = org.Body,
            Grid = grid,
            Bridge = bridge,
        };
    }

    private static double GetStateNumber(OrganismEntity org, string key)
    {
        Assert.That(org.TryGetState(key, out object value), Is.True, $"Missing org state '{key}'.");
        return Convert.ToDouble(value);
    }

    private struct Harness
    {
        public OrganismEntity Org;
        public PlantBody Body;
        public ResourceGrid Grid;
        public GrowlGameStateBridge Bridge;
    }
}
