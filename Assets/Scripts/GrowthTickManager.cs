using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GrowthTickManager : MonoBehaviour
{
    [Serializable]
    public sealed class SignalRecord
    {
        public string type;
        public float intensity;
        public float radius;
        public string sender;
        public long tick;
        public Vector3 senderPosition;

        public Dictionary<object, object> ToRuntimeDictionary()
        {
            return new Dictionary<object, object>
            {
                ["type"] = type,
                ["intensity"] = intensity,
                ["radius"] = radius,
                ["sender"] = sender,
                ["tick"] = tick,
            };
        }
    }

    [Header("Tick")]
    [SerializeField] private bool autoTick = true;
    [SerializeField] private float secondsPerTick = 1f;
    [SerializeField] private long currentTick;

    [Header("World Counters")]
    [SerializeField] private long totalSpawnedSeeds;

    private float _tickAccumulator;
    private readonly List<SignalRecord> _signals = new List<SignalRecord>();
    private readonly Dictionary<string, object> _customWorldValues = new Dictionary<string, object>(StringComparer.Ordinal);

    public event Action<long> OnTickAdvanced;

    public long CurrentTick => currentTick;
    public long SignalCount => _signals.Count;
    public long TotalSpawnedSeeds => totalSpawnedSeeds;
    public IReadOnlyList<SignalRecord> SignalLog => _signals;

    public string LastSignalType
    {
        get
        {
            if (_signals.Count == 0)
                return string.Empty;
            return _signals[_signals.Count - 1].type ?? string.Empty;
        }
    }

    private void Update()
    {
        if (!autoTick || secondsPerTick <= 0f)
            return;

        _tickAccumulator += Time.deltaTime;
        while (_tickAccumulator >= secondsPerTick)
        {
            _tickAccumulator -= secondsPerTick;
            AdvanceTick(1);
        }
    }

    public void AdvanceTick(long count)
    {
        if (count <= 0)
            return;

        currentTick += count;
        OnTickAdvanced?.Invoke(currentTick);
    }

    public void SetTick(long tick)
    {
        currentTick = Math.Max(0L, tick);
    }

    public void AddSpawnedSeeds(long count)
    {
        if (count <= 0)
            return;

        totalSpawnedSeeds += count;
    }

    public Dictionary<object, object> EmitSignal(string signalType, double intensity, double radius, string sender, Vector3 senderPosition = default)
    {
        var record = new SignalRecord
        {
            type = string.IsNullOrEmpty(signalType) ? "unknown" : signalType,
            intensity = Mathf.Max(0f, (float)intensity),
            radius = Mathf.Max(0f, (float)radius),
            sender = string.IsNullOrEmpty(sender) ? "unnamed" : sender,
            tick = currentTick,
            senderPosition = senderPosition,
        };

        _signals.Add(record);
        return record.ToRuntimeDictionary();
    }

    public List<SignalRecord> GetSignalsFromTick(long tick)
    {
        var result = new List<SignalRecord>();
        for (int i = 0; i < _signals.Count; i++)
        {
            if (_signals[i].tick == tick)
                result.Add(_signals[i]);
        }
        return result;
    }

    public List<SignalRecord> GetSignalsInRange(Vector3 receiverPos, long tick, string typeFilter, float maxDistance)
    {
        var result = new List<SignalRecord>();
        for (int i = 0; i < _signals.Count; i++)
        {
            SignalRecord sig = _signals[i];
            if (sig.tick != tick)
                continue;
            if (typeFilter != null && !string.Equals(sig.type, typeFilter, System.StringComparison.Ordinal))
                continue;
            float dist = Vector3.Distance(receiverPos, sig.senderPosition);
            if (maxDistance > 0f && dist > maxDistance)
                continue;
            result.Add(sig);
        }
        return result;
    }

    public List<object> CreateSignalRuntimeList()
    {
        var list = new List<object>(_signals.Count);
        for (int i = 0; i < _signals.Count; i++)
            list.Add(_signals[i].ToRuntimeDictionary());
        return list;
    }

    public bool TryGetWorldValue(string key, out object value)
    {
        switch (NormalizeKey(key))
        {
            case "tick":
                value = currentTick;
                return true;
            case "signal_count":
                value = (long)_signals.Count;
                return true;
            case "last_signal":
                value = LastSignalType;
                return true;
            case "total_spawned_seeds":
                value = totalSpawnedSeeds;
                return true;
            case "signals":
                value = CreateSignalRuntimeList();
                return true;
        }

        return _customWorldValues.TryGetValue(key ?? string.Empty, out value);
    }

    public bool TrySetWorldValue(string key, object value, out string errorMessage)
    {
        switch (NormalizeKey(key))
        {
            case "tick":
                SetTick(ToInteger(value));
                errorMessage = null;
                return true;

            case "total_spawned_seeds":
                totalSpawnedSeeds = Math.Max(0L, ToInteger(value));
                errorMessage = null;
                return true;

            case "signal_count":
            case "last_signal":
            case "signals":
                errorMessage = "World key '" + key + "' is read-only.";
                return false;
        }

        _customWorldValues[key ?? string.Empty] = value;
        errorMessage = null;
        return true;
    }

    public bool TryAddWorldValue(string key, double delta, out object result, out string errorMessage)
    {
        switch (NormalizeKey(key))
        {
            case "tick":
                currentTick = Math.Max(0L, currentTick + (long)Math.Round(delta));
                result = currentTick;
                errorMessage = null;
                return true;

            case "total_spawned_seeds":
                totalSpawnedSeeds = Math.Max(0L, totalSpawnedSeeds + (long)Math.Round(delta));
                result = totalSpawnedSeeds;
                errorMessage = null;
                return true;

            case "signal_count":
            case "last_signal":
            case "signals":
                result = null;
                errorMessage = "World key '" + key + "' is read-only.";
                return false;
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
            ["tick"] = currentTick,
            ["signal_count"] = (long)_signals.Count,
            ["last_signal"] = LastSignalType,
            ["total_spawned_seeds"] = totalSpawnedSeeds,
            ["signals"] = CreateSignalRuntimeList(),
        };

        foreach (KeyValuePair<string, object> pair in _customWorldValues)
            snapshot[pair.Key] = pair.Value;

        return snapshot;
    }

    private static string NormalizeKey(string key)
    {
        return (key ?? string.Empty).Trim().ToLowerInvariant();
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

    private static long ToInteger(object value)
    {
        if (TryConvertToDouble(value, out double number))
            return (long)Math.Round(number);

        return 0L;
    }

    private static bool IsWhole(double value)
    {
        return Math.Abs(value - Math.Round(value)) < 0.0000001d;
    }
}
