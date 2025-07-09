using Delta.Binding.BoundNodes;
using Delta.Symbols;
using System.Collections.Immutable;

namespace Delta.Evaluation
{
    internal sealed class ClassData(string name, BoundBlockStmt? ctor, ImmutableArray<PropertySymbol> properties, ImmutableDictionary<MethodSymbol, BoundBlockStmt> methods)
    {
        public string Name { get; } = name;
        public BoundBlockStmt? Ctor { get; } = ctor;
        public ImmutableArray<PropertySymbol> Properties { get; } = properties;
        public ImmutableDictionary<MethodSymbol, BoundBlockStmt> Methods { get; } = methods;
    }

    internal sealed class ClassInstance(ClassData data, ImmutableDictionary<PropertySymbol, object?> properties)
    {
        public ClassData Data { get; } = data;
        public ImmutableDictionary<PropertySymbol, object?> Properties { get; } = properties;
    }
}