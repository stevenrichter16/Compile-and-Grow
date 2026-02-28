using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ResourceGrid : MonoBehaviour
{
    [Header("Global Environment")]
    [SerializeField] private float worldPower = 100f;
    [SerializeField] private float worldTemperature = 22f;
    [SerializeField] private float worldMoisture = 0.5f;

    private readonly Dictionary<string, object> _customWorldValues = new Dictionary<string, object>(StringComparer.Ordinal);

    public bool TryGetWorldValue(string key, out object value)
    {
        switch (NormalizeKey(key))
        {
            case "power":
                value = worldPower;
                return true;
            case "temperature":
                value = worldTemperature;
                return true;
            case "moisture":
                value = worldMoisture;
                return true;
        }

        return _customWorldValues.TryGetValue(key ?? string.Empty, out value);
    }

    public bool TrySetWorldValue(string key, object value, out string errorMessage)
    {
        switch (NormalizeKey(key))
        {
            case "power":
                if (!TryConvertToFloat(value, out worldPower))
                {
                    errorMessage = "world.power expects a numeric value.";
                    return false;
                }
                errorMessage = null;
                return true;

            case "temperature":
                if (!TryConvertToFloat(value, out worldTemperature))
                {
                    errorMessage = "world.temperature expects a numeric value.";
                    return false;
                }
                errorMessage = null;
                return true;

            case "moisture":
                if (!TryConvertToFloat(value, out worldMoisture))
                {
                    errorMessage = "world.moisture expects a numeric value.";
                    return false;
                }

                worldMoisture = Mathf.Clamp01(worldMoisture);
                errorMessage = null;
                return true;
        }

        _customWorldValues[key ?? string.Empty] = value;
        errorMessage = null;
        return true;
    }

    public bool TryAddWorldValue(string key, double delta, out object result, out string errorMessage)
    {
        string normalized = NormalizeKey(key);
        switch (normalized)
        {
            case "power":
                worldPower += (float)delta;
                result = worldPower;
                errorMessage = null;
                return true;

            case "temperature":
                worldTemperature += (float)delta;
                result = worldTemperature;
                errorMessage = null;
                return true;

            case "moisture":
                worldMoisture = Mathf.Clamp01(worldMoisture + (float)delta);
                result = worldMoisture;
                errorMessage = null;
                return true;
        }

        if (_customWorldValues.TryGetValue(key ?? string.Empty, out object existing) &&
            TryConvertToDouble(existing, out double current))
        {
            double next = current + delta;
            object boxed = IsWhole(next) ? (object)(long)Math.Round(next) : next;
            _customWorldValues[key ?? string.Empty] = boxed;
            result = boxed;
            errorMessage = null;
            return true;
        }

        object first = IsWhole(delta) ? (object)(long)Math.Round(delta) : delta;
        _customWorldValues[key ?? string.Empty] = first;
        result = first;
        errorMessage = null;
        return true;
    }

    public Dictionary<string, object> CreateWorldSnapshot()
    {
        var snapshot = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["power"] = worldPower,
            ["temperature"] = worldTemperature,
            ["moisture"] = worldMoisture,
        };

        foreach (KeyValuePair<string, object> pair in _customWorldValues)
            snapshot[pair.Key] = pair.Value;

        return snapshot;
    }

    private static string NormalizeKey(string key)
    {
        return (key ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static bool TryConvertToFloat(object value, out float number)
    {
        if (TryConvertToDouble(value, out double asDouble))
        {
            number = (float)asDouble;
            return true;
        }

        number = 0f;
        return false;
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
