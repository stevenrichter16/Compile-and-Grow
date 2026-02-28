using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SeedInventory : MonoBehaviour
{
    [SerializeField] private int generation;
    [SerializeField] private int count = 1;
    [SerializeField] private float viability = 1f;

    private readonly Dictionary<string, object> _customValues = new Dictionary<string, object>(StringComparer.Ordinal);

    public bool TryGetSeedValue(string key, out object value)
    {
        switch (NormalizeKey(key))
        {
            case "generation":
                value = (long)generation;
                return true;
            case "count":
                value = (long)count;
                return true;
            case "viability":
                value = viability;
                return true;
        }

        return _customValues.TryGetValue(key ?? string.Empty, out value);
    }

    public bool TrySetSeedValue(string key, object value, out string errorMessage)
    {
        switch (NormalizeKey(key))
        {
            case "generation":
                generation = Mathf.Max(0, (int)ToInteger(value));
                errorMessage = null;
                return true;

            case "count":
                count = Mathf.Max(0, (int)ToInteger(value));
                errorMessage = null;
                return true;

            case "viability":
                viability = Mathf.Clamp01(ToFloat(value));
                errorMessage = null;
                return true;
        }

        _customValues[key ?? string.Empty] = value;
        errorMessage = null;
        return true;
    }

    public bool TryAddSeedValue(string key, double delta, out object result, out string errorMessage)
    {
        switch (NormalizeKey(key))
        {
            case "generation":
                generation = Mathf.Max(0, generation + (int)Math.Round(delta));
                result = (long)generation;
                errorMessage = null;
                return true;

            case "count":
                count = Mathf.Max(0, count + (int)Math.Round(delta));
                result = (long)count;
                errorMessage = null;
                return true;

            case "viability":
                viability = Mathf.Clamp01(viability + (float)delta);
                result = viability;
                errorMessage = null;
                return true;
        }

        if (_customValues.TryGetValue(key ?? string.Empty, out object existing) &&
            TryConvertToDouble(existing, out double current))
        {
            double next = current + delta;
            object boxed = IsWhole(next) ? (object)(long)Math.Round(next) : next;
            _customValues[key ?? string.Empty] = boxed;
            result = boxed;
            errorMessage = null;
            return true;
        }

        object first = IsWhole(delta) ? (object)(long)Math.Round(delta) : delta;
        _customValues[key ?? string.Empty] = first;
        result = first;
        errorMessage = null;
        return true;
    }

    public long SpawnSeeds(long amount)
    {
        if (amount <= 0)
            return count;

        count += (int)amount;
        return count;
    }

    public Dictionary<string, object> CreateSeedSnapshot()
    {
        var snapshot = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["generation"] = (long)generation,
            ["count"] = (long)count,
            ["viability"] = viability,
        };

        foreach (KeyValuePair<string, object> pair in _customValues)
            snapshot[pair.Key] = pair.Value;

        return snapshot;
    }

    private static string NormalizeKey(string key)
    {
        return (key ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static float ToFloat(object value)
    {
        return TryConvertToDouble(value, out double number) ? (float)number : 0f;
    }

    private static long ToInteger(object value)
    {
        return TryConvertToDouble(value, out double number) ? (long)Math.Round(number) : 0L;
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

    private static bool IsWhole(double value)
    {
        return Math.Abs(value - Math.Round(value)) < 0.0000001d;
    }
}
