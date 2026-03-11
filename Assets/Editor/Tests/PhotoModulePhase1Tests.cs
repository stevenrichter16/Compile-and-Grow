using GrowlLanguage.Runtime;
using NUnit.Framework;
using System;
using UnityEngine;

[TestFixture]
public class PhotoModulePhase1Tests
{
    private readonly System.Collections.Generic.List<GameObject> _objects = new System.Collections.Generic.List<GameObject>();

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
    public void Process_ProducesEnergyAndGlucose_WhenInputsExist()
    {
        var harness = CreateHarness();
        SeedPlant(harness.Body, leafSize: 1.4f, rootSize: 1.2f);
        harness.Org.TrySetState("water", 0.8d, out _);

        double energy = PhotoModule.Process(harness.Body, harness.Org, harness.Grid);

        Assert.That(energy, Is.GreaterThan(0d));
        Assert.That(GetStateNumber(harness.Org, "glucose"), Is.GreaterThan(0d));
        Assert.That(GetStateNumber(harness.Org, "glucose_per_tick"), Is.GreaterThan(0d));
    }

    [Test]
    public void Process_LowWater_MarksWaterAsLimitingFactor()
    {
        var harness = CreateHarness();
        SeedPlant(harness.Body, leafSize: 1.3f, rootSize: 1.0f, stomata: 0.8f);
        harness.Org.TrySetState("water", 0.01d, out _);

        string limiting = PhotoModule.GetLimitingFactor(harness.Body, harness.Org, harness.Grid);

        Assert.That(limiting, Is.EqualTo("water"));
    }

    [Test]
    public void Process_LowCarbon_MarksCarbonAsLimitingFactor()
    {
        var harness = CreateHarness();
        SeedPlant(harness.Body, leafSize: 1.3f, rootSize: 1.0f, stomata: 0.7f);
        harness.Grid.TrySetWorldValue("air_co2", 0.005d, out _);
        harness.Org.TrySetState("water", 1.0d, out _);

        string limiting = PhotoModule.GetLimitingFactor(harness.Body, harness.Org, harness.Grid);

        Assert.That(limiting, Is.EqualTo("carbon"));
    }

    [Test]
    public void Process_TinyLeafArea_MarksSurfaceAreaAsLimitingFactor()
    {
        var harness = CreateHarness();
        SeedPlant(harness.Body, leafSize: 0.1f, rootSize: 1.0f);

        string limiting = PhotoModule.GetLimitingFactor(harness.Body, harness.Org, harness.Grid);
        double energy = PhotoModule.Process(harness.Body, harness.Org, harness.Grid);

        Assert.That(limiting, Is.EqualTo("surface_area"));
        Assert.That(energy, Is.EqualTo(0d));
    }

    [Test]
    public void Process_ChangesGlucoseOutput_WhenStomataChange()
    {
        var closedHarness = CreateHarness();
        SeedPlant(closedHarness.Body, leafSize: 1.4f, rootSize: 1.2f, stomata: 0.2f);
        closedHarness.Org.TrySetState("water", 1.0d, out _);

        var openHarness = CreateHarness();
        SeedPlant(openHarness.Body, leafSize: 1.4f, rootSize: 1.2f, stomata: 0.8f);
        openHarness.Org.TrySetState("water", 1.0d, out _);

        PhotoModule.Process(closedHarness.Body, closedHarness.Org, closedHarness.Grid);
        PhotoModule.Process(openHarness.Body, openHarness.Org, openHarness.Grid);

        Assert.That(GetStateNumber(openHarness.Org, "glucose_per_tick"), Is.GreaterThan(GetStateNumber(closedHarness.Org, "glucose_per_tick")));
    }

    [Test]
    public void Process_ChangesGlucoseOutput_WhenLightChanges()
    {
        var dimHarness = CreateHarness();
        SeedPlant(dimHarness.Body, leafSize: 1.4f, rootSize: 1.2f, stomata: 0.6f);
        dimHarness.Grid.TrySetWorldValue("power", 20d, out _);
        dimHarness.Org.TrySetState("water", 1.0d, out _);

        var brightHarness = CreateHarness();
        SeedPlant(brightHarness.Body, leafSize: 1.4f, rootSize: 1.2f, stomata: 0.6f);
        brightHarness.Grid.TrySetWorldValue("power", 100d, out _);
        brightHarness.Org.TrySetState("water", 1.0d, out _);

        PhotoModule.Process(dimHarness.Body, dimHarness.Org, dimHarness.Grid);
        PhotoModule.Process(brightHarness.Body, brightHarness.Org, brightHarness.Grid);

        Assert.That(GetStateNumber(brightHarness.Org, "glucose_per_tick"), Is.GreaterThan(GetStateNumber(dimHarness.Org, "glucose_per_tick")));
    }

    [Test]
    public void GetLimitingFactor_ReturnsLastProcessedSnapshot_AfterProcessMutatesWaterState()
    {
        var harness = CreateHarness();
        SeedPlant(harness.Body, leafSize: 1.4f, rootSize: 1.2f, stomata: 0.4f);
        harness.Grid.TrySetWorldValue("power", 100d, out _);
        harness.Grid.TrySetWorldValue("air_co2", 0.04d, out _);
        harness.Org.TrySetState("water", 0.4d, out _);

        double energy = PhotoModule.Process(harness.Body, harness.Org, harness.Grid);

        Assert.That(energy, Is.GreaterThan(0d));
        Assert.That(GetStateString(harness.Org, "limiting_factor"), Is.EqualTo("carbon"));
        Assert.That(PhotoModule.GetLimitingFactor(harness.Body, harness.Org, harness.Grid), Is.EqualTo("carbon"));
    }

    [Test]
    public void Process_UsesStoredStemWater_WhenAvailable()
    {
        var dryHarness = CreateHarness();
        SeedPlant(dryHarness.Body, leafSize: 1.2f, rootSize: 1.0f, stomata: 0.6f);
        dryHarness.Org.TrySetState("water", 0.05d, out _);

        var storedHarness = CreateHarness();
        SeedPlant(storedHarness.Body, leafSize: 1.2f, rootSize: 1.0f, stomata: 0.6f, stemThickness: 1.2f);
        storedHarness.Org.TrySetState("water", 0.35d, out _);
        StemModule.StoreWater(storedHarness.Body, storedHarness.Org, 0.30f);

        PhotoModule.Process(dryHarness.Body, dryHarness.Org, dryHarness.Grid);
        PhotoModule.Process(storedHarness.Body, storedHarness.Org, storedHarness.Grid);

        Assert.That(GetStateNumber(storedHarness.Org, "glucose_per_tick"), Is.GreaterThan(GetStateNumber(dryHarness.Org, "glucose_per_tick")));
    }

    [Test]
    public void RootAbsorb_H2O_UsesWaterPath()
    {
        var harness = CreateHarness();
        SeedPlant(harness.Body, leafSize: 1.0f, rootSize: 1.5f);
        harness.Grid.TrySetWorldValue("soil_water", 0.8d, out _);

        double before = GetStateNumber(harness.Org, "water");
        double absorbed = RootModule.Absorb(harness.Body, harness.Org, harness.Grid, "H2O");
        double after = GetStateNumber(harness.Org, "water");

        Assert.That(absorbed, Is.GreaterThan(0d));
        Assert.That(after, Is.GreaterThan(before));
    }

    [Test]
    public void GrowlBridge_DispatchesPhase1PhotoBuiltins()
    {
        var harness = CreateHarness();
        SeedPlant(harness.Body, leafSize: 1.2f, rootSize: 1.0f, stomata: 1.0f);
        harness.Org.TrySetState("water", 0.8d, out _);

        bool processOk = harness.Bridge.TryInvokeBuiltin(
            "photo_process",
            Array.Empty<RuntimeCallArgument>(),
            out object processResult,
            out string processError);
        bool limitingOk = harness.Bridge.TryInvokeBuiltin(
            "photo_get_limiting_factor",
            Array.Empty<RuntimeCallArgument>(),
            out object limitingResult,
            out string limitingError);

        Assert.That(processOk, Is.True, processError);
        Assert.That(limitingOk, Is.True, limitingError);
        Assert.That(Convert.ToDouble(processResult), Is.GreaterThan(0d));
        Assert.That(limitingResult, Is.EqualTo(PhotoModule.GetLimitingFactor(harness.Body, harness.Org, harness.Grid)));
    }

    private Harness CreateHarness()
    {
        var go = new GameObject("Phase1Harness");
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

    private static void SeedPlant(PlantBody body, float leafSize, float rootSize, float stomata = 0.5f, float stemThickness = 1.0f)
    {
        PlantPart stem = body.CreatePart("stem_main", "stem", 1.0f, 0.1f);
        stem.TrySetProperty("thickness", stemThickness);

        body.CreatePart("root_main", "root", rootSize, 0.1f);
        body.CreatePart("leaf_1", "leaf", leafSize, 0.1f);
        body.AttachPart("root_main", "stem_main");
        body.AttachPart("leaf_1", "stem_main");
        LeafModule.OpenStomata(body, stomata);
    }

    private static double GetStateNumber(OrganismEntity org, string key)
    {
        Assert.That(org.TryGetState(key, out object value), Is.True, $"Missing org state '{key}'.");
        return Convert.ToDouble(value);
    }

    private static string GetStateString(OrganismEntity org, string key)
    {
        Assert.That(org.TryGetState(key, out object value), Is.True, $"Missing org state '{key}'.");
        return value?.ToString();
    }

    private struct Harness
    {
        public OrganismEntity Org;
        public PlantBody Body;
        public ResourceGrid Grid;
        public GrowlGameStateBridge Bridge;
    }
}
