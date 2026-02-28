using System;
using System.Collections.Generic;
using NUnit.Framework;
using GrowlLanguage.Runtime;

namespace GrowlLanguage.Tests.Runtime
{
    [TestFixture]
    public class RuntimeHostIntegrationTests
    {
        [Test]
        public void HostGlobals_WorldOrgSeed_AreVisibleAndMutable()
        {
            var host = new TestRuntimeHost();

            string src =
                "world[\"power\"] = world[\"power\"] + 5\n" +
                "org.health = org.health - 0.2\n" +
                "seed[\"count\"] = seed[\"count\"] + 2\n" +
                "world[\"power\"]\n";

            RuntimeResult result = GrowlRuntime.Execute(src, new RuntimeOptions
            {
                Host = host,
            });

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo(105L));
            Assert.That(host.World["power"], Is.EqualTo(105L));
            Assert.That(Convert.ToDouble(host.Org["health"]), Is.EqualTo(0.8d).Within(0.0001d));
            Assert.That(host.Seed["count"], Is.EqualTo(3L));
        }

        [Test]
        public void HostBuiltins_MutateStateAndReturnValues()
        {
            var host = new TestRuntimeHost();

            string src =
                "world_set(\"temperature\", 30)\n" +
                "org_damage(0.25)\n" +
                "org_memory_set(\"mode\", \"panic\")\n" +
                "seed_add(\"count\", 4)\n" +
                "emit_signal(\"distress\", intensity: 0.8, radius: 3)\n" +
                "world_get(\"signal_count\")\n";

            RuntimeResult result = GrowlRuntime.Execute(src, new RuntimeOptions
            {
                Host = host,
            });

            Assert.That(result.Success, Is.True);
            Assert.That(result.LastValue, Is.EqualTo(1L));
            Assert.That(host.World["temperature"], Is.EqualTo(30L));
            Assert.That(Convert.ToDouble(host.Org["health"]), Is.EqualTo(0.75d).Within(0.0001d));
            Assert.That(host.OrgMemory["mode"], Is.EqualTo("panic"));
            Assert.That(host.Seed["count"], Is.EqualTo(5L));
        }

        [Test]
        public void HostBuiltinWithoutHost_ReportsRuntimeError()
        {
            string src = "world_set(\"power\", 50)\n";
            RuntimeResult result = GrowlRuntime.Execute(src);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Messages.Count, Is.GreaterThan(0));
            Assert.That(result.Messages[0].Kind, Is.EqualTo(RuntimeMessageKind.RuntimeError));
            Assert.That(result.Messages[0].Message, Does.Contain("requires a runtime host bridge"));
        }

        private sealed class TestRuntimeHost : IGrowlRuntimeHost
        {
            public Dictionary<string, object> World { get; } = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["power"] = 100L,
                ["temperature"] = 22L,
                ["signal_count"] = 0L,
            };

            public Dictionary<string, object> Org { get; } = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["health"] = 1.0d,
                ["alive"] = true,
            };

            public Dictionary<string, object> Seed { get; } = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["count"] = 1L,
                ["viability"] = 1.0d,
            };

            public Dictionary<string, object> OrgMemory { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

            public void PopulateGlobals(IDictionary<string, object> globals)
            {
                globals["world"] = World;
                globals["org"] = Org;
                globals["seed"] = Seed;
            }

            public bool TryInvokeBuiltin(
                string builtinName,
                IReadOnlyList<RuntimeCallArgument> args,
                out object result,
                out string errorMessage)
            {
                result = null;
                errorMessage = null;

                switch (builtinName)
                {
                    case "world_get":
                        return TryGet(World, args, out result, out errorMessage);
                    case "world_set":
                        return TrySet(World, args, out result, out errorMessage);
                    case "world_add":
                        return TryAdd(World, args, out result, out errorMessage);
                    case "org_get":
                        return TryGet(Org, args, out result, out errorMessage);
                    case "org_set":
                        return TrySet(Org, args, out result, out errorMessage);
                    case "org_add":
                        return TryAdd(Org, args, out result, out errorMessage);
                    case "org_damage":
                        {
                            double amount = ReadNumber(args, 0, "amount", 0d);
                            double health = ToNumber(Org["health"]);
                            health = Math.Max(0d, health - amount);
                            Org["health"] = health;
                            Org["alive"] = health > 0d;
                            result = health;
                            return true;
                        }
                    case "org_heal":
                        {
                            double amount = ReadNumber(args, 0, "amount", 0d);
                            double health = ToNumber(Org["health"]);
                            health = Math.Min(1d, health + amount);
                            Org["health"] = health;
                            Org["alive"] = health > 0d;
                            result = health;
                            return true;
                        }
                    case "org_memory_get":
                        return TryGet(OrgMemory, args, out result, out errorMessage);
                    case "org_memory_set":
                        return TrySet(OrgMemory, args, out result, out errorMessage);
                    case "seed_get":
                        return TryGet(Seed, args, out result, out errorMessage);
                    case "seed_set":
                        return TrySet(Seed, args, out result, out errorMessage);
                    case "seed_add":
                        return TryAdd(Seed, args, out result, out errorMessage);
                    case "emit_signal":
                        {
                            long count = Convert.ToInt64(World["signal_count"]);
                            count++;
                            World["signal_count"] = count;
                            result = count;
                            return true;
                        }
                    case "spawn_seed":
                        {
                            long count = ReadInteger(args, 0, "count", 1L);
                            long current = Convert.ToInt64(Seed["count"]);
                            long next = current + count;
                            Seed["count"] = next;
                            result = next;
                            return true;
                        }
                }

                errorMessage = "Unknown builtin.";
                return false;
            }

            private static bool TryGet(
                Dictionary<string, object> map,
                IReadOnlyList<RuntimeCallArgument> args,
                out object result,
                out string errorMessage)
            {
                string key = ReadString(args, 0, "key", null);
                if (key == null)
                {
                    result = null;
                    errorMessage = "Missing key.";
                    return false;
                }

                if (map.TryGetValue(key, out result))
                {
                    errorMessage = null;
                    return true;
                }

                if (TryGetArg(args, 1, "default", out RuntimeCallArgument fallback))
                {
                    result = fallback.Value;
                    errorMessage = null;
                    return true;
                }

                result = null;
                errorMessage = null;
                return true;
            }

            private static bool TrySet(
                Dictionary<string, object> map,
                IReadOnlyList<RuntimeCallArgument> args,
                out object result,
                out string errorMessage)
            {
                string key = ReadString(args, 0, "key", null);
                if (key == null || !TryGetArg(args, 1, "value", out RuntimeCallArgument valueArg))
                {
                    result = null;
                    errorMessage = "Missing key/value.";
                    return false;
                }

                map[key] = valueArg.Value;
                result = valueArg.Value;
                errorMessage = null;
                return true;
            }

            private static bool TryAdd(
                Dictionary<string, object> map,
                IReadOnlyList<RuntimeCallArgument> args,
                out object result,
                out string errorMessage)
            {
                string key = ReadString(args, 0, "key", null);
                if (key == null)
                {
                    result = null;
                    errorMessage = "Missing key.";
                    return false;
                }

                double delta = ReadNumber(args, 1, "delta", 0d);
                double current = map.TryGetValue(key, out object v) ? ToNumber(v) : 0d;
                double next = current + delta;
                map[key] = IsWhole(next) ? (object)(long)Math.Round(next) : next;
                result = map[key];
                errorMessage = null;
                return true;
            }

            private static bool TryGetArg(
                IReadOnlyList<RuntimeCallArgument> args,
                int index,
                string name,
                out RuntimeCallArgument argument)
            {
                for (int i = 0; i < args.Count; i++)
                {
                    if (!string.IsNullOrEmpty(args[i].Name) && args[i].Name == name)
                    {
                        argument = args[i];
                        return true;
                    }
                }

                int positional = 0;
                for (int i = 0; i < args.Count; i++)
                {
                    if (!string.IsNullOrEmpty(args[i].Name))
                        continue;

                    if (positional == index)
                    {
                        argument = args[i];
                        return true;
                    }

                    positional++;
                }

                argument = default;
                return false;
            }

            private static string ReadString(IReadOnlyList<RuntimeCallArgument> args, int index, string name, string fallback)
            {
                if (!TryGetArg(args, index, name, out RuntimeCallArgument arg) || arg.Value == null)
                    return fallback;
                return arg.Value.ToString();
            }

            private static double ReadNumber(IReadOnlyList<RuntimeCallArgument> args, int index, string name, double fallback)
            {
                if (!TryGetArg(args, index, name, out RuntimeCallArgument arg) || arg.Value == null)
                    return fallback;

                return ToNumber(arg.Value);
            }

            private static long ReadInteger(IReadOnlyList<RuntimeCallArgument> args, int index, string name, long fallback)
            {
                if (!TryGetArg(args, index, name, out RuntimeCallArgument arg) || arg.Value == null)
                    return fallback;

                return (long)Math.Round(ToNumber(arg.Value));
            }

            private static double ToNumber(object value)
            {
                switch (value)
                {
                    case sbyte v: return v;
                    case byte v: return v;
                    case short v: return v;
                    case ushort v: return v;
                    case int v: return v;
                    case uint v: return v;
                    case long v: return v;
                    case ulong v: return v;
                    case float v: return v;
                    case double v: return v;
                    case decimal v: return (double)v;
                    default: return 0d;
                }
            }

            private static bool IsWhole(double value)
            {
                return Math.Abs(value - Math.Round(value)) < 0.0000001d;
            }
        }
    }
}
