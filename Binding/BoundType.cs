namespace Delta.Binding
{
    internal readonly struct BoundType(string type)
    {
        public readonly string Type { get; } = type;
        public static BoundType Number => new("number");
        public static BoundType String => new("string");
        public static BoundType Error => new("?");
        public static BoundType Bool => new("bool");

        public static bool operator ==(BoundType a, BoundType b) => a.Type == b.Type;

        public static bool operator !=(BoundType a, BoundType b) => a.Type != b.Type;

        public override bool Equals(object? obj)
        {
            if (obj is BoundType other)
                return this == other;
            throw new NotImplementedException();
        }

        public override int GetHashCode() => Type.GetHashCode();

        public override string ToString() => Type == "?" ? "unknown" : Type;
    }
}