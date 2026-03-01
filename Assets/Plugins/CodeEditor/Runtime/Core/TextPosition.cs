using System;

namespace CodeEditor.Core
{
    public readonly struct TextPosition : IEquatable<TextPosition>, IComparable<TextPosition>
    {
        public readonly int Line;
        public readonly int Column;

        public TextPosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public static readonly TextPosition Zero = new TextPosition(0, 0);

        public int CompareTo(TextPosition other)
        {
            int cmp = Line.CompareTo(other.Line);
            return cmp != 0 ? cmp : Column.CompareTo(other.Column);
        }

        public bool Equals(TextPosition other) => Line == other.Line && Column == other.Column;
        public override bool Equals(object obj) => obj is TextPosition other && Equals(other);
        public override int GetHashCode() => (Line * 397) ^ Column;
        public override string ToString() => $"({Line}:{Column})";

        public static bool operator ==(TextPosition a, TextPosition b) => a.Equals(b);
        public static bool operator !=(TextPosition a, TextPosition b) => !a.Equals(b);
        public static bool operator <(TextPosition a, TextPosition b) => a.CompareTo(b) < 0;
        public static bool operator >(TextPosition a, TextPosition b) => a.CompareTo(b) > 0;
        public static bool operator <=(TextPosition a, TextPosition b) => a.CompareTo(b) <= 0;
        public static bool operator >=(TextPosition a, TextPosition b) => a.CompareTo(b) >= 0;
    }
}
