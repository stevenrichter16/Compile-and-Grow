using System;
using System.Collections.Generic;
using CodeEditor.Completion;
using CodeEditor.Core;

/// <summary>
/// Provides parameter signature hints for Growl built-in functions and methods.
/// When the cursor is inside a function call like clamp(x, |), shows "clamp(val, lo, hi)"
/// with the active parameter bolded.
/// </summary>
public sealed class GrowlSignatureHintProvider : ISignatureHintProvider
{
    private static readonly Dictionary<string, SignatureHint> s_signatures = BuildSignatures();
    private static readonly Dictionary<string, Dictionary<string, SignatureHint>> s_methodSignatures = BuildMethodSignatures();

    public SignatureHint GetSignatureHint(DocumentModel doc, TextPosition cursor, out int activeParameter)
    {
        activeParameter = 0;
        string line = doc.GetLine(cursor.Line);
        int col = cursor.Column;

        // Walk backward from cursor to find the opening '(' that we're inside of.
        // Track nesting depth and count commas at our depth to determine activeParameter.
        int depth = 0;
        int commaCount = 0;
        int openParenCol = -1;

        for (int i = col - 1; i >= 0; i--)
        {
            char c = line[i];

            // Skip string literals
            if (c == '"' || c == '\'')
            {
                char quote = c;
                i--;
                while (i >= 0 && line[i] != quote)
                {
                    if (line[i] == '\\') i--; // skip escaped chars
                    i--;
                }
                continue;
            }

            if (c == ')')
            {
                depth++;
            }
            else if (c == '(')
            {
                if (depth == 0)
                {
                    openParenCol = i;
                    break;
                }
                depth--;
            }
            else if (c == ',' && depth == 0)
            {
                commaCount++;
            }
        }

        if (openParenCol < 0)
            return null;

        activeParameter = commaCount;

        // Extract the function name before the '('
        // Could be "funcName(" or "receiver.methodName("
        int nameEnd = openParenCol;
        int nameStart = nameEnd - 1;
        while (nameStart >= 0 && IsWordChar(line[nameStart]))
            nameStart--;
        nameStart++;

        if (nameStart >= nameEnd)
            return null;

        string funcName = line.Substring(nameStart, nameEnd - nameStart);

        // Check for dot-access: "receiver.method("
        int dotPos = nameStart - 1;
        if (dotPos >= 0 && line[dotPos] == '.')
        {
            int recEnd = dotPos;
            int recStart = recEnd - 1;
            while (recStart >= 0 && IsWordChar(line[recStart]))
                recStart--;
            recStart++;

            if (recStart < recEnd)
            {
                string receiver = line.Substring(recStart, recEnd - recStart);
                // Try to find method signature for this receiver
                if (s_methodSignatures.TryGetValue(receiver, out var methods)
                    && methods.TryGetValue(funcName, out var methodHint))
                    return methodHint;

                // Try generic "_any" methods
                if (s_methodSignatures.TryGetValue("_any", out var anyMethods)
                    && anyMethods.TryGetValue(funcName, out var anyHint))
                    return anyHint;
            }
        }

        // Look up as a global function
        if (s_signatures.TryGetValue(funcName, out var hint))
            return hint;

        return null;
    }

    private static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    // ── Signature definitions ───────────────────────────────────────────

    private static Dictionary<string, SignatureHint> BuildSignatures()
    {
        var d = new Dictionary<string, SignatureHint>(StringComparer.Ordinal);

        // Core
        d["print"] = Sig("print", P("value"));
        d["log"]   = Sig("log", P("value"));
        d["len"]   = Sig("len", P("collection"));
        d["type"]  = Sig("type", P("value"));
        d["warn"]  = Sig("warn", P("message"));
        d["error"] = Sig("error", P("message"));
        d["str"]   = Sig("str", P("value"));

        // Math
        d["min"]   = Sig("min", P("a"), P("b"));
        d["max"]   = Sig("max", P("a"), P("b"));
        d["abs"]   = Sig("abs", P("n"));
        d["round"] = Sig("round", P("n"), P("places", true));
        d["sqrt"]  = Sig("sqrt", P("n"));
        d["sin"]   = Sig("sin", P("radians"));
        d["cos"]   = Sig("cos", P("radians"));
        d["tan"]   = Sig("tan", P("radians"));
        d["clamp"] = Sig("clamp", P("value"), P("low"), P("high"));
        d["lerp"]  = Sig("lerp", P("a"), P("b"), P("t"));
        d["remap"] = Sig("remap", P("value"), P("inLow"), P("inHigh"), P("outLow"), P("outHigh"));
        d["floor"] = Sig("floor", P("n"));
        d["ceil"]  = Sig("ceil", P("n"));
        d["pow"]   = Sig("pow", P("base"), P("exponent"));

        // Random
        d["random"]        = Sig("random");
        d["random_int"]    = Sig("random_int", P("low"), P("high"));
        d["random_choice"] = Sig("random_choice", P("list"));
        d["noise"]         = Sig("noise", P("x"));
        d["chance"]        = Sig("chance", P("probability"));

        // Bio timing
        d["every"]       = Sig("every", P("n"));
        d["after"]       = Sig("after", P("n"));
        d["between"]     = Sig("between", P("low"), P("high"));
        d["season"]      = Sig("season");
        d["time_of_day"] = Sig("time_of_day");

        return d;
    }

    private static Dictionary<string, Dictionary<string, SignatureHint>> BuildMethodSignatures()
    {
        var d = new Dictionary<string, Dictionary<string, SignatureHint>>(StringComparer.Ordinal);

        // math namespace
        d["math"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["sin"]        = Sig("math.sin", P("radians")),
            ["cos"]        = Sig("math.cos", P("radians")),
            ["tan"]        = Sig("math.tan", P("radians")),
            ["asin"]       = Sig("math.asin", P("x")),
            ["acos"]       = Sig("math.acos", P("x")),
            ["atan2"]      = Sig("math.atan2", P("y"), P("x")),
            ["sqrt"]       = Sig("math.sqrt", P("n")),
            ["abs"]        = Sig("math.abs", P("n")),
            ["floor"]      = Sig("math.floor", P("n")),
            ["ceil"]       = Sig("math.ceil", P("n")),
            ["round"]      = Sig("math.round", P("n")),
            ["log"]        = Sig("math.log", P("n"), P("base", true)),
            ["log2"]       = Sig("math.log2", P("n")),
            ["log10"]      = Sig("math.log10", P("n")),
            ["pow"]        = Sig("math.pow", P("base"), P("exponent")),
            ["radians"]    = Sig("math.radians", P("degrees")),
            ["degrees"]    = Sig("math.degrees", P("radians")),
            ["sigmoid"]    = Sig("math.sigmoid", P("x")),
            ["smoothstep"] = Sig("math.smoothstep", P("edge0"), P("edge1"), P("x")),
            ["map_range"]  = Sig("math.map_range", P("value"), P("inLow"), P("inHigh"), P("outLow"), P("outHigh")),
        };

        d["morph"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["create_part"] = Sig("morph.create_part", P("name"), P("type"), P("size", true), P("energy_cost", true)),
            ["attach"] = Sig("morph.attach", P("part"), P("to_part"), P("position", true)),
        };

        d["root"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["absorb"] = Sig("root.absorb", P("resource")),
        };

        d["stem"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["store_water"] = Sig("stem.store_water", P("amount")),
            ["store_energy"] = Sig("stem.store_energy", P("amount")),
        };

        d["leaf"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["open_stomata"] = Sig("leaf.open_stomata", P("amount")),
            ["close_stomata"] = Sig("leaf.close_stomata"),
            ["track_light"] = Sig("leaf.track_light", P("enabled")),
        };

        d["photo"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["process"] = Sig("photo.process"),
            ["get_limiting_factor"] = Sig("photo.get_limiting_factor"),
        };

        // String methods
        var strMethods = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["split"]      = Sig("split", P("separator", true)),
            ["join"]       = Sig("join", P("list")),
            ["upper"]      = Sig("upper"),
            ["lower"]      = Sig("lower"),
            ["trim"]       = Sig("trim"),
            ["contains"]   = Sig("contains", P("substring")),
            ["startswith"] = Sig("startswith", P("prefix")),
            ["endswith"]   = Sig("endswith", P("suffix")),
            ["replace"]    = Sig("replace", P("old"), P("new")),
            ["format"]     = Sig("format", P("args...")),
            ["indexOf"]    = Sig("indexOf", P("substring")),
        };

        // List methods
        var listMethods = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["push"]      = Sig("push", P("item")),
            ["pop"]       = Sig("pop"),
            ["insert"]    = Sig("insert", P("index"), P("item")),
            ["remove"]    = Sig("remove", P("item")),
            ["contains"]  = Sig("contains", P("item")),
            ["sort"]      = Sig("sort", P("key", true)),
            ["reverse"]   = Sig("reverse"),
            ["map"]       = Sig("map", P("fn")),
            ["filter"]    = Sig("filter", P("fn")),
            ["reduce"]    = Sig("reduce", P("fn"), P("initial", true)),
            ["each"]      = Sig("each", P("fn")),
            ["any"]       = Sig("any", P("fn")),
            ["all"]       = Sig("all", P("fn")),
            ["find"]      = Sig("find", P("fn")),
            ["flatten"]   = Sig("flatten"),
            ["zip"]       = Sig("zip", P("other")),
            ["unique"]    = Sig("unique"),
            ["count"]     = Sig("count", P("fn", true)),
            ["min"]       = Sig("min"),
            ["max"]       = Sig("max"),
            ["sum"]       = Sig("sum"),
            ["avg"]       = Sig("avg"),
            ["sample"]    = Sig("sample", P("n", true)),
            ["shuffle"]   = Sig("shuffle"),
            ["enumerate"] = Sig("enumerate"),
            ["indexOf"]   = Sig("indexOf", P("item")),
        };

        // Dict methods
        var dictMethods = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["keys"]    = Sig("keys"),
            ["values"]  = Sig("values"),
            ["entries"] = Sig("entries"),
            ["has"]     = Sig("has", P("key")),
            ["remove"]  = Sig("remove", P("key")),
            ["merge"]   = Sig("merge", P("other")),
            ["get"]     = Sig("get", P("key"), P("default", true)),
        };

        // Set methods
        var setMethods = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["add"]      = Sig("add", P("item")),
            ["remove"]   = Sig("remove", P("item")),
            ["contains"] = Sig("contains", P("item")),
        };

        // Merge into _any for unknown receiver types
        var any = new Dictionary<string, SignatureHint>(StringComparer.Ordinal);
        foreach (var group in new[] { strMethods, listMethods, dictMethods, setMethods })
        {
            foreach (var kvp in group)
            {
                if (!any.ContainsKey(kvp.Key))
                    any[kvp.Key] = kvp.Value;
            }
        }
        d["_any"] = any;

        return d;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static SignatureParameter P(string name, bool optional = false)
    {
        return new SignatureParameter(name, optional);
    }

    private static SignatureHint Sig(string name, params SignatureParameter[] parameters)
    {
        return new SignatureHint(name, parameters);
    }
}
