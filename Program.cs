using Delta.Analysis;
using Delta.Environment;

while (true)
{
    string? text = Console.ReadLine();
    if (string.IsNullOrEmpty(text))
        continue;
    Parser parser = new(text);
    Expr expr = parser.Parse();
    if (parser.Diagnostics.Any())
        parser.Diagnostics.Print();
    else
        ASTPrinter.Print(expr);
}