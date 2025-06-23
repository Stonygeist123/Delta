using System.Reflection;

namespace Delta.Binding
{
    internal readonly struct BoundType(string type)
    {
        public readonly string Type { get; } = type;
        public static BoundType Number => new("number");
        public static BoundType String => new("string");
        public static BoundType Bool => new("bool");
        public static BoundType Any => new("any");
        public static BoundType Void => new("void");
        public static BoundType Error => new("?");

        public static bool operator ==(BoundType a, BoundType b) => a.Type == Any.Type || b.Type == Any.Type || a.Type == b.Type;

        public static bool operator !=(BoundType a, BoundType b) => !(a == b);

        public override bool Equals(object? obj)
        {
            if (obj is BoundType other)
                return this == other;
            throw new NotImplementedException();
        }

        public override int GetHashCode() => Type.GetHashCode();

        public override string ToString() => Type == "?" ? "unknown" : Type;

        public static BoundType? Bind(string name) => typeof(BoundType).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Static)
                ?.Where(p => p.PropertyType == typeof(BoundType)).Select(p => (BoundType?)p.GetValue(null)).First(t => t.HasValue && t.Value.Type == name);
    }
}