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
        private readonly Dictionary<string, ClassSymbol> _classes = [];
        public ImmutableArray<VarSymbol> Variables => [.. _variables.Values];
        public ImmutableArray<FnSymbol> Fns => [.. _fns.Values];
        public ImmutableArray<ClassSymbol> Classes => [.. _classes.Values];
        public BoundScope? Parent { get; } = _parent;

        public bool TryDeclareVar(VarSymbol symbol)
        {
            if (TryLookupVar(symbol.Name, out _))
                return false;
            _variables.Add(symbol.Name, symbol);
            return true;
        }

        public bool TryDeclareFn(FnSymbol symbol)
        {
            if (TryLookupFn(symbol.Name, out _))
                return false;
            _fns.Add(symbol.Name, symbol);
            return true;
        }

        public bool TryDeclareClass(ClassSymbol symbol)
        {
            if (TryLookupClass(symbol.Name, out _))
                return false;
            _classes.Add(symbol.Name, symbol);
            return true;
        }

        public bool TryLookupVar(string name, [MaybeNullWhen(false)] out VarSymbol symbol) => _variables.TryGetValue(name, out symbol) || Parent is not null && Parent.TryLookupVar(name, out symbol);

        public bool TryLookupFn(string name, [MaybeNullWhen(false)] out FnSymbol symbol)
        {
            FnSymbol? builtInFn = BuiltIn.Fns.Find(f => f.Name == name);
            if (builtInFn != null)
            {
                symbol = builtInFn;
                return true;
            }

            return _fns.TryGetValue(name, out symbol) || Parent is not null && Parent.TryLookupFn(name, out symbol);
        }

        public bool TryLookupClass(string name, [MaybeNullWhen(false)] out ClassSymbol symbol) => _classes.TryGetValue(name, out symbol) || Parent is not null && Parent.TryLookupClass(name, out symbol);
    }

    internal sealed class BoundGlobalScope(BoundGlobalScope? previous, FnSymbol scriptFn,
        BoundBlockStmt stmt, ImmutableArray<VarSymbol> variables, ImmutableArray<FnSymbol> functions, ImmutableArray<ClassSymbol> classes, ImmutableArray<Diagnostic> diagnostics)
    {
        public BoundGlobalScope? Previous { get; } = previous;
        public FnSymbol ScriptFn { get; } = scriptFn;
        public BoundBlockStmt Stmt { get; } = stmt;
        public ImmutableArray<VarSymbol> Variables { get; } = variables;
        public ImmutableArray<FnSymbol> Functions { get; } = functions;
        public ImmutableArray<ClassSymbol> Classes { get; } = classes;
        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
    }
}