using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OrganismEntity))]
public sealed class PlantVisualizer : MonoBehaviour
{
    private const float Scale = 0.08f;
    private const float MinWidth = 0.005f;
    private const float MaxWidth = 0.08f;

    private OrganismEntity _entity;
    private PlantBody _body;
    private Transform _container;
    private Material _lineMaterial;

    private readonly Dictionary<string, LineRenderer> _lines =
        new Dictionary<string, LineRenderer>(System.StringComparer.Ordinal);
    private readonly Dictionary<string, Vector2> _tips =
        new Dictionary<string, Vector2>(System.StringComparer.Ordinal);
    private int _lastPartCount = -1;

    private void Awake()
    {
        _entity = GetComponent<OrganismEntity>();
        _body = _entity.Body;

        var containerGo = new GameObject("[Visuals]");
        containerGo.transform.SetParent(transform, false);
        _container = containerGo.transform;

        var shader = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        _lineMaterial = new Material(shader);
    }

    private void LateUpdate()
    {
        if (_body == null) return;
        if (_body.PartCount != _lastPartCount)
            Rebuild();
    }

    private void OnDestroy()
    {
        if (_lineMaterial != null)
            Destroy(_lineMaterial);
    }

    public void Rebuild()
    {
        if (_entity == null || _body == null) return;

        _tips.Clear();
        _lastPartCount = _body.PartCount;

        float opacity = 1f;
        if (_body.TryGetMorphology("opacity", out object opObj) && opObj is double opD)
            opacity = (float)opD;

        // Pre-calc stem height (branches need it for branch_height)
        float stemHeight = 0f;
        for (int i = 0; i < _body.Parts.Count; i++)
        {
            if (string.Equals(_body.Parts[i].PartType, "stem",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                stemHeight = F(_body.Parts[i], "height", _body.Parts[i].Size) * Scale;
            }
        }

        var active = new HashSet<string>(System.StringComparer.Ordinal);

        for (int i = 0; i < _body.Parts.Count; i++)
        {
            PlantPart part = _body.Parts[i];
            active.Add(part.Name);

            switch ((part.PartType ?? "").ToLowerInvariant())
            {
                case "stem":    LayoutStem(part, opacity); break;
                case "root":    LayoutRoot(part, opacity); break;
                case "branch":  LayoutBranch(part, stemHeight, opacity); break;
                case "leaf":    LayoutLeaf(part, opacity); break;
                case "segment": LayoutSegment(part, opacity); break;
                default:        LayoutDefault(part, opacity); break;
            }
        }

        PruneStaleLines(active);
    }

    private void LayoutStem(PlantPart part, float opacity)
    {
        float h = F(part, "height", part.Size) * Scale;
        float thick = F(part, "thickness", 1f);
        float w = Mathf.Clamp(thick * 0.02f, MinWidth, MaxWidth);

        LineRenderer lr = GetOrCreateLine(part.Name, 1);
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPosition(0, Vector3.zero);
        lr.SetPosition(1, new Vector3(0f, h, 0f));
        lr.startWidth = w;
        lr.endWidth = w * 0.6f;

        Color c = PartColor(part, opacity);
        lr.startColor = c;
        lr.endColor = c;

        _tips[part.Name] = new Vector2(0f, h);
    }

    private void LayoutRoot(PlantPart part, float opacity)
    {
        Vector2 origin = ParentTip(part);
        float d = F(part, "depth", part.Size) * Scale;
        float spread = F(part, "spread", 0f) * Scale;

        int side = StableHash(part.Name) % 2 == 0 ? 1 : -1;
        float xOff = spread > 0f ? spread * side : side * d * 0.3f;

        LineRenderer lr = GetOrCreateLine(part.Name, -1);
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(origin.x, origin.y, 0f));
        lr.SetPosition(1, new Vector3(origin.x + xOff, origin.y - d, 0f));
        lr.startWidth = MinWidth * 2f;
        lr.endWidth = MinWidth;

        Color c = PartColor(part, opacity);
        lr.startColor = c;
        lr.endColor = c;

        _tips[part.Name] = new Vector2(origin.x + xOff, origin.y - d);
    }

    private void LayoutBranch(PlantPart part, float stemH, float opacity)
    {
        float bh = F(part, "branch_height", stemH / Scale) * Scale;
        float angle = F(part, "branch_angle", 0.785f);
        float len = part.Size * Scale;

        int side = StableHash(part.Name) % 2 == 0 ? 1 : -1;
        float dx = Mathf.Sin(angle) * len * side;
        float dy = Mathf.Cos(angle) * len;

        Vector2 start = new Vector2(0f, Mathf.Min(bh, stemH));

        LineRenderer lr = GetOrCreateLine(part.Name, 2);
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(start.x, start.y, 0f));
        lr.SetPosition(1, new Vector3(start.x + dx, start.y + dy, 0f));
        lr.startWidth = MinWidth * 1.5f;
        lr.endWidth = MinWidth;

        Color c = PartColor(part, opacity);
        lr.startColor = c;
        lr.endColor = c;

        _tips[part.Name] = new Vector2(start.x + dx, start.y + dy);
    }

    private void LayoutLeaf(PlantPart part, float opacity)
    {
        Vector2 tip = ParentTip(part);
        float s = Mathf.Sqrt(part.Size) * Scale * 0.5f;

        int side = StableHash(part.Name) % 2 == 0 ? 1 : -1;
        float xOff = s * 0.5f * side;

        LineRenderer lr = GetOrCreateLine(part.Name, 3);
        lr.loop = true;
        lr.positionCount = 4;
        lr.SetPosition(0, new Vector3(tip.x + xOff, tip.y, 0f));
        lr.SetPosition(1, new Vector3(tip.x + xOff + s * 0.4f, tip.y + s * 0.5f, 0f));
        lr.SetPosition(2, new Vector3(tip.x + xOff, tip.y + s, 0f));
        lr.SetPosition(3, new Vector3(tip.x + xOff - s * 0.4f, tip.y + s * 0.5f, 0f));
        lr.startWidth = MinWidth;
        lr.endWidth = MinWidth;

        Color c = PartColor(part, opacity);
        lr.startColor = c;
        lr.endColor = c;

        _tips[part.Name] = new Vector2(tip.x + xOff, tip.y + s);
    }

    private void LayoutSegment(PlantPart part, float opacity)
    {
        Vector2 origin = ParentTip(part);
        float len = part.Size * Scale;
        float angle = F(part, "angle", 0f);

        float dx = Mathf.Sin(angle) * len;
        float dy = Mathf.Cos(angle) * len;

        LineRenderer lr = GetOrCreateLine(part.Name, 2);
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(origin.x, origin.y, 0f));
        lr.SetPosition(1, new Vector3(origin.x + dx, origin.y + dy, 0f));
        lr.startWidth = MinWidth * 1.5f;
        lr.endWidth = MinWidth;

        Color c = PartColor(part, opacity);
        lr.startColor = c;
        lr.endColor = c;

        _tips[part.Name] = new Vector2(origin.x + dx, origin.y + dy);
    }

    private void LayoutDefault(PlantPart part, float opacity)
    {
        Vector2 origin = ParentTip(part);

        LineRenderer lr = GetOrCreateLine(part.Name, 0);
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(origin.x, origin.y, 0f));
        lr.SetPosition(1, new Vector3(origin.x, origin.y + 0.05f, 0f));
        lr.startWidth = MinWidth;
        lr.endWidth = MinWidth;

        Color c = PartColor(part, opacity);
        lr.startColor = c;
        lr.endColor = c;

        _tips[part.Name] = new Vector2(origin.x, origin.y + 0.05f);
    }

    private LineRenderer GetOrCreateLine(string name, int sortOrder)
    {
        if (_lines.TryGetValue(name, out LineRenderer existing))
        {
            existing.sortingOrder = sortOrder;
            return existing;
        }

        var go = new GameObject(name);
        go.transform.SetParent(_container, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.material = _lineMaterial;
        lr.useWorldSpace = false;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;
        lr.sortingOrder = sortOrder;

        _lines[name] = lr;
        return lr;
    }

    private void PruneStaleLines(HashSet<string> active)
    {
        var stale = new List<string>();
        foreach (var kvp in _lines)
        {
            if (!active.Contains(kvp.Key))
                stale.Add(kvp.Key);
        }
        for (int i = 0; i < stale.Count; i++)
        {
            if (_lines.TryGetValue(stale[i], out LineRenderer lr))
            {
                Destroy(lr.gameObject);
                _lines.Remove(stale[i]);
            }
        }
    }

    private Color PartColor(PlantPart part, float opacity)
    {
        Color baseColor;
        switch ((part.PartType ?? "").ToLowerInvariant())
        {
            case "root":
                baseColor = new Color(0.55f, 0.35f, 0.15f);
                break;
            case "stem":
                baseColor = new Color(0.2f, 0.5f, 0.2f);
                break;
            case "branch":
            case "segment":
                baseColor = new Color(0.3f, 0.45f, 0.2f);
                break;
            case "leaf":
                baseColor = new Color(0.3f, 0.75f, 0.3f);
                break;
            case "product":
                baseColor = new Color(0.85f, 0.7f, 0.2f);
                break;
            default:
                baseColor = new Color(0.5f, 0.5f, 0.5f);
                break;
        }

        float cr = F(part, "color_r", -1f);
        float cg = F(part, "color_g", -1f);
        float cb = F(part, "color_b", -1f);

        if (cr >= 0f || cg >= 0f || cb >= 0f)
        {
            Color partCol = new Color(
                cr >= 0f ? cr : baseColor.r,
                cg >= 0f ? cg : baseColor.g,
                cb >= 0f ? cb : baseColor.b
            );
            baseColor = Color.Lerp(baseColor, partCol, 0.5f);
        }

        baseColor *= Mathf.Clamp01(part.Health);
        baseColor.a = Mathf.Clamp01(opacity);

        return baseColor;
    }

    private Vector2 ParentTip(PlantPart part)
    {
        if (part.Parent != null && _tips.TryGetValue(part.Parent.Name, out Vector2 tip))
            return tip;
        return Vector2.zero;
    }

    private static float F(PlantPart part, string key, float fallback)
    {
        if (part.TryGetProperty(key, out object val))
        {
            switch (val)
            {
                case double d: return (float)d;
                case float f: return f;
                case int i:   return i;
                case long l:  return l;
            }
        }
        return fallback;
    }

    private static int StableHash(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int hash = 17;
        for (int i = 0; i < s.Length; i++)
            hash = hash * 31 + s[i];
        return hash & 0x7FFFFFFF;
    }
}
