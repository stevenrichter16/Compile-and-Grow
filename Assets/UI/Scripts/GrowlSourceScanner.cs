using System.Collections.Generic;
using System.Text.RegularExpressions;

public readonly struct GeneSlotInfo
{
    public readonly string Type;  // "role" or "gene"
    public readonly string Name;  // e.g. "intake", "my_gene"
    public readonly string FunctionName;

    public GeneSlotInfo(string type, string name, string functionName)
    {
        Type = type;
        Name = name;
        FunctionName = functionName;
    }
}

public static class GrowlSourceScanner
{
    static readonly Regex RolePattern = new Regex(
        @"@role\(\s*""(\w+)""\s*\)", RegexOptions.Compiled);
    static readonly Regex GenePattern = new Regex(
        @"@gene\(\s*""(\w+)""\s*\)", RegexOptions.Compiled);
    static readonly Regex FnPattern = new Regex(
        @"fn\s+(\w+)\s*\(", RegexOptions.Compiled);

    static readonly string[] RequiredRoles = { "intake", "structure", "energy", "output" };

    public static List<GeneSlotInfo> Scan(string source)
    {
        var slots = new List<GeneSlotInfo>();
        if (string.IsNullOrEmpty(source)) return slots;

        var lines = source.Split('\n');
        string pendingType = null;
        string pendingName = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimStart();

            var roleMatch = RolePattern.Match(line);
            if (roleMatch.Success)
            {
                pendingType = "role";
                pendingName = roleMatch.Groups[1].Value;
                continue;
            }

            var geneMatch = GenePattern.Match(line);
            if (geneMatch.Success)
            {
                pendingType = "gene";
                pendingName = geneMatch.Groups[1].Value;
                continue;
            }

            if (pendingType != null)
            {
                var fnMatch = FnPattern.Match(line);
                if (fnMatch.Success)
                {
                    slots.Add(new GeneSlotInfo(pendingType, pendingName, fnMatch.Groups[1].Value));
                    pendingType = null;
                    pendingName = null;
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // decorator not followed by fn — discard
                    pendingType = null;
                    pendingName = null;
                }
            }
        }

        return slots;
    }

    public static bool[] GetRequiredRoleFillStatus(List<GeneSlotInfo> slots)
    {
        var filled = new bool[RequiredRoles.Length];
        for (int i = 0; i < RequiredRoles.Length; i++)
        {
            for (int j = 0; j < slots.Count; j++)
            {
                if (slots[j].Type == "role" && slots[j].Name == RequiredRoles[i])
                {
                    filled[i] = true;
                    break;
                }
            }
        }
        return filled;
    }

    public static string[] GetRequiredRoleNames() => RequiredRoles;
}
