using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class UIPlantGraphic : MaskableGraphic
{
    private const float Scale = 0.08f;
    private const float MinWidth = 0.005f;
    private const float MaxWidth = 0.08f;
    private const float Padding = 4f;

    private PlantBody _body;
    private readonly List<SegmentData> _segments = new List<SegmentData>();
    private readonly Dictionary<string, Vector2> _tips =
        new Dictionary<string, Vector2>(System.StringComparer.Ordinal);

    private struct SegmentData
    {
        public Vector2[] points;
        public float startWidth, endWidth;
        public Color color;
        public bool loop;
    }

    public void SetBody(PlantBody body)
    {
        _body = body;
        SetVerticesDirty();
    }

    public void Refresh()
    {
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_body == null || _body.Parts.Count == 0)
            return;

        BuildSegments();

        if (_segments.Count == 0)
            return;

        // Compute bounding box of all segment points
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < _segments.Count; i++)
        {
            var pts = _segments[i].points;
            for (int j = 0; j < pts.Length; j++)
            {
                if (pts[j].x < minX) minX = pts[j].x;
                if (pts[j].x > maxX) maxX = pts[j].x;
                if (pts[j].y < minY) minY = pts[j].y;
                if (pts[j].y > maxY) maxY = pts[j].y;
            }
        }

        float plantW = maxX - minX;
        float plantH = maxY - minY;
        if (plantW < 0.001f) plantW = 0.1f;
        if (plantH < 0.001f) plantH = 0.1f;

        Rect r = rectTransform.rect;
        float availW = r.width - Padding * 2f;
        float availH = r.height - Padding * 2f;
        if (availW <= 0 || availH <= 0) return;

        float scale = Mathf.Min(availW / plantW, availH / plantH);
        float cx = (minX + maxX) * 0.5f;
        float cy = (minY + maxY) * 0.5f;
        Vector2 center = r.center;

        // Map plant-space point to rect-space
        System.Func<Vector2, Vector2> map = (Vector2 p) =>
        {
            return new Vector2(
                center.x + (p.x - cx) * scale,
                center.y + (p.y - cy) * scale
            );
        };

        for (int i = 0; i < _segments.Count; i++)
        {
            var seg = _segments[i];
            var pts = seg.points;

            if (seg.loop && pts.Length >= 3)
            {
                // Draw closed shape as triangle fan
                Vector2[] mapped = new Vector2[pts.Length];
                for (int j = 0; j < pts.Length; j++)
                    mapped[j] = map(pts[j]);

                // Find centroid
                Vector2 centroid = Vector2.zero;
                for (int j = 0; j < mapped.Length; j++)
                    centroid += mapped[j];
                centroid /= mapped.Length;

                for (int j = 0; j < mapped.Length; j++)
                {
                    int next = (j + 1) % mapped.Length;
                    AddTriangle(vh, centroid, mapped[j], mapped[next], seg.color);
                }
            }
            else
            {
                // Draw line segments as quads
                float widthScale = scale;
                for (int j = 0; j < pts.Length - 1; j++)
                {
                    float t = pts.Length > 2 ? (float)j / (pts.Length - 2) : 0f;
                    float w = Mathf.Lerp(seg.startWidth, seg.endWidth, t) * widthScale;
                    w = Mathf.Max(w, 1f); // minimum 1px
                    AddQuad(vh, map(pts[j]), map(pts[j + 1]), w, seg.color);
                }
            }
        }
    }

    private void BuildSegments()
    {
        _segments.Clear();
        _tips.Clear();

        if (_body == null) return;

        float opacity = 1f;
        if (_body.TryGetMorphology("opacity", out object opObj) && opObj is double opD)
            opacity = (float)opD;

        // Pre-calc stem height
        float stemHeight = 0f;
        for (int i = 0; i < _body.Parts.Count; i++)
        {
            if (string.Equals(_body.Parts[i].PartType, "stem",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                stemHeight = F(_body.Parts[i], "height", _body.Parts[i].Size) * Scale;
            }
        }

        for (int i = 0; i < _body.Parts.Count; i++)
        {
            PlantPart part = _body.Parts[i];

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
    }

    private void LayoutStem(PlantPart part, float opacity)
    {
        float h = F(part, "height", part.Size) * Scale;
        float thick = F(part, "thickness", 1f);
        float w = Mathf.Clamp(thick * 0.02f, MinWidth, MaxWidth);
        Color c = PartColor(part, opacity);

        _segments.Add(new SegmentData
        {
            points = new[] { Vector2.zero, new Vector2(0f, h) },
            startWidth = w, endWidth = w * 0.6f,
            color = c, loop = false
        });

        _tips[part.Name] = new Vector2(0f, h);
    }

    private void LayoutRoot(PlantPart part, float opacity)
    {
        Vector2 origin = ParentTip(part);
        float d = F(part, "depth", part.Size) * Scale;
        float spread = F(part, "spread", 0f) * Scale;

        int side = StableHash(part.Name) % 2 == 0 ? 1 : -1;
        float xOff = spread > 0f ? spread * side : side * d * 0.3f;
        Color c = PartColor(part, opacity);

        _segments.Add(new SegmentData
        {
            points = new[] { origin, new Vector2(origin.x + xOff, origin.y - d) },
            startWidth = MinWidth * 2f, endWidth = MinWidth,
            color = c, loop = false
        });

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
        Color c = PartColor(part, opacity);

        _segments.Add(new SegmentData
        {
            points = new[] { start, new Vector2(start.x + dx, start.y + dy) },
            startWidth = MinWidth * 1.5f, endWidth = MinWidth,
            color = c, loop = false
        });

        _tips[part.Name] = new Vector2(start.x + dx, start.y + dy);
    }

    private void LayoutLeaf(PlantPart part, float opacity)
    {
        Vector2 tip = ParentTip(part);
        float s = Mathf.Sqrt(part.Size) * Scale * 0.5f;

        int side = StableHash(part.Name) % 2 == 0 ? 1 : -1;
        float xOff = s * 0.5f * side;
        Color c = PartColor(part, opacity);

        _segments.Add(new SegmentData
        {
            points = new[]
            {
                new Vector2(tip.x + xOff, tip.y),
                new Vector2(tip.x + xOff + s * 0.4f, tip.y + s * 0.5f),
                new Vector2(tip.x + xOff, tip.y + s),
                new Vector2(tip.x + xOff - s * 0.4f, tip.y + s * 0.5f),
            },
            startWidth = MinWidth, endWidth = MinWidth,
            color = c, loop = true
        });

        _tips[part.Name] = new Vector2(tip.x + xOff, tip.y + s);
    }

    private void LayoutSegment(PlantPart part, float opacity)
    {
        Vector2 origin = ParentTip(part);
        float len = part.Size * Scale;
        float angle = F(part, "angle", 0f);

        float dx = Mathf.Sin(angle) * len;
        float dy = Mathf.Cos(angle) * len;
        Color c = PartColor(part, opacity);

        _segments.Add(new SegmentData
        {
            points = new[] { origin, new Vector2(origin.x + dx, origin.y + dy) },
            startWidth = MinWidth * 1.5f, endWidth = MinWidth,
            color = c, loop = false
        });

        _tips[part.Name] = new Vector2(origin.x + dx, origin.y + dy);
    }

    private void LayoutDefault(PlantPart part, float opacity)
    {
        Vector2 origin = ParentTip(part);
        Color c = PartColor(part, opacity);

        _segments.Add(new SegmentData
        {
            points = new[] { origin, new Vector2(origin.x, origin.y + 0.05f) },
            startWidth = MinWidth, endWidth = MinWidth,
            color = c, loop = false
        });

        _tips[part.Name] = new Vector2(origin.x, origin.y + 0.05f);
    }

    private static void AddQuad(VertexHelper vh, Vector2 from, Vector2 to, float width, Color color)
    {
        Vector2 dir = (to - from);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();
        Vector2 perp = new Vector2(-dir.y, dir.x) * (width * 0.5f);

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        int idx = vh.currentVertCount;

        v.position = from - perp; vh.AddVert(v);
        v.position = from + perp; vh.AddVert(v);
        v.position = to + perp;   vh.AddVert(v);
        v.position = to - perp;   vh.AddVert(v);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }

    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        int idx = vh.currentVertCount;

        v.position = a; vh.AddVert(v);
        v.position = b; vh.AddVert(v);
        v.position = c; vh.AddVert(v);

        vh.AddTriangle(idx, idx + 1, idx + 2);
    }

    private static Color PartColor(PlantPart part, float opacity)
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
