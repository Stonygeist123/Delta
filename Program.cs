using Delta.Analysis;

while (true)
{
    string? text = Console.ReadLine();
    if (string.IsNullOrEmpty(text))
        continue;
    Lexer lexer = new(text);
    List<Token> tokens = lexer.Lex();
    Parser parser = new(tokens);
    Expr expr = parser.Parse();
    if (expr is ErrorExpr)
        Console.WriteLine("Error parsing expression.");
}