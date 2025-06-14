using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Interpreter;

Interpreter interpreter = new();
while (true)
{
    ConsoleColor defaultColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.DarkGreen;
    Console.Write("> ");
    Console.ForegroundColor = defaultColor;

    string? text = Console.ReadLine();
    if (string.IsNullOrEmpty(text))
        continue;
    Parser parser = new(text);
    List<Stmt> stmts = parser.Parse();
    if (parser.Diagnostics.Any())
        parser.Diagnostics.Print();
    else
    {
        //foreach (Stmt stmt in stmts)
        //    ASTPrinter.Print(stmt);
        if (stmts.Count == 1 && stmts.First() is ExprStmt exprStmt)
        {
            object? result = interpreter.ExecuteExpr(exprStmt.Expr);
            if (result is not null)
                Console.WriteLine(result);
        }
        else
            stmts.ForEach(interpreter.Execute);
    }

    Console.WriteLine();
    Console.WriteLine();
}