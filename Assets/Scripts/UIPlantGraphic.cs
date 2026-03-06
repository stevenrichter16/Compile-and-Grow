using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class UIPlantGraphic : MaskableGraphic, IPointerMoveHandler, IPointerExitHandler
{
    private const float PadPx = 4f;
    private const float CellGap = 2f;
    private const float SectionGap = 4f;
    private const float MinCell = 6f;
    private const float MaxCell = 40f;
    private const float BorderPx = 1f;

    private static readonly string[] SectionTypes =
        { "root", "stem", "branch", "segment", "leaf", "product" };

    private static readonly Dictionary<string, string> SectionHeaders =
        new Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            { "root", "ROOTS" },
            { "stem", "STEMS" },
            { "branch", "BRANCHES" },
            { "segment", "SEGMENTS" },
            { "leaf", "LEAVES" },
            { "product", "PRODUCTS" },
        };

    private PlantBody _body;

    private struct CatalogNode
    {
        public PlantPart part;
        public Rect rect;
    }

    private struct SectionInfo
    {
        public string typeName;
        public string header;
        public List<PlantPart> parts;
    }

    private static readonly HashSet<string> BuiltInKeys = new HashSet<string>(System.StringComparer.Ordinal)
        { "name", "type", "size", "health", "age", "energy_cost", "color_r", "color_g", "color_b" };

    private readonly List<SectionInfo> _sections = new List<SectionInfo>();
    private readonly List<CatalogNode> _nodes = new List<CatalogNode>();
    private readonly List<TextMeshProUGUI> _headerPool = new List<TextMeshProUGUI>();

    private RectTransform _tooltipRoot;
    private Image _tooltipBg;
    private TextMeshProUGUI _tooltipText;
    private readonly StringBuilder _tooltipSb = new StringBuilder();

    public void SetBody(PlantBody body)
    {
        _body = body;
        HideTooltip();
        RebuildSections();
        SetVerticesDirty();
    }

    public void Refresh()
    {
        HideTooltip();
        RebuildSections();
        SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        HideTooltip();
        for (int i = _headerPool.Count - 1; i >= 0; i--)
        {
            if (_headerPool[i] != null)
                DestroyImmediate(_headerPool[i].gameObject);
        }
        _headerPool.Clear();
    }

    private void RebuildSections()
    {
        _sections.Clear();

        if (_body == null || _body.Parts.Count == 0)
        {
            EnsureHeaderCount(0);
            return;
        }

        // Bucket parts by type
        var buckets = new Dictionary<string, List<PlantPart>>(System.StringComparer.Ordinal);
        for (int i = 0; i < _body.Parts.Count; i++)
        {
            PlantPart part = _body.Parts[i];
            string type = (part.PartType ?? "").ToLowerInvariant();
            if (!buckets.TryGetValue(type, out var list))
            {
                list = new List<PlantPart>();
                buckets[type] = list;
            }
            list.Add(part);
        }

        // Add known types in order
        for (int i = 0; i < SectionTypes.Length; i++)
        {
            if (buckets.TryGetValue(SectionTypes[i], out var parts))
            {
                _sections.Add(new SectionInfo
                {
                    typeName = SectionTypes[i],
                    header = SectionHeaders[SectionTypes[i]],
                    parts = parts,
                });
                buckets.Remove(SectionTypes[i]);
            }
        }

        // Add unknown types at the bottom
        foreach (var kvp in buckets)
        {
            if (kvp.Value.Count == 0) continue;
            _sections.Add(new SectionInfo
            {
                typeName = kvp.Key,
                header = kvp.Key.ToUpperInvariant() + "S",
                parts = kvp.Value,
            });
        }

        EnsureHeaderCount(_sections.Count);
    }

    private void EnsureHeaderCount(int count)
    {
        // Deactivate excess
        for (int i = count; i < _headerPool.Count; i++)
        {
            if (_headerPool[i] != null)
                _headerPool[i].gameObject.SetActive(false);
        }

        // Create missing
        while (_headerPool.Count < count)
        {
            var go = new GameObject("SectionHeader", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.enableWordWrapping = false;
            tmp.fontStyle = FontStyles.Bold;
            _headerPool.Add(tmp);
        }

        // Activate needed
        for (int i = 0; i < count; i++)
        {
            if (_headerPool[i] != null)
                _headerPool[i].gameObject.SetActive(true);
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        _nodes.Clear();

        if (_body == null || _body.Parts.Count == 0 || _sections.Count == 0)
            return;

        Rect r = rectTransform.rect;
        float availW = r.width - PadPx * 2f;
        float availH = r.height - PadPx * 2f;
        if (availW <= 0 || availH <= 0) return;

        float opacity = 1f;
        if (_body.TryGetMorphology("opacity", out object opObj) && opObj is double opD)
            opacity = (float)opD;

        // Compute header height
        float headerH = Mathf.Clamp(r.height * 0.08f, 10f, 18f);

        // Estimate cell size
        int totalParts = 0;
        for (int i = 0; i < _sections.Count; i++)
            totalParts += _sections[i].parts.Count;

        float cellSize = Mathf.Clamp(availW / Mathf.Max(1, Mathf.Sqrt(totalParts) * 1.5f), MinCell, MaxCell);

        // Iteratively fit: compute total height, shrink if needed
        for (int iter = 0; iter < 5; iter++)
        {
            float totalH = ComputeTotalHeight(availW, cellSize, headerH);
            if (totalH <= availH || cellSize <= MinCell) break;
            cellSize *= availH / totalH;
            cellSize = Mathf.Max(cellSize, MinCell);
        }

        cellSize = Mathf.Clamp(cellSize, MinCell, MaxCell);
        float totalHeight = ComputeTotalHeight(availW, cellSize, headerH);

        // Vertical centering
        float startY = r.yMax - PadPx;
        if (totalHeight < availH)
            startY -= (availH - totalHeight) * 0.5f;

        float curY = startY;
        float leftX = r.xMin + PadPx;

        int maxPerRow = Mathf.Max(1, Mathf.FloorToInt((availW + CellGap) / (cellSize + CellGap)));

        for (int si = 0; si < _sections.Count; si++)
        {
            var section = _sections[si];

            // Position header TMP
            if (si < _headerPool.Count && _headerPool[si] != null)
            {
                var tmp = _headerPool[si];
                tmp.text = section.header;
                tmp.fontSize = headerH;
                tmp.color = PartColorForType(section.typeName, opacity);

                var rt = tmp.rectTransform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                // Position relative to parent rect
                rt.anchoredPosition = new Vector2(
                    leftX - r.xMin,
                    curY - r.yMax);
                rt.sizeDelta = new Vector2(availW, headerH);
            }

            curY -= headerH + SectionGap;

            // Layout squares
            int partsCount = section.parts.Count;
            int rows = Mathf.CeilToInt((float)partsCount / maxPerRow);

            for (int pi = 0; pi < partsCount; pi++)
            {
                int col = pi % maxPerRow;
                int row = pi / maxPerRow;

                float x = leftX + col * (cellSize + CellGap);
                float y = curY - row * (cellSize + CellGap) - cellSize;

                _nodes.Add(new CatalogNode
                {
                    part = section.parts[pi],
                    rect = new Rect(x, y, cellSize, cellSize),
                });
            }

            curY -= rows * (cellSize + CellGap);
        }

        // Draw all squares
        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            Color c = PartColor(node.part, opacity);

            // Outer border
            Color border = c * 0.4f;
            border.a = c.a;
            AddFilledRect(vh, node.rect, border);

            // Inner fill (1px inset)
            Rect inner = new Rect(
                node.rect.x + BorderPx,
                node.rect.y + BorderPx,
                node.rect.width - BorderPx * 2f,
                node.rect.height - BorderPx * 2f);

            if (inner.width > 0 && inner.height > 0)
                AddFilledRect(vh, inner, c);
        }
    }

    private float ComputeTotalHeight(float availW, float cellSize, float headerH)
    {
        int maxPerRow = Mathf.Max(1, Mathf.FloorToInt((availW + CellGap) / (cellSize + CellGap)));
        float total = 0f;
        for (int i = 0; i < _sections.Count; i++)
        {
            int rows = Mathf.CeilToInt((float)_sections[i].parts.Count / maxPerRow);
            total += headerH + SectionGap + rows * (cellSize + CellGap);
        }
        return total;
    }

    private static void AddFilledRect(VertexHelper vh, Rect rect, Color color)
    {
        UIVertex v = UIVertex.simpleVert;
        v.color = color;
        int idx = vh.currentVertCount;

        v.position = new Vector3(rect.xMin, rect.yMin); vh.AddVert(v);
        v.position = new Vector3(rect.xMax, rect.yMin); vh.AddVert(v);
        v.position = new Vector3(rect.xMax, rect.yMax); vh.AddVert(v);
        v.position = new Vector3(rect.xMin, rect.yMax); vh.AddVert(v);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }

    private static Color PartColorForType(string type, float opacity)
    {
        Color c;
        switch (type)
        {
            case "root":    c = new Color(0.55f, 0.35f, 0.15f); break;
            case "stem":    c = new Color(0.2f, 0.5f, 0.2f); break;
            case "branch":
            case "segment": c = new Color(0.3f, 0.45f, 0.2f); break;
            case "leaf":    c = new Color(0.3f, 0.75f, 0.3f); break;
            case "product": c = new Color(0.85f, 0.7f, 0.2f); break;
            default:        c = new Color(0.5f, 0.5f, 0.5f); break;
        }
        c.a = Mathf.Clamp01(opacity);
        return c;
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

    // ── Tooltip ──────────────────────────────────────────────

    private void EnsureTooltip()
    {
        if (_tooltipRoot != null) return;

        var go = new GameObject("Tooltip", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        _tooltipRoot = go.GetComponent<RectTransform>();
        _tooltipRoot.anchorMin = new Vector2(0, 1);
        _tooltipRoot.anchorMax = new Vector2(0, 1);
        _tooltipRoot.pivot = new Vector2(0, 1);

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 4, 4);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _tooltipBg = go.AddComponent<Image>();
        _tooltipBg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
        _tooltipBg.raycastTarget = false;

        var textGo = new GameObject("TooltipText", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        _tooltipText = textGo.AddComponent<TextMeshProUGUI>();
        _tooltipText.fontSize = 11f;
        _tooltipText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        _tooltipText.raycastTarget = false;
        _tooltipText.enableWordWrapping = false;
        _tooltipText.overflowMode = TextOverflowModes.Overflow;
        _tooltipText.richText = true;

        go.SetActive(false);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_nodes.Count == 0) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i].rect.Contains(localPoint))
            {
                ShowTooltip(_nodes[i].part, localPoint);
                return;
            }
        }

        HideTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void ShowTooltip(PlantPart part, Vector2 localPoint)
    {
        EnsureTooltip();

        // Build tooltip text
        _tooltipSb.Clear();
        _tooltipSb.Append("<b>").Append(part.Name).Append("</b>\n");
        _tooltipSb.Append(part.PartType)
            .Append(" \u00b7 age ").Append(part.Age)
            .Append(" \u00b7 hp ").Append(Mathf.RoundToInt(part.Health * 100f)).Append("%\n");
        _tooltipSb.Append("size: ").Append(part.Size.ToString("0.##"))
            .Append(" \u00b7 cost: ").Append(part.EnergyCost.ToString("0.##"));

        // Custom properties
        bool hasCustom = false;
        foreach (var kvp in part.Properties)
        {
            if (BuiltInKeys.Contains(kvp.Key)) continue;
            if (!hasCustom)
            {
                _tooltipSb.Append("\n\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500");
                hasCustom = true;
            }
            _tooltipSb.Append('\n').Append(kvp.Key).Append(": ").Append(kvp.Value);
        }

        _tooltipText.text = _tooltipSb.ToString();
        _tooltipText.ForceMeshUpdate();

        // Position: offset right and up from pointer, clamped within bounds
        Rect bounds = rectTransform.rect;
        Vector2 preferred = _tooltipText.GetPreferredValues();
        float tipW = preferred.x + 12f; // padding
        float tipH = preferred.y + 8f;

        float x = localPoint.x + 12f;
        float y = localPoint.y + 12f;

        // Clamp so tooltip stays inside the graphic rect
        if (x + tipW > bounds.xMax) x = localPoint.x - tipW - 4f;
        if (y > bounds.yMax) y = bounds.yMax;
        if (y - tipH < bounds.yMin) y = bounds.yMin + tipH;
        if (x < bounds.xMin) x = bounds.xMin;

        // anchoredPosition is relative to parent anchors (top-left)
        _tooltipRoot.anchoredPosition = new Vector2(
            x - bounds.xMin,
            y - bounds.yMax);

        _tooltipRoot.gameObject.SetActive(true);
    }

    private void HideTooltip()
    {
        if (_tooltipRoot != null)
            _tooltipRoot.gameObject.SetActive(false);
    }
}
