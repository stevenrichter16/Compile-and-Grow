using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PlantDetailController
{
    public event Action OnBack;

    readonly VisualTreeAsset _sectionTemplate;

    VisualElement _root;
    Label _nameLabel;
    VisualElement _statusDot;
    VisualElement _navStrip;
    Button _backBtn;

    // Section bodies keyed by name
    readonly Dictionary<string, VisualElement> _sectionBodies = new Dictionary<string, VisualElement>();
    readonly Dictionary<string, Label> _sectionSummaries = new Dictionary<string, Label>();

    OrganismEntity _organism;
    OrganismEntity[] _allOrganisms;
    int _currentIndex;

    public event Action<OrganismEntity> OnOrganismSwitched;

    static readonly string[] SectionNames =
    {
        "vitals", "genes", "resources", "environment", "parts", "nutrients", "reproduction"
    };

    static readonly string[] SectionTitles =
    {
        "Vitals", "Gene Slots", "Resource Allocation", "Environment",
        "Growth / Parts", "Nutrients", "Reproduction"
    };

    public PlantDetailController(VisualTreeAsset sectionTemplate)
    {
        _sectionTemplate = sectionTemplate;
    }

    public void Initialize(VisualElement detailRoot)
    {
        _root = detailRoot;
        _nameLabel = _root.Q<Label>("detail-name");
        _statusDot = _root.Q("detail-status-dot");
        _navStrip = _root.Q("nav-strip");
        _backBtn = _root.Q<Button>("back-btn");

        _backBtn.clicked += () => OnBack?.Invoke();

        // Build sections from template
        for (int i = 0; i < SectionNames.Length; i++)
        {
            var container = _root.Q($"section-{SectionNames[i]}");
            var sectionInstance = _sectionTemplate.Instantiate();
            var section = sectionInstance.Q("section");

            section.Q<Label>("section-title").text = SectionTitles[i];
            var summary = section.Q<Label>("section-summary");
            var body = section.Q("section-body");

            // Collapse toggle
            var header = section.Q("section-header");
            header.RegisterCallback<ClickEvent>(_ =>
            {
                section.ToggleInClassList("section--collapsed");
            });

            // All sections collapsed by default except vitals
            if (i > 0)
                section.AddToClassList("section--collapsed");

            _sectionBodies[SectionNames[i]] = body;
            _sectionSummaries[SectionNames[i]] = summary;

            container.Add(sectionInstance);
        }
    }

    public void ShowOrganism(OrganismEntity org, OrganismEntity[] allOrganisms, int index)
    {
        _organism = org;
        _allOrganisms = allOrganisms;
        _currentIndex = index;

        _root.RemoveFromClassList("detail-view--visible");
        _root.AddToClassList("detail-view--visible");
        _root.style.display = DisplayStyle.Flex;

        BuildNavDots();
        Refresh();
    }

    public void Hide()
    {
        _root.RemoveFromClassList("detail-view--visible");
        _root.style.display = DisplayStyle.None;
        _organism = null;
    }

    public void Refresh()
    {
        if (_organism == null) return;

        _nameLabel.text = _organism.OrganismName;

        float health = GetFloat(_organism, "health");
        float stress = GetFloat(_organism, "stress");
        float maturity = GetFloat(_organism, "maturity");
        string status = GetStatus(_organism.IsAlive, health, stress, maturity);
        _statusDot.style.backgroundColor = PlantSidebarController.GetStatusColor(status);

        RefreshVitals();
        RefreshGenes();
        RefreshResources();
        RefreshEnvironment();
        RefreshParts();
        RefreshNutrients();
        RefreshReproduction();
    }

    // ── Vitals ──

    void RefreshVitals()
    {
        var body = _sectionBodies["vitals"];
        body.Clear();

        float energy = GetFloat(_organism, "energy");
        float water = GetFloat(_organism, "water");
        float health = GetFloat(_organism, "health");
        float stress = GetFloat(_organism, "stress");
        float maturity = GetFloat(_organism, "maturity");
        long age = GetLong(_organism, "age");

        AddMetricBar(body, "Energy", energy, "metric-bar__fill--energy");
        AddMetricBar(body, "Water", water, "metric-bar__fill--water");
        AddMetricBar(body, "Health", health, "metric-bar__fill--health");
        AddMetricBar(body, "Stress", stress, "metric-bar__fill--stress");
        AddMetricBar(body, "Maturity", maturity, "metric-bar__fill--maturity");

        var ageLabel = new Label($"Age: {age} ticks");
        ageLabel.AddToClassList("env-row__value");
        body.Add(ageLabel);

        _sectionSummaries["vitals"].text =
            $"E:{energy * 100:0} H:{health * 100:0}% W:{water * 100:0}%";
    }

    // ── Gene Slots ──

    void RefreshGenes()
    {
        var body = _sectionBodies["genes"];
        body.Clear();

        var source = _organism.GrowlSource;
        var slots = GrowlSourceScanner.Scan(source);
        var roleFill = GrowlSourceScanner.GetRequiredRoleFillStatus(slots);
        var roleNames = GrowlSourceScanner.GetRequiredRoleNames();

        var grid = new VisualElement();
        grid.AddToClassList("gene-grid");

        // Required roles
        int filledRoles = 0;
        for (int i = 0; i < roleNames.Length; i++)
        {
            var card = new VisualElement();
            card.AddToClassList("gene-card");
            card.AddToClassList("gene-card--role");

            if (!roleFill[i])
                card.AddToClassList("gene-card--empty");
            else
                filledRoles++;

            var typeLabel = new Label("ROLE");
            typeLabel.AddToClassList("gene-card__type");
            card.Add(typeLabel);

            // Find the matching slot for function name
            string fnName = roleNames[i];
            for (int j = 0; j < slots.Count; j++)
            {
                if (slots[j].Type == "role" && slots[j].Name == roleNames[i])
                {
                    fnName = slots[j].FunctionName;
                    break;
                }
            }

            var nameLabel = new Label(roleFill[i] ? fnName : $"({roleNames[i]})");
            nameLabel.AddToClassList("gene-card__name");
            card.Add(nameLabel);

            grid.Add(card);
        }

        // Optional genes
        int geneCount = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].Type != "gene") continue;
            geneCount++;

            var card = new VisualElement();
            card.AddToClassList("gene-card");

            var typeLabel = new Label("GENE");
            typeLabel.AddToClassList("gene-card__type");
            card.Add(typeLabel);

            var nameLabel = new Label(slots[i].FunctionName);
            nameLabel.AddToClassList("gene-card__name");
            card.Add(nameLabel);

            grid.Add(card);
        }

        body.Add(grid);
        _sectionSummaries["genes"].text = $"{filledRoles}/4 roles, {geneCount} genes";
    }

    // ── Resource Allocation ──

    void RefreshResources()
    {
        var body = _sectionBodies["resources"];
        body.Clear();

        float energy = GetFloat(_organism, "energy");
        float maintenanceCost = _organism.Body != null
            ? _organism.Body.GetTotalMaintenanceCost()
            : 0f;

        // Estimate photosynthesis income from leaf count
        int leafCount = 0;
        if (_organism.Body != null)
        {
            var leaves = _organism.Body.FindPartsByType("leaf");
            leafCount = leaves?.Count ?? 0;
        }

        // Memory cost: each key costs 0.1
        int memoryKeys = 0;
        if (_organism.Memory != null)
            memoryKeys = _organism.Memory.Count;
        float memoryCost = memoryKeys * 0.1f;

        float totalCost = maintenanceCost + memoryCost;
        float glucosePerTick = GetFloat(_organism, "glucose_per_tick");
        float netEnergyPerTick = GetFloat(_organism, "net_energy_per_tick");
        float waterEfficiency = GetFloat(_organism, "water_efficiency");
        float lightCapturePct = GetFloat(_organism, "light_capture_pct");
        float rootSupplyRatio = GetFloat(_organism, "root_supply_ratio");
        string limitingFactor = GetString(_organism, "limiting_factor", "none");

        AddEnvRow(body, "Energy", $"{energy:F2}");
        AddEnvRow(body, "Part maintenance", $"-{maintenanceCost:F2}/tick");
        AddEnvRow(body, "Memory cost", $"-{memoryCost:F2}/tick ({memoryKeys} keys)");
        AddEnvRow(body, "Total cost", $"-{totalCost:F2}/tick");
        AddEnvRow(body, "Leaf count", $"{leafCount}");
        AddEnvRow(body, "Glucose / tick", $"{glucosePerTick:F2}");
        AddEnvRow(body, "Net energy / tick", $"{netEnergyPerTick:F2}");
        AddEnvRow(body, "Water efficiency", $"{waterEfficiency:F2}");
        AddEnvRow(body, "Light capture", $"{lightCapturePct:F0}%");
        AddEnvRow(body, "Root supply ratio", $"{rootSupplyRatio * 100f:F0}%");
        AddEnvRow(body, "Limiting factor", limitingFactor);

        // Per-tick resource flows
        AddEnvRow(body, "Water flow", $"+{_organism.WaterGained:F2} / -{_organism.WaterSpent:F2} per tick");
        AddEnvRow(body, "CO2 flow", $"+{_organism.Co2Gained:F2} / -{_organism.Co2Spent:F2} per tick");

        _sectionSummaries["resources"].text = $"glucose:{glucosePerTick:F2} {limitingFactor}";
    }

    // ── Environment ──

    void RefreshEnvironment()
    {
        var body = _sectionBodies["environment"];
        body.Clear();

        var resourceGrid = UnityEngine.Object.FindFirstObjectByType<ResourceGrid>();
        if (resourceGrid == null)
        {
            AddEnvRow(body, "No ResourceGrid", "—");
            _sectionSummaries["environment"].text = "N/A";
            return;
        }

        var snapshot = resourceGrid.CreateWorldSnapshot();
        string summaryText = "";

        foreach (var kv in snapshot)
        {
            string valueStr = FormatEnvValue(kv.Key, kv.Value);
            AddEnvRow(body, kv.Key, valueStr);

            if (kv.Key == "power" || kv.Key == "temperature")
                summaryText += $"{kv.Key}:{valueStr} ";
        }

        _sectionSummaries["environment"].text = summaryText.TrimEnd();
    }

    // ── Growth / Parts ──

    void RefreshParts()
    {
        var body = _sectionBodies["parts"];
        body.Clear();

        var plantBody = _organism.Body;
        if (plantBody == null)
        {
            AddEnvRow(body, "No PlantBody", "—");
            _sectionSummaries["parts"].text = "0 parts";
            return;
        }

        // Morphology summary
        var morph = plantBody.CreateMorphologySnapshot();
        float height = 0f;
        if (morph.TryGetValue("height", out var hVal))
            height = Convert.ToSingle(hVal);

        AddEnvRow(body, "Parts", plantBody.PartCount.ToString());
        if (morph.TryGetValue("height", out var h)) AddEnvRow(body, "Height", $"{Convert.ToSingle(h):F1}");
        if (morph.TryGetValue("volume", out var v)) AddEnvRow(body, "Volume", $"{Convert.ToSingle(v):F1}");
        if (morph.TryGetValue("rigidity", out var r)) AddEnvRow(body, "Rigidity", $"{Convert.ToSingle(r):F2}");

        // Part list
        var parts = plantBody.Parts;
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var row = new VisualElement();
            row.AddToClassList("part-row");

            var dot = new VisualElement();
            dot.AddToClassList("part-row__dot");
            dot.style.backgroundColor = PartTypeColor(part.PartType);
            row.Add(dot);

            var nameLabel = new Label(part.Name);
            nameLabel.AddToClassList("part-row__name");
            row.Add(nameLabel);

            var info = new Label($"s:{part.Size:F1} h:{part.Health:F0}% a:{part.Age}");
            info.AddToClassList("part-row__info");
            row.Add(info);

            body.Add(row);
        }

        _sectionSummaries["parts"].text = $"{plantBody.PartCount} parts, h:{height:F1}";
    }

    // ── Nutrients ──

    void RefreshNutrients()
    {
        var body = _sectionBodies["nutrients"];
        body.Clear();

        var nutrients = _organism.Nutrients;
        if (nutrients == null)
        {
            _sectionSummaries["nutrients"].text = "N/A";
            return;
        }

        string[] names =
        {
            "nitrogen", "phosphorus", "potassium", "iron", "calcium",
            "sulfur", "zinc", "copper", "magnesium", "carbon"
        };

        int deficientCount = 0;

        for (int i = 0; i < names.Length; i++)
        {
            float level = nutrients.GetLevel(names[i]);
            string fillClass = "metric-bar__fill--nutrient";

            if (level < 0.2f)
            {
                fillClass = "metric-bar__fill--nutrient-low";
                deficientCount++;
            }
            else if (level > 0.8f)
            {
                fillClass = "metric-bar__fill--nutrient-high";
            }

            AddMetricBar(body, names[i], level, fillClass);
        }

        _sectionSummaries["nutrients"].text = deficientCount > 0
            ? $"{deficientCount} deficient"
            : "balanced";
    }

    // ── Reproduction ──

    void RefreshReproduction()
    {
        var body = _sectionBodies["reproduction"];
        body.Clear();

        var seedInv = _organism.GetComponent<SeedInventory>();
        float maturity = GetFloat(_organism, "maturity");

        AddMetricBar(body, "Maturity", maturity, "metric-bar__fill--maturity");

        if (seedInv != null)
        {
            seedInv.TryGetSeedValue("generation", out var gen);
            seedInv.TryGetSeedValue("count", out var cnt);
            seedInv.TryGetSeedValue("viability", out var via);

            long generation = gen is long gl ? gl : 0;
            long count = cnt is long cl ? cl : 0;
            float viability = via is float vf ? vf : (via is double vd ? (float)vd : 0f);

            AddEnvRow(body, "Generation", generation.ToString());
            AddEnvRow(body, "Seed count", count.ToString());
            AddEnvRow(body, "Viability", $"{viability * 100:0}%");

            _sectionSummaries["reproduction"].text = $"gen:{generation}, seeds:{count}";
        }
        else
        {
            _sectionSummaries["reproduction"].text = $"mat:{maturity * 100:0}%";
        }
    }

    // ── Nav Dots ──

    void BuildNavDots()
    {
        _navStrip.Clear();

        if (_allOrganisms == null || _allOrganisms.Length <= 1) return;

        for (int i = 0; i < _allOrganisms.Length; i++)
        {
            var dot = new VisualElement();
            dot.AddToClassList("nav-dot");
            if (i == _currentIndex)
                dot.AddToClassList("nav-dot--active");

            int idx = i;
            dot.RegisterCallback<ClickEvent>(_ =>
            {
                if (idx < _allOrganisms.Length && _allOrganisms[idx] != null)
                {
                    _currentIndex = idx;
                    _organism = _allOrganisms[idx];
                    BuildNavDots();
                    Refresh();
                    OnOrganismSwitched?.Invoke(_organism);
                }
            });

            _navStrip.Add(dot);
        }
    }

    // ── Helpers ──

    static void AddMetricBar(VisualElement parent, string label, float ratio, string fillClass)
    {
        var bar = new VisualElement();
        bar.AddToClassList("metric-bar");

        var lbl = new Label(label.Length > 3 ? label.Substring(0, 3) : label);
        lbl.AddToClassList("metric-bar__label");
        if (label.Length > 3) lbl.style.width = 80;
        bar.Add(lbl);

        var track = new VisualElement();
        track.AddToClassList("metric-bar__track");

        var fill = new VisualElement();
        fill.AddToClassList("metric-bar__fill");
        fill.AddToClassList(fillClass);
        fill.style.width = Length.Percent(Mathf.Clamp01(ratio) * 100f);
        track.Add(fill);
        bar.Add(track);

        var val = new Label($"{ratio * 100:0}%");
        val.AddToClassList("metric-bar__value");
        bar.Add(val);

        parent.Add(bar);
    }

    static void AddEnvRow(VisualElement parent, string key, string value)
    {
        var row = new VisualElement();
        row.AddToClassList("env-row");

        var keyLabel = new Label(key);
        keyLabel.AddToClassList("env-row__key");
        row.Add(keyLabel);

        var valLabel = new Label(value);
        valLabel.AddToClassList("env-row__value");
        row.Add(valLabel);

        parent.Add(row);
    }

    static string FormatEnvValue(string key, object val)
    {
        if (val is float f) return $"{f:F2}";
        if (val is double d) return $"{d:F2}";
        return val?.ToString() ?? "—";
    }

    static float GetFloat(OrganismEntity org, string key)
    {
        if (org.TryGetState(key, out var val))
        {
            if (val is float f) return f;
            if (val is double d) return (float)d;
            return Convert.ToSingle(val);
        }
        return 0f;
    }

    static long GetLong(OrganismEntity org, string key)
    {
        if (org.TryGetState(key, out var val))
        {
            if (val is long l) return l;
            return Convert.ToInt64(val);
        }
        return 0;
    }

    static string GetString(OrganismEntity org, string key, string fallback)
    {
        if (org.TryGetState(key, out var val) && val != null)
            return val.ToString();
        return fallback;
    }

    static string GetStatus(bool alive, float health, float stress, float maturity)
    {
        if (!alive) return "dead";
        if (health < 0.3f) return "critical";
        if (stress > 0.7f || health < 0.6f) return "attention";
        if (maturity < 0.01f) return "dormant";
        return "healthy";
    }

    static Color PartTypeColor(string type)
    {
        switch (type?.ToLowerInvariant())
        {
            case "root": return new Color(0.6f, 0.4f, 0.2f);
            case "stem": return new Color(0.2f, 0.7f, 0.3f);
            case "branch":
            case "segment": return new Color(0.5f, 0.6f, 0.2f);
            case "leaf": return new Color(0.3f, 0.9f, 0.3f);
            case "product": return new Color(0.9f, 0.75f, 0.2f);
            default: return Color.gray;
        }
    }
}
