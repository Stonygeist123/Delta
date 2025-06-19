using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Binding;
using Delta.Binding.BoundNodes;
using Delta.Interpreter;

BoundScope boundGlobalScope = new(null);
Scope globalScope = new(null);
Interpreter interpreter = new(globalScope);
while (true)
{
    ConsoleColor defaultColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.DarkGreen;
    Console.Write("> ");
    Console.ForegroundColor = ConsoleColor.White;
    string? text = Console.ReadLine();
    Console.ForegroundColor = defaultColor;
    if (string.IsNullOrEmpty(text))
        continue;
    Parser parser = new(text);
    List<Stmt> stmts = parser.Parse();
    if (parser.Diagnostics.Any())
        parser.Diagnostics.Print();
    else
    {
        Binder binder = new(text, boundGlobalScope);
        List<BoundStmt> boundStmts = binder.Bind(stmts);
        if (binder.Diagnostics.Any())
            binder.Diagnostics.Print();
        else
        {
            if (boundStmts.Last() is BoundExprStmt exprStmt)
            {
                boundStmts.RemoveAt(boundStmts.Count - 1);
                boundStmts.ForEach(interpreter.Execute);
                Console.WriteLine(interpreter.ExecuteExpr(exprStmt.Expr));
            }
            else
                boundStmts.ForEach(interpreter.Execute);
        }
        //BoundASTPrinter.PrintAll(boundStmts);
    }
}