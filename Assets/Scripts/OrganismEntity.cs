using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class OrganismEntity : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string organismName = "unnamed";

    [Header("Growl")]
    [SerializeField, TextArea(3, 10)] private string growlSource = "";

    [Header("Core State")]
    [SerializeField] private bool alive = true;
    [SerializeField] private int age;
    [SerializeField] private float maturity;
    [SerializeField] private float energy = 20f;
    [SerializeField] private float water = 0.5f;
    [SerializeField] private float health = 1f;
    [SerializeField] private float stress;

    private readonly Dictionary<string, object> _customState = new Dictionary<string, object>(StringComparer.Ordinal);
    private readonly Dictionary<string, object> _memory = new Dictionary<string, object>(StringComparer.Ordinal);

    public string OrganismName => organismName;
    public bool IsAlive => alive;
    public IDictionary<string, object> Memory => _memory;

    public string GrowlSource
    {
        get => growlSource;
        set => growlSource = value ?? "";
    }

    public bool TryGetState(string key, out object value)
    {
        switch (NormalizeKey(key))
        {
            case "name":
                value = organismName;
                return true;
            case "alive":
                value = alive;
                return true;
            case "age":
                value = (long)age;
                return true;
            case "maturity":
                value = maturity;
                return true;
            case "energy":
                value = energy;
                return true;
            case "water":
                value = water;
                return true;
            case "health":
                value = health;
                return true;
            case "stress":
                value = stress;
                return true;
            case "memory":
                value = _memory;
                return true;
        }

        return _customState.TryGetValue(key ?? string.Empty, out value);
    }

    public bool TrySetState(string key, object value, out string errorMessage)
    {
        string normalized = NormalizeKey(key);

        switch (normalized)
        {
            case "name":
                organismName = value == null ? "unnamed" : value.ToString();
                errorMessage = null;
                return true;

            case "alive":
                alive = ToBoolean(value);
                errorMessage = null;
                return true;

            case "age":
                age = Mathf.Max(0, (int)ToInteger(value));
                errorMessage = null;
                return true;

            case "maturity":
                maturity = Mathf.Clamp01(ToFloat(value));
                errorMessage = null;
                return true;

            case "energy":
                energy = ToFloat(value);
                errorMessage = null;
                return true;

            case "water":
                water = Mathf.Clamp01(ToFloat(value));
                errorMessage = null;
                return true;

            case "health":
                health = Mathf.Clamp01(ToFloat(value));
                if (health <= 0f)
                    alive = false;
                else if (!alive)
                    alive = true;
                errorMessage = null;
                return true;

            case "stress":
                stress = Mathf.Clamp01(ToFloat(value));
                errorMessage = null;
                return true;

            case "memory":
                errorMessage = "org.memory is read-only as a container; set memory entries via org.memory[...] or org_memory_set.";
                return false;
        }

        _customState[key ?? string.Empty] = value;
        errorMessage = null;
        return true;
    }

    public bool TryAddState(string key, double delta, out object result, out string errorMessage)
    {
        switch (NormalizeKey(key))
        {
            case "age":
                age = Mathf.Max(0, age + (int)Math.Round(delta));
                result = (long)age;
                errorMessage = null;
                return true;

            case "maturity":
                maturity = Mathf.Clamp01(maturity + (float)delta);
                result = maturity;
                errorMessage = null;
                return true;

            case "energy":
                energy += (float)delta;
                result = energy;
                errorMessage = null;
                return true;

            case "water":
                water = Mathf.Clamp01(water + (float)delta);
                result = water;
                errorMessage = null;
                return true;

            case "health":
                health = Mathf.Clamp01(health + (float)delta);
                if (health <= 0f)
                    alive = false;
                else
                    alive = true;
                result = health;
                errorMessage = null;
                return true;

            case "stress":
                stress = Mathf.Clamp01(stress + (float)delta);
                result = stress;
                errorMessage = null;
                return true;
        }

        if (_customState.TryGetValue(key ?? string.Empty, out object existing) &&
            TryConvertToDouble(existing, out double current))
        {
            double next = current + delta;
            object boxed = IsWhole(next) ? (object)(long)Math.Round(next) : next;
            _customState[key ?? string.Empty] = boxed;
            result = boxed;
            errorMessage = null;
            return true;
        }

        object first = IsWhole(delta) ? (object)(long)Math.Round(delta) : delta;
        _customState[key ?? string.Empty] = first;
        result = first;
        errorMessage = null;
        return true;
    }

    public double ApplyDamage(double amount)
    {
        if (amount < 0d)
            amount = 0d;

        health = Mathf.Clamp01(health - (float)amount);
        if (health <= 0f)
            alive = false;

        return health;
    }

    public double ApplyHeal(double amount)
    {
        if (amount < 0d)
            amount = 0d;

        health = Mathf.Clamp01(health + (float)amount);
        if (health > 0f)
            alive = true;

        return health;
    }

    public bool TryGetMemoryValue(string key, out object value)
    {
        if (string.IsNullOrEmpty(key))
        {
            value = null;
            return false;
        }

        return _memory.TryGetValue(key, out value);
    }

    public void SetMemoryValue(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
            return;

        _memory[key] = value;
    }

    public Dictionary<string, object> CreateStateSnapshot()
    {
        var snapshot = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["name"] = organismName,
            ["alive"] = alive,
            ["age"] = (long)age,
            ["maturity"] = maturity,
            ["energy"] = energy,
            ["water"] = water,
            ["health"] = health,
            ["stress"] = stress,
            ["memory"] = _memory,
        };

        foreach (KeyValuePair<string, object> pair in _customState)
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

    private static bool ToBoolean(object value)
    {
        switch (value)
        {
            case bool b:
                return b;
            case string s:
                return string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
            default:
                return TryConvertToDouble(value, out double n) && Math.Abs(n) > double.Epsilon;
        }
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
