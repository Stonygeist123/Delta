using Delta.Binding.BoundNodes;
using Delta.Diagnostics;
using Delta.Evaluation;
using Delta.Symbols;
using System.Collections.Immutable;

namespace Delta.Binding
{
    internal class BoundProgram(BoundProgram? previous, FnSymbol? scriptFn, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FnSymbol, BoundBlockStmt> functions, ImmutableDictionary<ClassSymbol, ClassData> classes)
    {
        public BoundProgram? Previous { get; } = previous;
        public FnSymbol? ScriptFn { get; } = scriptFn;
        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
        public ImmutableDictionary<FnSymbol, BoundBlockStmt> Functions { get; } = functions;
        public ImmutableDictionary<ClassSymbol, ClassData> Classes { get; } = classes;
    }
}