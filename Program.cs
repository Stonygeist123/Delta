using Delta.Analysis;
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
    Expr expr = parser.Parse();
    if (parser.Diagnostics.Any())
        parser.Diagnostics.Print();
    else
        ASTPrinter.Print(expr);

    Console.WriteLine();
    Console.WriteLine();
}