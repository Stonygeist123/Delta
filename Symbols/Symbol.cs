using Delta.Analysis.Nodes;
using System.Collections.Immutable;
using System.Reflection;

namespace Delta.Symbols
{
    internal abstract class Symbol(string name)
    {
        public string Name { get; } = name;

        public void WriteTo(TextWriter writer) => SymbolPrinter.WriteTo(this, writer);

        public override string ToString()
        {
            using StringWriter writer = new();
            WriteTo(writer);
            return writer.ToString();
        }
    }

    internal abstract class VarSymbol(string name, TypeSymbol type, bool mutable) : Symbol(name)
    {
        public TypeSymbol Type { get; } = type;
        public bool Mutable { get; } = mutable;
    }

    internal sealed class GlobalVarSymbol(string name, TypeSymbol type, bool mutable) : VarSymbol(name, type, mutable)
    {
    }

    internal class LocalVarSymbol(string name, TypeSymbol type, bool mutable) : VarSymbol(name, type, mutable)
    {
    }

    internal sealed class ParamSymbol(string name, TypeSymbol type) : LocalVarSymbol(name, type, true)
    {
    }

    internal class FnSymbol(string name, TypeSymbol returnType, ImmutableArray<ParamSymbol> parameters, FnDecl? decl = null) : Symbol(name)
    {
        public TypeSymbol ReturnType { get; } = returnType;
        public ImmutableArray<ParamSymbol> Parameters { get; } = parameters;
        public FnDecl? Decl { get; } = decl;
    }

    internal enum Accessibility
    {
        Pub, Priv
    }

    internal sealed class PropertySymbol(Accessibility accessibility, string name, TypeSymbol type, bool mutable) : VarSymbol(name, type, mutable)
    {
        public Accessibility Accessibility { get; } = accessibility;
    }

    internal sealed class MethodSymbol(Accessibility accessibility, string name, TypeSymbol returnType, ImmutableArray<ParamSymbol> parameters, MethodDecl? decl = null) : Symbol(name)
    {
        public Accessibility Accessibility { get; } = accessibility;
        public TypeSymbol ReturnType { get; } = returnType;
        public ImmutableArray<ParamSymbol> Parameters { get; } = parameters;
        public MethodDecl? Decl { get; } = decl;
    }

    internal sealed class ClassSymbol(string name, ImmutableArray<PropertySymbol> properties, ImmutableArray<MethodSymbol> methods, ClassDecl decl) : Symbol(name)
    {
        public ImmutableArray<PropertySymbol> Properties { get; } = properties;
        public ImmutableArray<MethodSymbol> Methods { get; } = methods;
        public ClassDecl Decl { get; } = decl;
    }

    internal class BuiltInFn(string name, TypeSymbol type, IEnumerable<ParamSymbol> paramList) : FnSymbol(name, type, [.. paramList], null)
    {
    }

    internal sealed class TypeSymbol(string name) : Symbol(name)
    {
        public static TypeSymbol Number => new("number");
        public static TypeSymbol String => new("string");
        public static TypeSymbol Bool => new("bool");
        public static TypeSymbol Any => new("any");
        public static TypeSymbol Void => new("void");
        public static TypeSymbol Error => new("?");

        public static bool operator ==(TypeSymbol a, TypeSymbol b) => a.Name == Any.Name || b.Name == Any.Name || a.Name == b.Name;

        public static bool operator !=(TypeSymbol a, TypeSymbol b) => !(a == b);

        public override bool Equals(object? obj)
        {
            if (obj is TypeSymbol other)
                return this == other;
            throw new NotImplementedException();
        }

        public override int GetHashCode() => Name.GetHashCode();

        public override string ToString() => Name == "?" ? "unknown" : Name;

        public static TypeSymbol? Bind(string name) => typeof(TypeSymbol).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Static)
                ?.Where(p => p.PropertyType == typeof(TypeSymbol)).Select(p => (TypeSymbol?)p.GetValue(null)).First(t => t is not null && t.Name == name);
    }

    internal sealed class LabelSymbol(string name) : Symbol(name)
    {
    }
}