using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Environment;

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
        foreach (Stmt stmt in stmts)
            ASTPrinter.Print(stmt);
    //Console.WriteLine(Interpreter.Execute(expr));

    Console.WriteLine();
    Console.WriteLine();
}