using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GrowlLanguage.Runtime
{
    public sealed class GrowlTuple
    {
        public IReadOnlyList<object> Elements { get; }

        public GrowlTuple(IReadOnlyList<object> elements)
        {
            Elements = elements ?? new List<object>();
        }
    }

    public sealed class GrowlSet
    {
        internal List<object> MutableElements { get; }
        public IReadOnlyList<object> Elements => MutableElements;

        public GrowlSet(IReadOnlyList<object> elements)
        {
            if (elements is List<object> existing)
                MutableElements = existing;
            else
            {
                MutableElements = new List<object>(elements?.Count ?? 0);
                if (elements != null)
                    for (int i = 0; i < elements.Count; i++)
                        MutableElements.Add(elements[i]);
            }
        }
    }

    public sealed class GrowlRange
    {
        public long Start { get; }
        public long End { get; }
        public long Step { get; }
        public bool Inclusive { get; }

        public GrowlRange(long start, long end, bool inclusive, long step)
        {
            Start = start;
            End = end;
            Inclusive = inclusive;
            Step = step == 0 ? 1 : step;
        }

        public IEnumerable<object> Enumerate()
        {
            long cursor = Start;
            if (Step > 0)
            {
                while (Inclusive ? cursor <= End : cursor < End)
                {
                    yield return cursor;
                    cursor += Step;
                }
            }
            else
            {
                while (Inclusive ? cursor >= End : cursor > End)
                {
                    yield return cursor;
                    cursor += Step;
                }
            }
        }
    }

    public sealed class GrowlVector
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public GrowlVector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Magnitude => System.Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
    }

    public static class RuntimeValueFormatter
    {
        public static string Format(object value)
        {
            if (value == null)
                return "none";

            switch (value)
            {
                case string s:
                    return "\"" + s + "\"";
                case bool b:
                    return b ? "true" : "false";
                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);
                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);
                case decimal m:
                    return m.ToString(CultureInfo.InvariantCulture);
                case GrowlTuple tuple:
                    return "(" + JoinList(tuple.Elements) + ")";
                case GrowlSet set:
                    return "{" + JoinList(set.Elements) + "}";
                case GrowlRange range:
                    return range.Start + (range.Inclusive ? "..=" : "..") + range.End;
                case GrowlVector vector:
                    return "<" +
                           vector.X.ToString(CultureInfo.InvariantCulture) + ", " +
                           vector.Y.ToString(CultureInfo.InvariantCulture) + ", " +
                           vector.Z.ToString(CultureInfo.InvariantCulture) + ">";
            }

            if (value is IDictionary dictionary)
            {
                // Class instance: display as ClassName(field=val, ...)
                bool isClassInstance = false;
                string className = null;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string ek && ek == "__class")
                    {
                        isClassInstance = true;
                        break;
                    }
                    if (entry.Key is string tk && tk == "__type" && entry.Value is string tv && tv != "__super__")
                    {
                        className = tv;
                    }
                }

                if (isClassInstance && className != null)
                {
                    var sb = new StringBuilder();
                    sb.Append(className);
                    sb.Append("(");
                    bool first = true;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        string key = entry.Key as string;
                        if (key == "__type" || key == "__class" || key == "__mro")
                            continue;
                        if (!first)
                            sb.Append(", ");
                        first = false;
                        sb.Append(key);
                        sb.Append("=");
                        sb.Append(Format(entry.Value));
                    }
                    sb.Append(")");
                    return sb.ToString();
                }

                // Plain dict / struct
                {
                    var sb = new StringBuilder();
                    sb.Append("{");

                    bool first = true;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        if (!first)
                            sb.Append(", ");
                        first = false;
                        sb.Append(Format(entry.Key));
                        sb.Append(": ");
                        sb.Append(Format(entry.Value));
                    }

                    sb.Append("}");
                    return sb.ToString();
                }
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                var parts = new List<string>();
                foreach (object item in enumerable)
                    parts.Add(Format(item));
                return "[" + string.Join(", ", parts) + "]";
            }

            return value.ToString();
        }

        private static string JoinList(IReadOnlyList<object> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            var parts = new List<string>(values.Count);
            for (int i = 0; i < values.Count; i++)
                parts.Add(Format(values[i]));

            return string.Join(", ", parts);
        }
    }
}
