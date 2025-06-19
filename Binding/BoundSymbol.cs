namespace Delta.Binding
{
    internal abstract class BoundSymbol(string name, BoundType type)
    {
        public string Name { get; } = name;
        public BoundType Type { get; } = type;
    }

    internal class VarSymbol(string name, BoundType type, bool mutable) : BoundSymbol(name, type)
    {
        public bool Mutable { get; } = mutable;
    }
}