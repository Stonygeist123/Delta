using System.Diagnostics.CodeAnalysis;

namespace Delta.Binding
{
    internal class BoundScope(BoundScope? _parent)
    {
        private readonly Dictionary<string, VarSymbol> _variables = [];
        public Dictionary<string, VarSymbol> Variables => _variables;
        public BoundScope? Parent { get; } = _parent;

        public bool TryDeclareVar(string name, BoundType type, bool mutable, [MaybeNullWhen(false)] out VarSymbol symbol)

        {
            if (HasVar(name))

            {
                symbol = null;
                return false;
            }

            Variables.Add(name, symbol = new VarSymbol(name, type, mutable));
            return true;
        }

        public bool TryDeclareVar(string name, BoundType type, bool mutable) => TryDeclareVar(name, type, mutable, out _);

        public bool TryGetVar(string name, [MaybeNullWhen(false)] out VarSymbol variable) => Variables.TryGetValue(name, out variable) || Parent is not null && Parent.TryGetVar(name, out variable);

        public bool HasVar(string name) => Variables.ContainsKey(name) || Parent is not null && Parent.HasVar(name);
    }
}