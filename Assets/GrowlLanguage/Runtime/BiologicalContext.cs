using System.Collections.Generic;

namespace GrowlLanguage.Runtime
{
    /// <summary>
    /// Persistent state container for biological constructs across ticks.
    /// Stored per-organism (in OrganismEntity.Memory) and passed into RuntimeOptions
    /// so the Interpreter can implement edge detection, cycle tracking, and tick gating.
    /// </summary>
    public sealed class BiologicalContext
    {
        /// <summary>Current game tick. Set by GeneExecutionManager before each execution.</summary>
        public long CurrentTick;

        /// <summary>
        /// Edge detection for when/then blocks.
        /// Key = source location "line:col", Value = condition was true last tick.
        /// </summary>
        public readonly Dictionary<string, bool> WhenPreviousState =
            new Dictionary<string, bool>(System.StringComparer.Ordinal);

        /// <summary>
        /// Cycle start tracking per named cycle.
        /// Key = cycle name, Value = tick when cycle started.
        /// </summary>
        public readonly Dictionary<string, long> CycleStartTicks =
            new Dictionary<string, long>(System.StringComparer.Ordinal);

        /// <summary>
        /// Ticker interval tracking per named ticker.
        /// Key = ticker name, Value = last tick when ticker fired.
        /// </summary>
        public readonly Dictionary<string, long> TickerLastFired =
            new Dictionary<string, long>(System.StringComparer.Ordinal);

        /// <summary>
        /// Queued events for respond-to blocks.
        /// Key = event name, Value = list of event data payloads.
        /// Events are consumed (cleared) after dispatch each tick.
        /// </summary>
        public readonly Dictionary<string, List<object>> PendingEvents =
            new Dictionary<string, List<object>>(System.StringComparer.Ordinal);

        /// <summary>Queue an event for dispatch by respond-to blocks.</summary>
        public void QueueEvent(string eventName, object data)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            if (!PendingEvents.TryGetValue(eventName, out List<object> queue))
            {
                queue = new List<object>();
                PendingEvents[eventName] = queue;
            }

            queue.Add(data);
        }
    }
}
