using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PlantSidebarController
{
    public event Action<OrganismEntity> OnPlantSelected;

    readonly VisualTreeAsset _cardTemplate;
    VisualElement _root;
    ScrollView _cardScroll;
    Label _countLabel;

    readonly List<OrganismEntity> _organisms = new List<OrganismEntity>();
    readonly List<VisualElement> _cards = new List<VisualElement>();
    readonly Dictionary<int, float[]> _prevValues = new Dictionary<int, float[]>();

    static readonly Color ArrowGreen = new Color(76f / 255, 175f / 255, 80f / 255);
    static readonly Color ArrowRed = new Color(239f / 255, 83f / 255, 80f / 255);

    public PlantSidebarController(VisualTreeAsset cardTemplate)
    {
        _cardTemplate = cardTemplate;
    }

    public void Initialize(VisualElement sidebarRoot)
    {
        _root = sidebarRoot;
        _cardScroll = _root.Q<ScrollView>("card-scroll");
        _countLabel = _root.Q<Label>("plant-count");
    }

    public void Show()
    {
        _root.RemoveFromClassList("sidebar--hidden");
    }

    public void Hide()
    {
        _root.AddToClassList("sidebar--hidden");
    }

    public void Refresh()
    {
        // Discover organisms
        var found = UnityEngine.Object.FindObjectsByType<OrganismEntity>(FindObjectsSortMode.None);
        _organisms.Clear();
        _organisms.AddRange(found);

        _countLabel.text = _organisms.Count.ToString();

        SyncCards();
        UpdateCardValues();
    }

    void SyncCards()
    {
        // Add cards if needed
        while (_cards.Count < _organisms.Count)
        {
            var cardInstance = _cardTemplate.Instantiate();
            var card = cardInstance.Q("plant-card");
            int index = _cards.Count;
            card.RegisterCallback<ClickEvent>(_ => OnCardClicked(index));
            _cardScroll.Add(cardInstance);
            _cards.Add(card);
        }

        // Hide excess cards
        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].parent.style.display =
                i < _organisms.Count ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    void UpdateCardValues()
    {
        for (int i = 0; i < _organisms.Count; i++)
        {
            var org = _organisms[i];
            var card = _cards[i];

            // Name
            card.Q<Label>("plant-name").text = org.OrganismName;

            // Read state values
            float energy = GetFloat(org, "energy");
            float glucose = GetFloat(org, "glucose");
            float health = GetFloat(org, "health");
            float water = GetFloat(org, "water");
            float storedWater = GetStoredWater(org);
            float stress = GetFloat(org, "stress");
            float maturity = GetFloat(org, "maturity");
            float waterEfficiency = GetFloat(org, "water_efficiency");
            bool alive = org.IsAlive;

            // Age
            long age = 0;
            if (org.TryGetState("age", out var ageVal))
                age = ageVal is long l ? l : Convert.ToInt64(ageVal);
            card.Q<Label>("plant-age").text = $"t:{age}";

            // Stat values + trend arrows
            int id = org.GetInstanceID();
            _prevValues.TryGetValue(id, out float[] prev);

            SetStat(card, "energy-value", "energy-arrow", energy, prev?[0]);
            SetStat(card, "glucose-value", "glucose-arrow", glucose, prev?[1]);
            SetStat(card, "water-current-value", "water-current-arrow", water, prev?[2]);
            SetValue(card, "stored-water-value", storedWater);
            SetValue(card, "water-gained-value", org.WaterGained);
            SetValue(card, "water-lost-value", org.WaterSpent);
            SetValue(card, "water-efficiency-value", waterEfficiency);
            SetStat(card, "health-value", "health-arrow", health, prev?[3]);

            _prevValues[id] = new[] { energy, glucose, water, health };

            // Status
            string status = GetStatus(alive, health, stress, maturity);
            ApplyStatusClass(card, status);

            // Growth health bar (continuous color on left border)
            // Multiplicative: energy decline drives bar to deep red
            float energyDelta = prev != null ? energy - prev[0] : 0f;
            float energyFactor = Mathf.Clamp01(0.5f + energyDelta);
            float baseFitness = 0.4f * health + 0.3f * water + 0.3f * (1f - stress);
            float growthHealth = Mathf.Clamp01(energyFactor * baseFitness);
            card.style.borderLeftColor = GrowthHealthColor(growthHealth);

            // Status dot color
            var dot = card.Q("status-dot");
            dot.style.backgroundColor = GetStatusColor(status);
        }
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

    static void SetStat(VisualElement card, string valueName, string arrowName, float current, float? previous)
    {
        card.Q<Label>(valueName).text = current.ToString("0.00");

        var arrow = card.Q<Label>(arrowName);
        if (!previous.HasValue || Mathf.Approximately(current, previous.Value))
        {
            arrow.text = "";
        }
        else if (current > previous.Value)
        {
            arrow.text = "\u25B2";
            arrow.style.color = ArrowGreen;
        }
        else
        {
            arrow.text = "\u25BC";
            arrow.style.color = ArrowRed;
        }
    }

    static void SetValue(VisualElement card, string valueName, float current)
    {
        card.Q<Label>(valueName).text = current.ToString("0.00");
    }

    static float GetStoredWater(OrganismEntity org)
    {
        if (org == null || org.Body == null)
            return 0f;

        PlantPart storagePart = org.Body.FindPart("stem_main");
        if (storagePart == null)
        {
            var stems = org.Body.FindPartsByType("stem");
            if (stems != null && stems.Count > 0)
                storagePart = stems[0];
        }

        if (storagePart == null || !storagePart.TryGetProperty("stored_water", out object value))
            return 0f;

        if (value is float f) return f;
        if (value is double d) return (float)d;
        if (value is int i) return i;
        if (value is long l) return l;
        return 0f;
    }

    static string GetStatus(bool alive, float health, float stress, float maturity)
    {
        if (!alive) return "dead";
        if (health < 0.3f) return "critical";
        if (stress > 0.7f || health < 0.6f) return "attention";
        if (maturity < 0.01f) return "dormant";
        return "healthy";
    }

    static readonly string[] StatusClasses =
    {
        "plant-card--healthy", "plant-card--attention",
        "plant-card--critical", "plant-card--dormant", "plant-card--dead"
    };

    static void ApplyStatusClass(VisualElement card, string status)
    {
        for (int i = 0; i < StatusClasses.Length; i++)
            card.RemoveFromClassList(StatusClasses[i]);
        card.AddToClassList($"plant-card--{status}");
    }

    internal static Color GetStatusColor(string status)
    {
        switch (status)
        {
            case "healthy":   return new Color(76f/255, 175f/255, 80f/255);
            case "attention":  return new Color(1f, 183f/255, 77f/255);
            case "critical":   return new Color(239f/255, 83f/255, 80f/255);
            case "dormant":    return new Color(120f/255, 120f/255, 130f/255);
            case "dead":       return new Color(80f/255, 80f/255, 80f/255);
            default:           return Color.gray;
        }
    }

    static Color GrowthHealthColor(float t)
    {
        if (t < 0.5f)
            return Color.Lerp(new Color(239f/255, 83f/255, 80f/255), new Color(1f, 183f/255, 77f/255), t * 2f);
        return Color.Lerp(new Color(1f, 183f/255, 77f/255), new Color(76f/255, 175f/255, 80f/255), (t - 0.5f) * 2f);
    }

    void OnCardClicked(int index)
    {
        if (index < _organisms.Count)
            OnPlantSelected?.Invoke(_organisms[index]);
    }

    public OrganismEntity[] GetOrganisms() => _organisms.ToArray();
}
