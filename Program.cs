using Delta.Analysis;
using Delta.Evaluation;
using Delta.IO;

if (args.Length == 0)
{
    DeltaRepl repl = new();
    repl.Run();
}
else if (args.Length == 1)
{
    string path = Path.GetFullPath(args[0]);
    if (!File.Exists(path))
    {
        Console.Out.SetForeground(ConsoleColor.DarkRed);
        Console.Out.WriteLine($"Could not find file '{path}'.");
        Console.Out.ResetColor();
        return;
    }

    string text = File.ReadAllText(path);
    SyntaxTree syntaxTree = SyntaxTree.Parse(SourceText.From(text, "<stdin>"));
    Compilation compilation = Compilation.Create(null, syntaxTree);

    try
    {
        EvaluationResult result = compilation.Evaluate([]);
        if (result.Diagnostics.Any())
            Console.Out.WriteDiagnostics(result.Diagnostics);
    }
    catch (RuntimeException ex)
    {
        Console.Out.SetForeground(ConsoleColor.DarkRed);
        Console.Out.WriteLine(ex);
        Console.Out.ResetColor();
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Usage: delta [filename]\nGot {args.Length} arguments.");
    Console.ResetColor();
}