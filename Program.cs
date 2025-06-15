using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Binding;
using Delta.Binding.BoundNodes;
using Delta.Environment;
using Delta.Interpreter;

Interpreter interpreter = new();
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
        Binder binder = new(text);
        List<BoundStmt> boundStmts = binder.Bind(stmts);
        BoundASTPrinter.PrintAll(boundStmts);
    }

    Console.WriteLine();
    Console.WriteLine();
}