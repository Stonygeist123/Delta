namespace Delta.Diagnostics
{
    internal class Diagnostic(string message, string text, int column, int length)
    {
        public string Message { get; } = message;
        public int Column { get; } = column;
        public string Text => text;

        public void Print()
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(text);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new string(' ', Column) + new string('^', length));

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string(' ', Column) + $"[Error] {Message}");

            Console.ForegroundColor = originalColor;
        }
    }
}