using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the body parts registry and morphology state for an organism.
/// Attached alongside OrganismEntity. Provides org.parts and org.morphology
/// functionality as defined in the API spec.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlantBody : MonoBehaviour
{
    // ── Morphology state ────────────────────────────────────────────
    [Header("Morphology")]
    [SerializeField] private float height;
    [SerializeField] private float width;
    [SerializeField] private float depth;
    [SerializeField] private float volume;
    [SerializeField] private float surfaceArea;
    [SerializeField] private string symmetry = "radial";
    [SerializeField] private string growthPattern = "apical";
    [SerializeField] private string texture = "smooth";
    [SerializeField] private float rigidity = 0.5f;
    [SerializeField] private float opacity = 1f;
    [SerializeField] private float colorR;
    [SerializeField] private float colorG = 0.5f;
    [SerializeField] private float colorB;
    [SerializeField] private string orientation = "up";

    // ── Parts registry ──────────────────────────────────────────────
    private readonly List<PlantPart> _parts = new List<PlantPart>();
    private readonly Dictionary<string, PlantPart> _partsByName =
        new Dictionary<string, PlantPart>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PlantPart> Parts => _parts;
    public int PartCount => _parts.Count;

    // ── Part operations ─────────────────────────────────────────────

    public PlantPart CreatePart(string name, string type, float size = 1f, float energyCost = 0.1f)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[PlantBody] Cannot create part with empty name.", this);
            return null;
        }

        if (_partsByName.ContainsKey(name))
        {
            Debug.LogWarning($"[PlantBody] Part '{name}' already exists.", this);
            return null;
        }

        var part = new PlantPart(name, type, size, energyCost);
        _parts.Add(part);
        _partsByName[name] = part;
        RecalculateMorphology();
        return part;
    }

    public PlantPart FindPart(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        _partsByName.TryGetValue(name, out PlantPart part);
        return part;
    }

    public List<PlantPart> FindPartsByType(string type)
    {
        var results = new List<PlantPart>();
        if (string.IsNullOrEmpty(type)) return results;

        string normalized = type.Trim().ToLowerInvariant();
        for (int i = 0; i < _parts.Count; i++)
        {
            if (string.Equals(_parts[i].PartType, normalized, StringComparison.OrdinalIgnoreCase))
                results.Add(_parts[i]);
        }
        return results;
    }

    public int CountPartsByType(string type)
    {
        if (string.IsNullOrEmpty(type)) return 0;

        int count = 0;
        string normalized = type.Trim().ToLowerInvariant();
        for (int i = 0; i < _parts.Count; i++)
        {
            if (string.Equals(_parts[i].PartType, normalized, StringComparison.OrdinalIgnoreCase))
                count++;
        }
        return count;
    }

    public bool RemovePart(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!_partsByName.TryGetValue(name, out PlantPart part)) return false;

        // Reparent children to this part's parent
        for (int i = part.Children.Count - 1; i >= 0; i--)
        {
            PlantPart child = part.Children[i];
            if (part.Parent != null)
                part.Parent.AddChild(child);
            else
                child.Parent = null;
        }

        // Detach from parent
        if (part.Parent != null)
            part.Parent.RemoveChild(part);

        _parts.Remove(part);
        _partsByName.Remove(name);
        RecalculateMorphology();
        return true;
    }

    public bool AttachPart(string partName, string toPartName, string position = "tip")
    {
        PlantPart child = FindPart(partName);
        PlantPart parent = FindPart(toPartName);
        if (child == null || parent == null || child == parent) return false;

        // Store position as a property on the child for later use
        parent.AddChild(child);
        child.TrySetProperty("attach_position", position ?? "tip");
        return true;
    }

    public bool GrowPart(string partName, string property, double amount)
    {
        PlantPart part = FindPart(partName);
        if (part == null) return false;

        if (part.TryGetProperty(property, out object current) && TryConvertToDouble(current, out double val))
        {
            part.TrySetProperty(property, val + amount);
        }
        else
        {
            part.TrySetProperty(property, amount);
        }

        RecalculateMorphology();
        return true;
    }

    public bool ShrinkPart(string partName, string property, double amount)
    {
        return GrowPart(partName, property, -amount);
    }

    // ── Morphology get/set ──────────────────────────────────────────

    public bool TryGetMorphology(string key, out object value)
    {
        if (string.IsNullOrEmpty(key))
        {
            value = null;
            return false;
        }

        switch (key.Trim().ToLowerInvariant())
        {
            case "height":
                value = (double)height;
                return true;
            case "width":
                value = (double)width;
                return true;
            case "depth":
                value = (double)depth;
                return true;
            case "volume":
                value = (double)volume;
                return true;
            case "surface_area":
                value = (double)surfaceArea;
                return true;
            case "symmetry":
                value = symmetry;
                return true;
            case "growth_pattern":
                value = growthPattern;
                return true;
            case "texture":
                value = texture;
                return true;
            case "rigidity":
                value = (double)rigidity;
                return true;
            case "opacity":
                value = (double)opacity;
                return true;
            case "color":
                // Return as a list for (r, g, b) tuple access
                value = new List<object> { (double)colorR, (double)colorG, (double)colorB };
                return true;
            case "color_r":
                value = (double)colorR;
                return true;
            case "color_g":
                value = (double)colorG;
                return true;
            case "color_b":
                value = (double)colorB;
                return true;
            case "center_of_mass":
                value = new List<object> { 0.0, (double)(height * 0.4f), 0.0 };
                return true;
            case "orientation":
                value = orientation;
                return true;
        }

        value = null;
        return false;
    }

    public bool TrySetMorphology(string key, object value, out string errorMessage)
    {
        if (string.IsNullOrEmpty(key))
        {
            errorMessage = "Morphology key cannot be empty.";
            return false;
        }

        string normalized = key.Trim().ToLowerInvariant();
        errorMessage = null;

        switch (normalized)
        {
            case "height":
                height = Math.Max(0f, ToFloat(value));
                return true;
            case "width":
                width = Math.Max(0f, ToFloat(value));
                return true;
            case "depth":
                depth = Math.Max(0f, ToFloat(value));
                return true;
            case "volume":
                volume = Math.Max(0f, ToFloat(value));
                return true;
            case "surface_area":
                surfaceArea = Math.Max(0f, ToFloat(value));
                return true;
            case "symmetry":
                string sym = value?.ToString() ?? "radial";
                if (sym != "radial" && sym != "bilateral" && sym != "asymmetric")
                {
                    errorMessage = "Symmetry must be 'radial', 'bilateral', or 'asymmetric'.";
                    return false;
                }
                symmetry = sym;
                return true;
            case "growth_pattern":
                string gp = value?.ToString() ?? "apical";
                growthPattern = gp;
                return true;
            case "texture":
                texture = value?.ToString() ?? "smooth";
                return true;
            case "rigidity":
                rigidity = Clamp01(ToFloat(value));
                return true;
            case "opacity":
                opacity = Clamp01(ToFloat(value));
                return true;
            case "color_r":
                colorR = Clamp01(ToFloat(value));
                return true;
            case "color_g":
                colorG = Clamp01(ToFloat(value));
                return true;
            case "color_b":
                colorB = Clamp01(ToFloat(value));
                return true;
            case "orientation":
                orientation = value?.ToString() ?? "up";
                return true;
        }

        errorMessage = "Unknown morphology key '" + key + "'.";
        return false;
    }

    public Dictionary<string, object> CreateMorphologySnapshot()
    {
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["height"] = (double)height,
            ["width"] = (double)width,
            ["depth"] = (double)depth,
            ["volume"] = (double)volume,
            ["surface_area"] = (double)surfaceArea,
            ["symmetry"] = symmetry,
            ["growth_pattern"] = growthPattern,
            ["texture"] = texture,
            ["rigidity"] = (double)rigidity,
            ["opacity"] = (double)opacity,
            ["color"] = new List<object> { (double)colorR, (double)colorG, (double)colorB },
            ["center_of_mass"] = new List<object> { 0.0, (double)(height * 0.4f), 0.0 },
            ["orientation"] = orientation,
        };
    }

    // ── Surface properties per part ─────────────────────────────────

    public bool SetSurface(string partName, IDictionary properties)
    {
        PlantPart part = FindPart(partName);
        if (part == null) return false;

        foreach (DictionaryEntry entry in properties)
        {
            string propKey = entry.Key?.ToString();
            if (!string.IsNullOrEmpty(propKey))
                part.TrySetProperty("surface_" + propKey.Trim().ToLowerInvariant(), entry.Value);
        }
        return true;
    }

    // ── Tick: age all parts ─────────────────────────────────────────

    public void TickAllParts()
    {
        for (int i = 0; i < _parts.Count; i++)
            _parts[i].TickAge();
    }

    public float GetTotalMaintenanceCost()
    {
        float total = 0f;
        for (int i = 0; i < _parts.Count; i++)
            total += _parts[i].EnergyCost;
        return total;
    }

    // ── Parts list proxy for Growl runtime ───────────────────────────

    /// <summary>
    /// Creates a PartsProxy that acts as the org.parts object in Growl.
    /// Supports org.parts.find("name"), org.parts.find_type("type"), org.parts.count("type").
    /// </summary>
    public PartsProxy CreatePartsProxy()
    {
        return new PartsProxy(this);
    }

    // ── Internal: recalculate morphology from parts ────────────────

    private void RecalculateMorphology()
    {
        float totalVolume = 0f;
        float maxHeight = 0f;
        float maxWidth = 0f;
        float maxDepth = 0f;

        for (int i = 0; i < _parts.Count; i++)
        {
            PlantPart p = _parts[i];
            float partVol = p.Size;
            totalVolume += partVol;

            string type = p.PartType?.ToLowerInvariant() ?? "";
            switch (type)
            {
                case "stem":
                case "trunk":
                    maxHeight += p.Size;
                    break;
                case "root":
                    maxDepth += p.Size;
                    break;
                case "leaf":
                case "branch":
                case "canopy":
                    if (p.Size > maxWidth)
                        maxWidth = p.Size;
                    break;
            }
        }

        volume = totalVolume;

        // Only update derived dimensions if parts contributed to them
        if (maxHeight > 0f) height = maxHeight;
        if (maxWidth > 0f) width = maxWidth;
        if (maxDepth > 0f) depth = maxDepth;

        // Rough surface area estimate
        if (volume > 0f)
            surfaceArea = (float)(4.84 * Math.Pow(volume, 2.0 / 3.0));
    }

    // ── Utility ────────────────────────────────────────────────────

    private static float ToFloat(object value)
    {
        return TryConvertToDouble(value, out double n) ? (float)n : 0f;
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

    // ── PartsProxy: the object exposed as org.parts in Growl ────────

    /// <summary>
    /// Acts as org.parts in the Growl runtime. Implements IDictionary so the
    /// runtime can treat it as a dictionary-like object (consistent with other
    /// proxy objects like org, world, seed). Special keys "find", "find_type",
    /// "count" return delegate-like values; numeric indices return parts by position.
    /// </summary>
    public sealed class PartsProxy : IDictionary
    {
        private readonly PlantBody _body;

        public PartsProxy(PlantBody body)
        {
            _body = body;
        }

        public PlantBody Body => _body;

        public object this[object key]
        {
            get
            {
                string k = key?.ToString() ?? "";

                // Numeric index access: org.parts[0], org.parts[1], etc.
                if (int.TryParse(k, out int index) && index >= 0 && index < _body._parts.Count)
                    return _body._parts[index].CreateSnapshot();

                switch (k.ToLowerInvariant())
                {
                    case "count":
                        return (long)_body._parts.Count;
                    case "length":
                        return (long)_body._parts.Count;
                }

                // Try to find part by name and return its snapshot
                PlantPart part = _body.FindPart(k);
                if (part != null)
                    return part.CreateSnapshot();

                return null;
            }
            set { /* parts are read-only through the proxy; use morph builtins */ }
        }

        public ICollection Keys
        {
            get
            {
                var keys = new List<string>(_body._parts.Count + 2);
                keys.Add("count");
                keys.Add("length");
                for (int i = 0; i < _body._parts.Count; i++)
                    keys.Add(_body._parts[i].Name);
                return keys;
            }
        }

        public ICollection Values
        {
            get
            {
                var vals = new List<object>(_body._parts.Count + 2);
                vals.Add((long)_body._parts.Count);
                vals.Add((long)_body._parts.Count);
                for (int i = 0; i < _body._parts.Count; i++)
                    vals.Add(_body._parts[i].CreateSnapshot());
                return vals;
            }
        }

        public bool IsReadOnly => true;
        public bool IsFixedSize => false;
        public int Count => _body._parts.Count + 2;
        public object SyncRoot => this;
        public bool IsSynchronized => false;

        public void Add(object key, object value) { }
        public void Clear() { }
        public void Remove(object key) { }

        public bool Contains(object key)
        {
            string k = key?.ToString() ?? "";
            if (k == "count" || k == "length") return true;
            return _body.FindPart(k) != null;
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal);
            dict["count"] = (long)_body._parts.Count;
            for (int i = 0; i < _body._parts.Count; i++)
                dict[_body._parts[i].Name] = _body._parts[i].CreateSnapshot();
            return ((IDictionary)dict).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(Array array, int index)
        {
            // minimal implementation for interface compliance
        }
    }
}
