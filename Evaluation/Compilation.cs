using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Binding;
using Delta.Binding.BoundNodes;
using Delta.Diagnostics;
using Delta.IO;
using Delta.Symbols;
using System.CodeDom.Compiler;
using System.Collections.Immutable;

namespace Delta.Evaluation
{
    internal class EvaluationResult(ImmutableArray<Diagnostic> diagnostics, object? value)
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
        public object? Value { get; } = value;
    }

    internal sealed class Compilation(Compilation? previous, params SyntaxTree[] syntaxTrees)
    {
        private BoundGlobalScope? _globalScope;
        public ImmutableArray<FnSymbol> Functions => GlobalScope.Functions;
        public ImmutableArray<VarSymbol> Variables => GlobalScope.Variables;
        public Compilation? Previous { get; } = previous;
        public ImmutableArray<SyntaxTree> SyntaxTrees { get; } = [.. syntaxTrees];

        public static Compilation Create(Compilation? previous, SyntaxTree syntaxTree) => new(previous, syntaxTree);

        public BoundGlobalScope GlobalScope
        {
            get
            {
                if (_globalScope is null)
                {
                    BoundGlobalScope globalScope = Binder.BindGlobalScope(Previous?.GlobalScope, SyntaxTrees);
                    Interlocked.CompareExchange(ref _globalScope, globalScope, null);
                }

                return _globalScope;
            }
        }

        private BoundProgram GetProgram() => Binder.BindProgram(Previous?.GetProgram(), GlobalScope);

        public IEnumerable<Symbol> GetSymbols()
        {
            Compilation? submission = this;
            HashSet<string> seenNames = [];
            while (submission is not null)
            {
                foreach (FnSymbol fn in submission.Functions)
                    if (seenNames.Add(fn.Name))
                        yield return fn;

                foreach (VarSymbol var in submission.Variables)
                    if (seenNames.Add(var.Name))
                        yield return var;

                List<FnSymbol?> builtInFns = [.. typeof(BuiltIn).GetFields().Where(fi => fi.FieldType == typeof(FnSymbol)).Select(fi => (FnSymbol?)fi.GetValue(null))];
                foreach (FnSymbol? biFn in builtInFns)
                    if (biFn is not null && seenNames.Add(biFn.Name))
                        yield return biFn;

                submission = submission.Previous;
            }
        }

        public Compilation ContinueWith(SyntaxTree syntaxTree) => new(this, syntaxTree);

        public EvaluationResult Evaluate(Dictionary<VarSymbol, object?> variables)
        {
            ImmutableArray<Diagnostic> diagnostics = [.. SyntaxTrees.SelectMany(s => s.Diagnostics), .. GlobalScope.Diagnostics];
            if (diagnostics.Any())
                return new(diagnostics, null);

            BoundProgram program = GetProgram()!;
            if (program.Diagnostics.Any())
                return new(program.Diagnostics, null);

            Interpreter interpreter = new(program, variables);
            object? value = interpreter.Execute();
            return new([], value);
        }

        public void EmitTree(TextWriter writer) => EmitTree(GlobalScope.ScriptFn!, writer);

        public void EmitTree(FnSymbol symbol, TextWriter writer)
        {
            BoundProgram program = Binder.BindProgram(GetProgram(), GlobalScope);
            symbol.WriteTo(writer);
            if (!program.Functions.TryGetValue(symbol, out BoundBlockStmt? body))
                return;

            writer.WriteSpace();
            if (body.Stmts.Length == 1 && body.Stmts.First() is BoundExprStmt e)
            {
                writer.WritePunctuation(NodeKind.LBrace);
                writer.WriteLine();
                if (writer is IndentedTextWriter iw)
                    ++iw.Indent;
                else
                    writer.Write(IndentedTextWriter.DefaultTabString);

                writer.WriteKeyword(NodeKind.Ret);
                writer.WriteSpace();
                e.Expr.WriteTo(writer);
                writer.WritePunctuation(NodeKind.Semicolon);
                writer.WriteLine();

                if (writer is IndentedTextWriter iw1)
                    --iw1.Indent;

                writer.WritePunctuation(NodeKind.RBrace);
                writer.WriteLine();
            }
            else
                body.WriteTo(writer);
        }
    }
}