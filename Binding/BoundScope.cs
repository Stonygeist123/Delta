using Delta.Binding.BoundNodes;
using Delta.Environment;
using System.Diagnostics.CodeAnalysis;

namespace Delta.Binding
{
    internal class BoundScope(BoundScope? _parent)
    {
        private readonly Dictionary<string, VarSymbol> _variables = [];
        private readonly Dictionary<string, FnSymbol> _fns = [];
        public Dictionary<string, VarSymbol> Variables => _variables;
        public Dictionary<string, FnSymbol> Fns => _fns;
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

        public bool TryDeclareFn(string name, BoundType type, BoundBlockStmt body, List<ParamSymbol> paramList, [MaybeNullWhen(false)] out FnSymbol symbol)
        {
            if (HasFn(name))
            {
                symbol = null;
                return false;
            }

            Fns.Add(name, symbol = new FnSymbol(name, type, paramList, body));
            return true;
        }

        public bool TryGetVar(string name, [MaybeNullWhen(false)] out VarSymbol variable) => Variables.TryGetValue(name, out variable) || Parent is not null && Parent.TryGetVar(name, out variable);

        public bool TryGetFn(string name, [MaybeNullWhen(false)] out FnSymbol fn)
        {
            FnSymbol? builtInFn = BuiltIn.Fns.Find(f => f.Name == name);
            if (builtInFn != null)
            {
                fn = builtInFn;
                return true;
            }

            return (Fns.TryGetValue(name, out fn) || Parent is not null && Parent.TryGetFn(name, out fn));
        }

        public bool HasVar(string name) => Variables.ContainsKey(name) || Parent is not null && Parent.HasVar(name);

        public bool HasFn(string name) => Fns.ContainsKey(name) || Parent is not null && Parent.HasFn(name);
    }
}