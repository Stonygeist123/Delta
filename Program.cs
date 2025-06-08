using Delta.Analysis;

while (true)
{
    string? text = Console.ReadLine();
    if (string.IsNullOrEmpty(text))
        continue;
    Lexer lexer = new(text);
    List<Token> tokens = lexer.Lex();
    foreach (Token token in tokens)
        Console.WriteLine(token);
}