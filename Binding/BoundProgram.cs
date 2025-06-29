using Delta.Binding.BoundNodes;
using Delta.Diagnostics;
using Delta.Symbols;
using System.Collections.Immutable;

namespace Delta.Binding
{
    internal class BoundProgram(BoundProgram? previous, FnSymbol? scriptFn, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FnSymbol, BoundBlockStmt> functions)
    {
        public BoundProgram? Previous { get; } = previous;
        public FnSymbol? ScriptFn { get; } = scriptFn;
        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
        public ImmutableDictionary<FnSymbol, BoundBlockStmt> Functions { get; } = functions;
    }
}