using System;
using System.Collections.Generic;
using CodeEditor.Completion;
using CodeEditor.Core;
using CodeEditor.Editor;

/// <summary>
/// Provides auto-complete suggestions for the Growl language.
/// Covers keywords, built-in functions, constants, dot-access methods,
/// and user-defined symbols found in the current document.
/// </summary>
public sealed class GrowlCompletionProvider : ICompletionProvider
{
    // ── Static completion lists (built once) ────────────────────────────

    private static readonly List<CompletionItem> s_keywords = BuildKeywords();
    private static readonly List<CompletionItem> s_builtins = BuildBuiltins();
    private static readonly List<CompletionItem> s_constants = BuildConstants();
    private static readonly Dictionary<string, List<CompletionItem>> s_dotCompletions = BuildDotCompletions();

    // ── User symbol cache ───────────────────────────────────────────────

    private int _cachedVersion = -1;
    private List<CompletionItem> _cachedUserSymbols = new List<CompletionItem>();

    // ── ICompletionProvider ─────────────────────────────────────────────

    public CompletionResult GetCompletions(DocumentModel doc, TextPosition cursor)
    {
        string line = doc.GetLine(cursor.Line);
        int col = cursor.Column;

        // ── Dot-access completions ──────────────────────────────────
        // Check if cursor is after "receiver." or "receiver.partialMember"
        int memberEnd = col;
        int memberStart = col;
        while (memberStart > 0 && EditorController.IsWordChar(line[memberStart - 1]))
            memberStart--;

        int dotPos = memberStart - 1;
        if (dotPos >= 0 && line[dotPos] == '.')
        {
            int recEnd = dotPos;
            int recStart = recEnd;
            while (recStart > 0 && EditorController.IsWordChar(line[recStart - 1]))
                recStart--;

            if (recStart < recEnd)
            {
                string receiver = line.Substring(recStart, recEnd - recStart);
                string memberPrefix = line.Substring(memberStart, memberEnd - memberStart);
                var memberRange = new TextRange(
                    new TextPosition(cursor.Line, memberStart),
                    new TextPosition(cursor.Line, memberEnd));

                return FilterDotCompletions(receiver, memberPrefix, memberRange);
            }
        }

        // ── Free-standing identifier completions ────────────────────
        int prefixStart = col;
        while (prefixStart > 0 && EditorController.IsWordChar(line[prefixStart - 1]))
            prefixStart--;

        string prefix = line.Substring(prefixStart, col - prefixStart);
        var prefixRange = new TextRange(
            new TextPosition(cursor.Line, prefixStart),
            new TextPosition(cursor.Line, col));

        if (prefix.Length == 0)
            return CompletionResult.Empty;

        var items = new List<CompletionItem>();

        AddMatching(items, s_keywords, prefix);
        AddMatching(items, s_builtins, prefix);
        AddMatching(items, s_constants, prefix);
        AddMatching(items, GetUserSymbols(doc), prefix);

        // Sort: exact prefix match first, then by kind priority, then alphabetical
        items.Sort((a, b) =>
        {
            bool aExact = a.Label.StartsWith(prefix, StringComparison.Ordinal);
            bool bExact = b.Label.StartsWith(prefix, StringComparison.Ordinal);
            if (aExact != bExact) return aExact ? -1 : 1;

            int kindCmp = KindPriority(a.Kind).CompareTo(KindPriority(b.Kind));
            if (kindCmp != 0) return kindCmp;

            return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
        });

        return new CompletionResult(items, prefixRange);
    }

    // ── Dot completions ─────────────────────────────────────────────────

    private CompletionResult FilterDotCompletions(string receiver, string memberPrefix, TextRange memberRange)
    {
        // Try exact receiver name first (e.g. "math")
        List<CompletionItem> candidates = null;
        if (s_dotCompletions.TryGetValue(receiver, out var exact))
            candidates = exact;

        // If not a known name, offer all type-agnostic methods
        if (candidates == null && s_dotCompletions.TryGetValue("_any", out var any))
            candidates = any;

        if (candidates == null)
            return CompletionResult.Empty;

        var items = new List<CompletionItem>();
        if (memberPrefix.Length == 0)
        {
            items.AddRange(candidates);
        }
        else
        {
            AddMatching(items, candidates, memberPrefix);
        }

        items.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
        return new CompletionResult(items, memberRange);
    }

    // ── User symbol scanning ────────────────────────────────────────────

    private List<CompletionItem> GetUserSymbols(DocumentModel doc)
    {
        if (doc.Version == _cachedVersion)
            return _cachedUserSymbols;

        _cachedVersion = doc.Version;
        var symbols = new HashSet<string>();
        var result = new List<CompletionItem>();

        for (int i = 0; i < doc.LineCount; i++)
        {
            string raw = doc.GetLine(i);
            string trimmed = raw.TrimStart();

            // fn name(...)
            if (trimmed.StartsWith("fn ") && trimmed.Length > 3)
            {
                string name = ExtractIdentifier(trimmed, 3);
                if (name != null && symbols.Add(name))
                    result.Add(new CompletionItem(name, CompletionKind.Function, "user fn"));
            }

            // class/struct/enum/trait Name
            foreach (string kw in s_declKeywords)
            {
                if (trimmed.StartsWith(kw) && trimmed.Length > kw.Length)
                {
                    string name = ExtractIdentifier(trimmed, kw.Length);
                    if (name != null && symbols.Add(name))
                        result.Add(new CompletionItem(name, CompletionKind.Variable, "user " + kw.TrimEnd()));
                }
            }

            // name = ... (assignment)
            int eqIdx = raw.IndexOf(" = ", StringComparison.Ordinal);
            if (eqIdx > 0)
            {
                string candidate = raw.Substring(0, eqIdx).TrimStart();
                if (candidate.Length > 0 && IsValidIdentifier(candidate) && symbols.Add(candidate))
                    result.Add(new CompletionItem(candidate, CompletionKind.Variable, "variable"));
            }
        }

        _cachedUserSymbols = result;
        return result;
    }

    private static readonly string[] s_declKeywords = { "class ", "struct ", "enum ", "trait " };

    // ── Helpers ─────────────────────────────────────────────────────────

    private static void AddMatching(List<CompletionItem> dest, IReadOnlyList<CompletionItem> source, string prefix)
    {
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i].Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(source[i].Label, prefix, StringComparison.Ordinal))
            {
                dest.Add(source[i]);
            }
        }
    }

    private static int KindPriority(CompletionKind kind)
    {
        switch (kind)
        {
            case CompletionKind.Variable: return 0;
            case CompletionKind.Function: return 1;
            case CompletionKind.Constant: return 2;
            case CompletionKind.Keyword:  return 3;
            default: return 4;
        }
    }

    private static string ExtractIdentifier(string line, int startIndex)
    {
        int i = startIndex;
        while (i < line.Length && EditorController.IsWordChar(line[i]))
            i++;
        return i > startIndex ? line.Substring(startIndex, i - startIndex) : null;
    }

    private static bool IsValidIdentifier(string s)
    {
        if (s.Length == 0) return false;
        if (!char.IsLetter(s[0]) && s[0] != '_') return false;
        for (int i = 1; i < s.Length; i++)
        {
            if (!EditorController.IsWordChar(s[i])) return false;
        }
        return true;
    }

    // ── Static data builders ────────────────────────────────────────────

    private static List<CompletionItem> BuildKeywords()
    {
        string[] kws =
        {
            "fn", "class", "struct", "enum", "trait", "mixin", "abstract", "static",
            "const", "type", "module", "import", "from", "as",
            "if", "elif", "else", "for", "in", "while", "loop",
            "break", "continue", "return", "yield",
            "match", "case", "try", "recover", "always",
            "and", "or", "not", "is", "self", "super", "cls",
            "phase", "when", "then", "respond", "to", "adapt", "toward", "rate",
            "otherwise", "cycle", "at", "period", "ticker", "every",
            "wait", "defer", "until", "mutate", "by",
            "true", "false", "none",
        };
        var list = new List<CompletionItem>(kws.Length);
        for (int i = 0; i < kws.Length; i++)
            list.Add(new CompletionItem(kws[i], CompletionKind.Keyword, "keyword"));
        return list;
    }

    private static List<CompletionItem> BuildBuiltins()
    {
        var list = new List<CompletionItem>
        {
            // Core
            I("print",   CompletionKind.Function, "print(value)"),
            I("log",     CompletionKind.Function, "log(value)"),
            I("len",     CompletionKind.Function, "len(collection)"),
            I("type",    CompletionKind.Function, "type(value)"),
            I("warn",    CompletionKind.Function, "warn(message)"),
            I("error",   CompletionKind.Function, "error(message)"),
            I("str",     CompletionKind.Function, "str(value)"),

            // Math (global shortcuts)
            I("min",    CompletionKind.Function, "min(a, b)"),
            I("max",    CompletionKind.Function, "max(a, b)"),
            I("abs",    CompletionKind.Function, "abs(n)"),
            I("round",  CompletionKind.Function, "round(n, places?)"),
            I("sqrt",   CompletionKind.Function, "sqrt(n)"),
            I("sin",    CompletionKind.Function, "sin(radians)"),
            I("cos",    CompletionKind.Function, "cos(radians)"),
            I("tan",    CompletionKind.Function, "tan(radians)"),
            I("clamp",  CompletionKind.Function, "clamp(val, lo, hi)"),
            I("lerp",   CompletionKind.Function, "lerp(a, b, t)"),
            I("remap",  CompletionKind.Function, "remap(v, iLo, iHi, oLo, oHi)"),
            I("floor",  CompletionKind.Function, "floor(n)"),
            I("ceil",   CompletionKind.Function, "ceil(n)"),
            I("pow",    CompletionKind.Function, "pow(base, exp)"),

            // Random
            I("random",        CompletionKind.Function, "random()"),
            I("random_int",    CompletionKind.Function, "random_int(lo, hi)"),
            I("random_choice", CompletionKind.Function, "random_choice(list)"),
            I("noise",         CompletionKind.Function, "noise(x)"),
            I("chance",        CompletionKind.Function, "chance(probability)"),

            // Bio timing
            I("every",      CompletionKind.Function, "every(n)"),
            I("after",      CompletionKind.Function, "after(n)"),
            I("between",    CompletionKind.Function, "between(lo, hi)"),
            I("season",     CompletionKind.Function, "season()"),
            I("time_of_day", CompletionKind.Function, "time_of_day()"),

            // Namespace
            I("math", CompletionKind.Variable, "math namespace"),
            I("morph", CompletionKind.Variable, "morph module"),
            I("root", CompletionKind.Variable, "root module"),
            I("stem", CompletionKind.Variable, "stem module"),
            I("leaf", CompletionKind.Variable, "leaf module"),
            I("photo", CompletionKind.Variable, "photo module"),
        };
        return list;
    }

    private static List<CompletionItem> BuildConstants()
    {
        return new List<CompletionItem>
        {
            I("UP",    CompletionKind.Constant, "vec(0,1,0)"),
            I("DOWN",  CompletionKind.Constant, "vec(0,-1,0)"),
            I("LEFT",  CompletionKind.Constant, "vec(-1,0,0)"),
            I("RIGHT", CompletionKind.Constant, "vec(1,0,0)"),
            I("NORTH", CompletionKind.Constant, "vec(0,1,0)"),
            I("SOUTH", CompletionKind.Constant, "vec(0,-1,0)"),
            I("EAST",  CompletionKind.Constant, "vec(1,0,0)"),
            I("WEST",  CompletionKind.Constant, "vec(-1,0,0)"),
            I("NONE",  CompletionKind.Constant, "null"),
            I("TICK",  CompletionKind.Constant, "current tick"),
            I("SELF",  CompletionKind.Constant, "current entity"),
        };
    }

    private static Dictionary<string, List<CompletionItem>> BuildDotCompletions()
    {
        var dict = new Dictionary<string, List<CompletionItem>>(StringComparer.Ordinal);

        // math namespace
        dict["math"] = new List<CompletionItem>
        {
            I("PI",         CompletionKind.Property, "3.14159..."),
            I("E",          CompletionKind.Property, "2.71828..."),
            I("TAU",        CompletionKind.Property, "6.28318..."),
            I("INF",        CompletionKind.Property, "infinity"),
            I("sin",        CompletionKind.Method,   "sin(rad)"),
            I("cos",        CompletionKind.Method,   "cos(rad)"),
            I("tan",        CompletionKind.Method,   "tan(rad)"),
            I("asin",       CompletionKind.Method,   "asin(x)"),
            I("acos",       CompletionKind.Method,   "acos(x)"),
            I("atan2",      CompletionKind.Method,   "atan2(y, x)"),
            I("sqrt",       CompletionKind.Method,   "sqrt(n)"),
            I("abs",        CompletionKind.Method,   "abs(n)"),
            I("floor",      CompletionKind.Method,   "floor(n)"),
            I("ceil",       CompletionKind.Method,   "ceil(n)"),
            I("round",      CompletionKind.Method,   "round(n)"),
            I("log",        CompletionKind.Method,   "log(n, base?)"),
            I("log2",       CompletionKind.Method,   "log2(n)"),
            I("log10",      CompletionKind.Method,   "log10(n)"),
            I("pow",        CompletionKind.Method,   "pow(b, e)"),
            I("radians",    CompletionKind.Method,   "radians(deg)"),
            I("degrees",    CompletionKind.Method,   "degrees(rad)"),
            I("sigmoid",    CompletionKind.Method,   "sigmoid(x)"),
            I("smoothstep", CompletionKind.Method,   "smoothstep(e0, e1, x)"),
            I("map_range",  CompletionKind.Method,   "map_range(v, iLo, iHi, oLo, oHi)"),
        };

        dict["morph"] = new List<CompletionItem>
        {
            I("create_part", CompletionKind.Method, "create_part(name, type, size?, energy_cost?)"),
            I("attach", CompletionKind.Method, "attach(part, to_part, position?)"),
        };

        dict["root"] = new List<CompletionItem>
        {
            I("absorb", CompletionKind.Method, "absorb(resource)"),
        };

        dict["stem"] = new List<CompletionItem>
        {
            I("store_water", CompletionKind.Method, "store_water(amount)"),
            I("store_energy", CompletionKind.Method, "store_energy(amount)"),
            I("store_glucose", CompletionKind.Method, "store_glucose(amount)"),
        };

        dict["leaf"] = new List<CompletionItem>
        {
            I("open_stomata", CompletionKind.Method, "open_stomata(amount)"),
            I("close_stomata", CompletionKind.Method, "close_stomata()"),
            I("track_light", CompletionKind.Method, "track_light(enabled)"),
        };

        dict["photo"] = new List<CompletionItem>
        {
            I("process", CompletionKind.Method, "process()"),
            I("get_limiting_factor", CompletionKind.Method, "get_limiting_factor()"),
            I("set_glucose_storage_bias", CompletionKind.Method, "set_glucose_storage_bias(value)"),
            I("set_energy_storage_bias", CompletionKind.Method, "set_energy_storage_bias(value)"),
        };

        // String methods
        var stringMethods = new List<CompletionItem>
        {
            I("split",      CompletionKind.Method, "split(sep?)"),
            I("join",       CompletionKind.Method, "join(list)"),
            I("upper",      CompletionKind.Method, "upper()"),
            I("lower",      CompletionKind.Method, "lower()"),
            I("trim",       CompletionKind.Method, "trim()"),
            I("contains",   CompletionKind.Method, "contains(sub)"),
            I("startswith", CompletionKind.Method, "startswith(prefix)"),
            I("endswith",   CompletionKind.Method, "endswith(suffix)"),
            I("replace",    CompletionKind.Method, "replace(old, new)"),
            I("format",     CompletionKind.Method, "format(args...)"),
            I("indexOf",    CompletionKind.Method, "indexOf(sub)"),
        };

        // List methods
        var listMethods = new List<CompletionItem>
        {
            I("push",      CompletionKind.Method, "push(item)"),
            I("pop",       CompletionKind.Method, "pop()"),
            I("insert",    CompletionKind.Method, "insert(idx, item)"),
            I("remove",    CompletionKind.Method, "remove(item)"),
            I("contains",  CompletionKind.Method, "contains(item)"),
            I("sort",      CompletionKind.Method, "sort(key?)"),
            I("reverse",   CompletionKind.Method, "reverse()"),
            I("map",       CompletionKind.Method, "map(fn)"),
            I("filter",    CompletionKind.Method, "filter(fn)"),
            I("reduce",    CompletionKind.Method, "reduce(fn, init?)"),
            I("each",      CompletionKind.Method, "each(fn)"),
            I("any",       CompletionKind.Method, "any(fn)"),
            I("all",       CompletionKind.Method, "all(fn)"),
            I("find",      CompletionKind.Method, "find(fn)"),
            I("flatten",   CompletionKind.Method, "flatten()"),
            I("zip",       CompletionKind.Method, "zip(other)"),
            I("unique",    CompletionKind.Method, "unique()"),
            I("count",     CompletionKind.Method, "count(fn?)"),
            I("min",       CompletionKind.Method, "min()"),
            I("max",       CompletionKind.Method, "max()"),
            I("sum",       CompletionKind.Method, "sum()"),
            I("avg",       CompletionKind.Method, "avg()"),
            I("sample",    CompletionKind.Method, "sample(n?)"),
            I("shuffle",   CompletionKind.Method, "shuffle()"),
            I("enumerate", CompletionKind.Method, "enumerate()"),
            I("indexOf",   CompletionKind.Method, "indexOf(item)"),
        };

        // Dict methods
        var dictMethods = new List<CompletionItem>
        {
            I("keys",    CompletionKind.Method, "keys()"),
            I("values",  CompletionKind.Method, "values()"),
            I("entries", CompletionKind.Method, "entries()"),
            I("has",     CompletionKind.Method, "has(key)"),
            I("remove",  CompletionKind.Method, "remove(key)"),
            I("merge",   CompletionKind.Method, "merge(other)"),
            I("get",     CompletionKind.Method, "get(key, default?)"),
        };

        // Set methods
        var setMethods = new List<CompletionItem>
        {
            I("add",      CompletionKind.Method, "add(item)"),
            I("remove",   CompletionKind.Method, "remove(item)"),
            I("contains", CompletionKind.Method, "contains(item)"),
        };

        // Merge all collection/string methods into a generic "_any" list
        // so we can offer them when we don't know the receiver type
        var anyMethods = new HashSet<string>();
        var anyList = new List<CompletionItem>();
        foreach (var group in new[] { stringMethods, listMethods, dictMethods, setMethods })
        {
            foreach (var item in group)
            {
                if (anyMethods.Add(item.Label))
                    anyList.Add(item);
            }
        }

        dict["_any"] = anyList;
        return dict;
    }

    private static CompletionItem I(string label, CompletionKind kind, string detail)
    {
        return new CompletionItem(label, kind, detail);
    }
}
