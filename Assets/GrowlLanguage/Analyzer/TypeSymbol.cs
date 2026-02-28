using System;

namespace GrowlLanguage.Analyzer
{
    /// <summary>
    /// Minimal semantic type representation for early type-checking passes.
    /// </summary>
    public sealed class TypeSymbol
    {
        public static readonly TypeSymbol Unknown = new TypeSymbol("unknown");
        public static readonly TypeSymbol Int = new TypeSymbol("int");
        public static readonly TypeSymbol Float = new TypeSymbol("float");
        public static readonly TypeSymbol String = new TypeSymbol("string");
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol None = new TypeSymbol("none");
        public static readonly TypeSymbol Conflict = new TypeSymbol("conflict");

        public string Name { get; }

        public bool IsUnknown => Name == "unknown";
        public bool IsNumeric => Name == "int" || Name == "float";
        public bool IsString => Name == "string";
        public bool IsBool => Name == "bool";
        public bool IsNone => Name == "none";

        public TypeSymbol(string name)
        {
            Name = string.IsNullOrEmpty(name) ? "unknown" : name;
        }

        public static TypeSymbol FromBuiltinName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Unknown;

            switch (name.Trim().ToLowerInvariant())
            {
                case "int":
                case "integer":
                    return Int;
                case "float":
                case "double":
                case "number":
                    return Float;
                case "string":
                case "str":
                    return String;
                case "bool":
                case "boolean":
                    return Bool;
                case "none":
                case "null":
                case "void":
                    return None;
                default:
                    return Unknown;
            }
        }

        public static TypeSymbol CreateList(TypeSymbol elementType)
        {
            TypeSymbol elem = elementType ?? Unknown;
            return new TypeSymbol("list<" + elem.Name + ">");
        }

        public static TypeSymbol CreateDict(TypeSymbol keyType, TypeSymbol valueType)
        {
            TypeSymbol key = keyType ?? Unknown;
            TypeSymbol value = valueType ?? Unknown;
            return new TypeSymbol("dict<" + key.Name + "," + value.Name + ">");
        }

        public static bool TryGetListElementType(TypeSymbol type, out TypeSymbol elementType)
        {
            elementType = Unknown;
            if (type == null || string.IsNullOrEmpty(type.Name))
                return false;
            if (!type.Name.StartsWith("list<", StringComparison.Ordinal) || !type.Name.EndsWith(">", StringComparison.Ordinal))
                return false;

            string inner = type.Name.Substring(5, type.Name.Length - 6);
            elementType = new TypeSymbol(inner);
            return true;
        }

        public static bool TryGetDictTypes(TypeSymbol type, out TypeSymbol keyType, out TypeSymbol valueType)
        {
            keyType = Unknown;
            valueType = Unknown;
            if (type == null || string.IsNullOrEmpty(type.Name))
                return false;
            if (!type.Name.StartsWith("dict<", StringComparison.Ordinal) || !type.Name.EndsWith(">", StringComparison.Ordinal))
                return false;

            string inner = type.Name.Substring(5, type.Name.Length - 6);
            int split = FindTopLevelComma(inner);
            if (split < 0)
                return false;

            string keyName = inner.Substring(0, split);
            string valueName = inner.Substring(split + 1);
            keyType = new TypeSymbol(keyName);
            valueType = new TypeSymbol(valueName);
            return true;
        }

        private static int FindTopLevelComma(string text)
        {
            if (string.IsNullOrEmpty(text))
                return -1;

            int depth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    depth++;
                    continue;
                }

                if (c == '>')
                {
                    depth = Math.Max(0, depth - 1);
                    continue;
                }

                if (c == ',' && depth == 0)
                    return i;
            }

            return -1;
        }

        public override string ToString() => Name;
    }
}
