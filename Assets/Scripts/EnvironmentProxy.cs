using System;
using System.Collections;
using System.Collections.Generic;
using GrowlLanguage.Runtime;

/// <summary>
/// Structured environment proxy exposed as <c>env</c> in Growl.
/// Provides env.soil, env.air, env.light, env.weather as nested dictionaries
/// with sensible defaults. Reads live values from ResourceGrid where available.
/// </summary>
public sealed class EnvironmentProxy : IDictionary
{
    private readonly ResourceGrid _grid;
    private BiologicalContext _bioContext;

    public EnvironmentProxy(ResourceGrid grid)
    {
        _grid = grid;
    }

    public void SetBioContext(BiologicalContext context)
    {
        _bioContext = context;
    }

    public object this[object key]
    {
        get
        {
            string k = (key?.ToString() ?? "").ToLowerInvariant();
            switch (k)
            {
                case "soil":    return BuildSoilDict();
                case "air":     return BuildAirDict();
                case "light":   return BuildLightDict();
                case "weather": return BuildWeatherDict();
                default:        return null;
            }
        }
        set { /* env is read-only */ }
    }

    public ICollection Keys => new List<object> { "soil", "air", "light", "weather" };
    public ICollection Values => new List<object> { BuildSoilDict(), BuildAirDict(), BuildLightDict(), BuildWeatherDict() };
    public bool IsReadOnly => true;
    public bool IsFixedSize => true;
    public int Count => 4;
    public object SyncRoot => this;
    public bool IsSynchronized => false;

    public void Add(object key, object value) { }
    public void Clear() { }
    public void Remove(object key) { }

    public bool Contains(object key)
    {
        string k = (key?.ToString() ?? "").ToLowerInvariant();
        return k == "soil" || k == "air" || k == "light" || k == "weather";
    }

    public IDictionaryEnumerator GetEnumerator()
    {
        var dict = new Dictionary<object, object>
        {
            ["soil"] = BuildSoilDict(),
            ["air"] = BuildAirDict(),
            ["light"] = BuildLightDict(),
            ["weather"] = BuildWeatherDict(),
        };
        return ((IDictionary)dict).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo(Array array, int index)
    {
        var dict = new Dictionary<object, object>
        {
            ["soil"] = BuildSoilDict(),
            ["air"] = BuildAirDict(),
            ["light"] = BuildLightDict(),
            ["weather"] = BuildWeatherDict(),
        };
        ((IDictionary)dict).CopyTo(array, index);
    }

    // ── Sub-dict builders ──────────────────────────────────────────

    private Dictionary<object, object> BuildSoilDict()
    {
        float moisture = 0.5f;
        float temperature = 22f;
        if (_grid != null)
        {
            if (_grid.TryGetWorldValue("moisture", out object mVal) && mVal is float m) moisture = m;
            if (_grid.TryGetWorldValue("temperature", out object tVal) && tVal is float t) temperature = t;
        }

        return new Dictionary<object, object>
        {
            ["moisture"] = (double)moisture,
            ["temperature"] = (double)temperature,
            ["ph"] = 7.0,
            ["depth"] = 100.0,
            ["density"] = 0.5,
            ["type"] = "loam",
            ["organic_matter"] = 0.5,
            ["toxins"] = new List<object>(),
            ["fungi_present"] = false,
        };
    }

    private Dictionary<object, object> BuildAirDict()
    {
        float temperature = 22f;
        float humidity = 0.5f;
        if (_grid != null)
        {
            if (_grid.TryGetWorldValue("temperature", out object tVal) && tVal is float t) temperature = t;
            if (_grid.TryGetWorldValue("moisture", out object mVal) && mVal is float m) humidity = m;
        }

        return new Dictionary<object, object>
        {
            ["temperature"] = (double)temperature,
            ["humidity"] = (double)humidity,
            ["co2"] = 400.0,
            ["oxygen"] = 210000.0,
            ["wind_speed"] = 2.0,
            ["wind_direction"] = new GrowlVector(1.0, 0.0, 0.0),
            ["pressure"] = 1013.0,
            ["toxins"] = new List<object>(),
            ["spores"] = new List<object>(),
            ["chemicals"] = new List<object>(),
        };
    }

    private Dictionary<object, object> BuildLightDict()
    {
        string dayPhase = "morning";
        if (_bioContext != null)
        {
            long tickInDay = _bioContext.CurrentTick % 2400;
            if (tickInDay < 200) dayPhase = "dawn";
            else if (tickInDay < 600) dayPhase = "morning";
            else if (tickInDay < 1000) dayPhase = "noon";
            else if (tickInDay < 1400) dayPhase = "afternoon";
            else if (tickInDay < 1800) dayPhase = "dusk";
            else dayPhase = "night";
        }

        return new Dictionary<object, object>
        {
            ["intensity"] = 1.0,
            ["direction"] = new GrowlVector(0.0, 1.0, 0.0),
            ["spectrum"] = "natural",
            ["day_phase"] = dayPhase,
            ["day_length"] = 1200.0,
            ["uv_index"] = 5.0,
        };
    }

    private Dictionary<object, object> BuildWeatherDict()
    {
        return new Dictionary<object, object>
        {
            ["current"] = "clear",
            ["intensity"] = 0.0,
            ["duration_remaining"] = 0L,
            ["precipitation"] = 0.0,
            ["lightning_risk"] = 0.0,
        };
    }
}
