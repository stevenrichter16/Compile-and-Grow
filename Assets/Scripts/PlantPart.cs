using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single body part on an organism. Parts are created dynamically
/// by gene code and registered in the organism's PlantBody parts list.
/// Matches the API spec: part.name, part.type, part.size, part.health, part.age,
/// part.energy_cost, part.properties, part.children, part.parent.
/// </summary>
public sealed class PlantPart
{
    public string Name { get; }
    public string PartType { get; }
    public float Size { get; set; }
    public float Health { get; set; } = 1f;
    public int Age { get; set; }
    public float EnergyCost { get; set; }
    public PlantPart Parent { get; set; }

    private readonly List<PlantPart> _children = new List<PlantPart>();
    private readonly Dictionary<string, object> _properties = new Dictionary<string, object>(StringComparer.Ordinal);

    public IReadOnlyList<PlantPart> Children => _children;
    public IDictionary<string, object> Properties => _properties;

    public PlantPart(string name, string partType, float size = 1f, float energyCost = 0.1f)
    {
        Name = name ?? "unnamed";
        PartType = partType ?? "generic";
        Size = size;
        EnergyCost = energyCost;
    }

    public void AddChild(PlantPart child)
    {
        if (child == null || child == this) return;
        if (child.Parent != null)
            child.Parent.RemoveChild(child);
        child.Parent = this;
        _children.Add(child);
    }

    public bool RemoveChild(PlantPart child)
    {
        if (child == null) return false;
        if (_children.Remove(child))
        {
            child.Parent = null;
            return true;
        }
        return false;
    }

    public bool TryGetProperty(string key, out object value)
    {
        if (string.IsNullOrEmpty(key))
        {
            value = null;
            return false;
        }

        string normalized = key.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "name":
                value = Name;
                return true;
            case "type":
                value = PartType;
                return true;
            case "size":
                value = (double)Size;
                return true;
            case "health":
                value = (double)Health;
                return true;
            case "age":
                value = (long)Age;
                return true;
            case "energy_cost":
                value = (double)EnergyCost;
                return true;
        }

        return _properties.TryGetValue(normalized, out value);
    }

    public bool TrySetProperty(string key, object value)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string normalized = key.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "name":
            case "type":
                return false; // immutable
            case "size":
                Size = Math.Max(0f, ToFloat(value));
                return true;
            case "health":
                Health = Clamp01(ToFloat(value));
                return true;
            case "age":
                Age = Math.Max(0, (int)ToLong(value));
                return true;
            case "energy_cost":
                EnergyCost = Math.Max(0f, ToFloat(value));
                return true;
        }

        _properties[normalized] = value;
        return true;
    }

    public Dictionary<string, object> CreateSnapshot()
    {
        var snapshot = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["name"] = Name,
            ["type"] = PartType,
            ["size"] = (double)Size,
            ["health"] = (double)Health,
            ["age"] = (long)Age,
            ["energy_cost"] = (double)EnergyCost,
            ["parent"] = Parent?.Name,
            ["children_count"] = (long)_children.Count,
        };

        foreach (KeyValuePair<string, object> pair in _properties)
            snapshot[pair.Key] = pair.Value;

        return snapshot;
    }

    public void TickAge()
    {
        Age++;
    }

    private static float ToFloat(object value)
    {
        return TryConvertToDouble(value, out double n) ? (float)n : 0f;
    }

    private static long ToLong(object value)
    {
        return TryConvertToDouble(value, out double n) ? (long)Math.Round(n) : 0L;
    }

    private static float Clamp01(float v)
    {
        return v < 0f ? 0f : v > 1f ? 1f : v;
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
