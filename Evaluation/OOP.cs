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

    internal class ClassInstance(ClassData data, Dictionary<PropertySymbol, object?> properties)
    {
        public ClassData Data { get; } = data;
        public Dictionary<PropertySymbol, object?> Properties { get; } = properties;
    }

    internal sealed class StaticClassInstance(ClassSymbol classSymbol, Dictionary<PropertySymbol, object?> properties)
        : ClassInstance(new(classSymbol.Name, null, classSymbol.Properties, classSymbol.MethodsWithBody.Where(m => m.Key.Attributes.IsStatic).ToImmutableDictionary()), properties)
    {
    }
}