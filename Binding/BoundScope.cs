using Delta.Binding.BoundNodes;
using Delta.Diagnostics;
using Delta.Symbols;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Delta.Binding
{
    internal class BoundScope(BoundScope? _parent)
    {
        private readonly Dictionary<string, VarSymbol> _variables = [];
        private readonly Dictionary<string, FnSymbol> _fns = [];
        public ImmutableArray<VarSymbol> Variables => [.. _variables.Values];
        public ImmutableArray<FnSymbol> Fns => [.. _fns.Values];
        public BoundScope? Parent { get; } = _parent;

        public bool TryDeclareVar(VarSymbol symbol)
        {
            if (TryLookupVar(symbol.Name, out _))
                return false;
            _variables.Add(symbol.Name, symbol);
            return true;
        }

        public bool TryDeclareFn(FnSymbol fn)
        {
            if (TryLookupFn(fn.Name, out _))
                return false;

            _fns.Add(fn.Name, fn);
            return true;
        }

        public bool TryLookupVar(string name, [MaybeNullWhen(false)] out VarSymbol variable) => _variables.TryGetValue(name, out variable) || Parent is not null && Parent.TryLookupVar(name, out variable);

        public bool TryLookupFn(string name, [MaybeNullWhen(false)] out FnSymbol fn)
        {
            FnSymbol? builtInFn = BuiltIn.Fns.Find(f => f.Name == name);
            if (builtInFn != null)
            {
                fn = builtInFn;
                return true;
            }

            return _fns.TryGetValue(name, out fn) || Parent is not null && Parent.TryLookupFn(name, out fn);
        }
    }

    internal sealed class BoundGlobalScope(BoundGlobalScope? previous, FnSymbol scriptFn,
        BoundBlockStmt stmt, ImmutableArray<VarSymbol> variables, ImmutableArray<FnSymbol> functions, ImmutableArray<Diagnostic> diagnostics)
    {
        public BoundGlobalScope? Previous { get; } = previous;
        public FnSymbol ScriptFn { get; } = scriptFn;
        public BoundBlockStmt Stmt { get; } = stmt;
        public ImmutableArray<VarSymbol> Variables { get; } = variables;
        public ImmutableArray<FnSymbol> Functions { get; } = functions;
        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
    }
}