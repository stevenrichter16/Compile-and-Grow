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

        // Bio globals
        d["synthesize"]      = Sig("synthesize", P("base"), P("density", true), P("water_content", true), P("growth_rate", true));
        d["produce"]         = Sig("produce", P("product"), P("location", true), P("rate", true));
        d["emit"]            = Sig("emit", P("product"), P("rate", true));
        d["emit_signal"]     = Sig("emit_signal", P("type"), P("intensity", true), P("radius", true));
        d["org_get"]         = Sig("org_get", P("key"), P("default", true));
        d["org_set"]         = Sig("org_set", P("key"), P("value"));
        d["org_add"]         = Sig("org_add", P("key"), P("delta"));
        d["org_damage"]      = Sig("org_damage", P("amount"));
        d["org_heal"]        = Sig("org_heal", P("amount"));
        d["org_memory_get"]  = Sig("org_memory_get", P("key"), P("default", true));
        d["org_memory_set"]  = Sig("org_memory_set", P("key"), P("value"));
        d["world_get"]       = Sig("world_get", P("key"), P("default", true));
        d["world_set"]       = Sig("world_set", P("key"), P("value"));
        d["world_add"]       = Sig("world_add", P("key"), P("delta"));
        d["spawn_seed"]      = Sig("spawn_seed", P("count", true));
        d["spawn"]           = Sig("spawn", P("class_name"), P("source"), P("position", true));
        d["parts_find"]      = Sig("parts_find", P("name"));
        d["parts_find_type"] = Sig("parts_find_type", P("type"));
        d["parts_count"]     = Sig("parts_count", P("type"));

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

        // ── Biological modules ────────────────────────────────────────

        d["root"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["grow_down"]          = Sig("root.grow_down", P("distance", true)),
            ["grow_up"]            = Sig("root.grow_up", P("distance", true)),
            ["grow_wide"]          = Sig("root.grow_wide", P("distance", true)),
            ["grow_toward"]        = Sig("root.grow_toward", P("direction"), P("distance", true)),
            ["branch"]             = Sig("root.branch", P("count", true), P("from_part", true)),
            ["thicken"]            = Sig("root.thicken", P("part_name"), P("amount")),
            ["absorb"]             = Sig("root.absorb", P("resource")),
            ["absorb_all"]         = Sig("root.absorb_all"),
            ["absorb_filtered"]    = Sig("root.absorb_filtered", P("resources")),
            ["set_absorption_rate"] = Sig("root.set_absorption_rate", P("resource"), P("rate")),
            ["deposit"]            = Sig("root.deposit", P("resource"), P("amount")),
            ["exude"]              = Sig("root.exude", P("chemical"), P("amount")),
            ["anchor"]             = Sig("root.anchor", P("strength")),
            ["connect_fungi"]      = Sig("root.connect_fungi", P("network", true)),
            ["sense_depth"]        = Sig("root.sense_depth"),
            ["sense_moisture"]     = Sig("root.sense_moisture", P("direction", true)),
            ["sense_obstacle"]     = Sig("root.sense_obstacle", P("direction", true)),
            ["sense_neighbors"]    = Sig("root.sense_neighbors"),
        };

        d["stem"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["grow_up"]        = Sig("stem.grow_up", P("distance", true)),
            ["grow_horizontal"] = Sig("stem.grow_horizontal", P("distance"), P("direction", true)),
            ["grow_thick"]     = Sig("stem.grow_thick", P("amount")),
            ["branch"]         = Sig("stem.branch", P("count", true), P("height", true), P("angle", true)),
            ["grow_segment"]   = Sig("stem.grow_segment", P("length"), P("angle"), P("from_part", true)),
            ["split"]          = Sig("stem.split", P("count", true)),
            ["set_rigidity"]   = Sig("stem.set_rigidity", P("value")),
            ["set_material"]   = Sig("stem.set_material", P("type", true)),
            ["store_water"]    = Sig("stem.store_water", P("amount")),
            ["store_energy"]   = Sig("stem.store_energy", P("amount")),
            ["attach_to"]      = Sig("stem.attach_to", P("target", true)),
            ["support_weight"] = Sig("stem.support_weight", P("part_name")),
            ["shed"]           = Sig("stem.shed", P("part_name")),
            ["heal"]           = Sig("stem.heal", P("part_name"), P("rate")),
            ["set_color"]      = Sig("stem.set_color", P("r"), P("g"), P("b")),
            ["set_texture"]    = Sig("stem.set_texture", P("type", true)),
            ["produce_bark"]   = Sig("stem.produce_bark", P("thickness")),
            ["produce_wax"]    = Sig("stem.produce_wax", P("thickness")),
        };

        d["leaf"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["grow"]              = Sig("leaf.grow", P("area"), P("from_part", true)),
            ["grow_count"]        = Sig("leaf.grow_count", P("number"), P("size_each"), P("from_part", true)),
            ["reshape"]           = Sig("leaf.reshape", P("part_name"), P("shape", true)),
            ["orient"]            = Sig("leaf.orient", P("direction", true)),
            ["track_light"]       = Sig("leaf.track_light", P("enabled")),
            ["set_angle_range"]   = Sig("leaf.set_angle_range", P("min"), P("max")),
            ["open_stomata"]      = Sig("leaf.open_stomata", P("amount")),
            ["close_stomata"]     = Sig("leaf.close_stomata"),
            ["set_stomata_schedule"] = Sig("leaf.set_stomata_schedule", P("schedule")),
            ["filter_gas"]        = Sig("leaf.filter_gas", P("gas"), P("action", true)),
            ["set_color"]         = Sig("leaf.set_color", P("r"), P("g"), P("b")),
            ["set_coating"]       = Sig("leaf.set_coating", P("type", true)),
            ["set_lifespan"]      = Sig("leaf.set_lifespan", P("ticks")),
            ["shed"]              = Sig("leaf.shed", P("part_name", true)),
            ["regrow"]            = Sig("leaf.regrow", P("part_name")),
            ["absorb_moisture"]   = Sig("leaf.absorb_moisture"),
            ["absorb_nutrients"]  = Sig("leaf.absorb_nutrients", P("resource")),
            ["absorb_chemical"]   = Sig("leaf.absorb_chemical", P("chemical")),
        };

        d["photo"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["absorb_light"]       = Sig("photo.absorb_light", P("efficiency", true)),
            ["set_pigment"]        = Sig("photo.set_pigment", P("type", true)),
            ["boost_chlorophyll"]  = Sig("photo.boost_chlorophyll", P("factor", true)),
            ["set_light_saturation"] = Sig("photo.set_light_saturation", P("threshold")),
            ["chemosynthesis"]     = Sig("photo.chemosynthesis", P("source")),
            ["thermosynthesis"]    = Sig("photo.thermosynthesis", P("source")),
            ["radiosynthesis"]     = Sig("photo.radiosynthesis"),
            ["parasitic"]          = Sig("photo.parasitic", P("target", true)),
            ["decompose"]          = Sig("photo.decompose"),
            ["set_metabolism"]     = Sig("photo.set_metabolism", P("rate", true)),
            ["store_energy"]       = Sig("photo.store_energy", P("amount"), P("location", true)),
            ["retrieve_energy"]    = Sig("photo.retrieve_energy", P("amount"), P("location", true)),
            ["share_energy"]       = Sig("photo.share_energy", P("target"), P("amount")),
        };

        d["morph"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["create_part"]       = Sig("morph.create_part", P("name"), P("type"), P("size", true), P("energy_cost", true)),
            ["remove_part"]       = Sig("morph.remove_part", P("name")),
            ["attach"]            = Sig("morph.attach", P("part"), P("to_part"), P("position", true)),
            ["grow_part"]         = Sig("morph.grow_part", P("part_name"), P("property"), P("amount")),
            ["shrink_part"]       = Sig("morph.shrink_part", P("part_name"), P("property"), P("amount")),
            ["set_symmetry"]      = Sig("morph.set_symmetry", P("type")),
            ["set_growth_pattern"] = Sig("morph.set_growth_pattern", P("type")),
            ["set_surface"]       = Sig("morph.set_surface", P("part_name"), P("properties", true)),
            ["emit_light"]        = Sig("morph.emit_light", P("intensity"), P("r", true), P("g", true), P("b", true), P("part", true)),
            ["orient_toward"]     = Sig("morph.orient_toward", P("direction")),
            ["contract"]          = Sig("morph.contract", P("part_name"), P("amount")),
            ["expand"]            = Sig("morph.expand", P("part_name"), P("amount")),
            ["pulse"]             = Sig("morph.pulse", P("part_name"), P("frequency"), P("amplitude")),
        };

        d["defense"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["grow_thorns"]          = Sig("defense.grow_thorns", P("part", true), P("sharpness"), P("density")),
            ["grow_armor"]           = Sig("defense.grow_armor", P("part_name"), P("thickness")),
            ["grow_camouflage"]      = Sig("defense.grow_camouflage", P("environment", true)),
            ["produce_toxin"]        = Sig("defense.produce_toxin", P("type"), P("potency", true), P("location", true)),
            ["produce_repellent"]    = Sig("defense.produce_repellent", P("type"), P("radius", true)),
            ["produce_attractant"]   = Sig("defense.produce_attractant", P("type"), P("target"), P("radius", true)),
            ["sticky_trap"]          = Sig("defense.sticky_trap", P("part_name"), P("strength", true)),
            ["resist_disease"]       = Sig("defense.resist_disease", P("type"), P("strength", true)),
            ["quarantine_part"]      = Sig("defense.quarantine_part", P("part_name")),
            ["fever"]                = Sig("defense.fever", P("amount")),
            ["on_damage"]            = Sig("defense.on_damage", P("callback")),
            ["on_neighbor_distress"] = Sig("defense.on_neighbor_distress", P("callback")),
        };

        d["reproduce"] = new Dictionary<string, SignatureHint>(StringComparer.Ordinal)
        {
            ["generate_seeds"]  = Sig("reproduce.generate_seeds", P("count", true), P("energy_per_seed", true)),
            ["set_dispersal"]   = Sig("reproduce.set_dispersal", P("method"), P("params", true)),
            ["set_germination"] = Sig("reproduce.set_germination", P("conditions")),
            ["mutate"]          = Sig("reproduce.mutate", P("variance")),
            ["mutate_gene"]     = Sig("reproduce.mutate_gene", P("slot_name"), P("variance", true)),
            ["crossbreed"]      = Sig("reproduce.crossbreed", P("other_org")),
            ["clone"]           = Sig("reproduce.clone", P("direction", true)),
            ["fragment"]        = Sig("reproduce.fragment", P("part_name")),
            ["set_lifecycle"]   = Sig("reproduce.set_lifecycle", P("type")),
            ["set_maturity_age"] = Sig("reproduce.set_maturity_age", P("ticks")),
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
