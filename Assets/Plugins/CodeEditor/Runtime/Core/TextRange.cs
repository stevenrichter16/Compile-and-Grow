using System;

namespace CodeEditor.Core
{
    public readonly struct TextRange : IEquatable<TextRange>
    {
        public readonly TextPosition Start;
        public readonly TextPosition End;

        public TextRange(TextPosition start, TextPosition end)
        {
            if (start.CompareTo(end) <= 0)
            {
                Start = start;
                End = end;
            }
            else
            {
                Start = end;
                End = start;
            }
        }

        public bool IsEmpty => Start.Equals(End);
        public bool SpansMultipleLines => Start.Line != End.Line;

        public bool Contains(TextPosition pos)
        {
            return pos.CompareTo(Start) >= 0 && pos.CompareTo(End) <= 0;
        }

        public bool Equals(TextRange other) => Start.Equals(other.Start) && End.Equals(other.End);
        public override bool Equals(object obj) => obj is TextRange other && Equals(other);
        public override int GetHashCode() => (Start.GetHashCode() * 397) ^ End.GetHashCode();
        public override string ToString() => $"[{Start}-{End}]";

        public static bool operator ==(TextRange a, TextRange b) => a.Equals(b);
        public static bool operator !=(TextRange a, TextRange b) => !a.Equals(b);
    }
}
