using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Nutrient storage for an organism, exposed as org.nutrients in Growl.
/// Implements IDictionary so the runtime treats it like other state proxies.
/// 10 nutrients initialized at 0.5, with total/deficiencies/surplus methods
/// dispatched via GrowlDictMethods-style attribute access.
/// </summary>
public sealed class NutrientStore : IDictionary
{
    private const float DefaultLevel = 0.5f;
    private const float DeficiencyThreshold = 0.2f;
    private const float SurplusThreshold = 0.8f;

    private static readonly string[] NutrientNames =
    {
        "nitrogen", "phosphorus", "potassium", "iron", "calcium",
        "sulfur", "zinc", "copper", "magnesium", "carbon"
    };

    private readonly Dictionary<string, float> _levels = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public NutrientStore()
    {
        for (int i = 0; i < NutrientNames.Length; i++)
            _levels[NutrientNames[i]] = DefaultLevel;
    }

    public float GetLevel(string nutrient)
    {
        return _levels.TryGetValue(nutrient, out float val) ? val : 0f;
    }

    public void SetLevel(string nutrient, float value)
    {
        string key = nutrient?.ToLowerInvariant() ?? "";
        _levels[key] = Math.Max(0f, Math.Min(1f, value));
    }

    public float Total()
    {
        float sum = 0f;
        foreach (var kvp in _levels)
            sum += kvp.Value;
        return sum;
    }

    public List<object> Deficiencies()
    {
        var result = new List<object>();
        foreach (var kvp in _levels)
        {
            if (kvp.Value < DeficiencyThreshold)
                result.Add(kvp.Key);
        }
        return result;
    }

    public List<object> Surplus()
    {
        var result = new List<object>();
        foreach (var kvp in _levels)
        {
            if (kvp.Value > SurplusThreshold)
                result.Add(kvp.Key);
        }
        return result;
    }

    // ── IDictionary implementation ─────────────────────────────────

    public object this[object key]
    {
        get
        {
            string k = (key?.ToString() ?? "").ToLowerInvariant();
            return _levels.TryGetValue(k, out float val) ? (object)(double)val : null;
        }
        set
        {
            string k = (key?.ToString() ?? "").ToLowerInvariant();
            if (TryConvertToFloat(value, out float f))
                _levels[k] = Math.Max(0f, Math.Min(1f, f));
        }
    }

    public ICollection Keys
    {
        get
        {
            var keys = new List<object>(_levels.Count);
            foreach (var k in _levels.Keys) keys.Add(k);
            return keys;
        }
    }

    public ICollection Values
    {
        get
        {
            var vals = new List<object>(_levels.Count);
            foreach (var v in _levels.Values) vals.Add((double)v);
            return vals;
        }
    }

    public bool IsReadOnly => false;
    public bool IsFixedSize => false;
    public int Count => _levels.Count;
    public object SyncRoot => this;
    public bool IsSynchronized => false;

    public void Add(object key, object value) { this[key] = value; }
    public void Clear() { foreach (var k in NutrientNames) _levels[k] = 0f; }

    public bool Contains(object key)
    {
        string k = (key?.ToString() ?? "").ToLowerInvariant();
        return _levels.ContainsKey(k);
    }

    public void Remove(object key) { }

    public IDictionaryEnumerator GetEnumerator()
    {
        var snapshot = new Dictionary<object, object>(_levels.Count);
        foreach (var kvp in _levels)
            snapshot[kvp.Key] = (double)kvp.Value;
        return ((IDictionary)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo(Array array, int index)
    {
        var snapshot = new Dictionary<object, object>(_levels.Count);
        foreach (var kvp in _levels)
            snapshot[kvp.Key] = (double)kvp.Value;
        ((IDictionary)snapshot).CopyTo(array, index);
    }

    private static bool TryConvertToFloat(object value, out float number)
    {
        switch (value)
        {
            case float f: number = f; return true;
            case double d: number = (float)d; return true;
            case int i: number = i; return true;
            case long l: number = l; return true;
            default: number = 0f; return false;
        }
    }
}
