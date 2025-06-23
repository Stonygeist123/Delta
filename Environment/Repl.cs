using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Binding;
using Delta.Binding.BoundNodes;
using Delta.Evaluation;

namespace Delta.Environment
{
    internal class Repl
    {
        private string? _text;
        private static readonly BoundScope boundGlobalScope = new(null);
        private static readonly Scope _globalScope = new(null);
        private static readonly Interpreter _interpreter = new(_globalScope);

        public void Start()
        {
            while (true)
            {
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("> ");
                Console.ForegroundColor = ConsoleColor.White;
                _text = Console.ReadLine();
                Console.ForegroundColor = defaultColor;
                if (string.IsNullOrWhiteSpace(_text))
                    continue;
                Parser parser = new(_text);
                List<Stmt> stmts = parser.Parse();
                if (parser.Diagnostics.Any())
                    parser.Diagnostics.Print();
                else
                    Run(stmts);
            }
        }

        public void Run(List<Stmt> stmts)
        {
            if (string.IsNullOrWhiteSpace(_text))
                return;

            Binder binder = new(_text, boundGlobalScope);
            List<BoundStmt> boundStmts = binder.Bind(stmts);
            if (binder.Diagnostics.Any())
                binder.Diagnostics.Print();
            else
            {
                if (boundStmts.Last() is BoundExprStmt exprStmt)
                {
                    boundStmts.RemoveAt(boundStmts.Count - 1);
                    boundStmts.ForEach(_interpreter.Execute);
                    Console.WriteLine(_interpreter.ExecuteExpr(exprStmt.Expr));
                }
                else
                    boundStmts.ForEach(_interpreter.Execute);
            }
        }
    }
}