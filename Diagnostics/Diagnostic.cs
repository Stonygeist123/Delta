using Delta.Analysis;

namespace Delta.Diagnostics
{
    internal class Diagnostic(string text, string message, TextSpan span)
    {
        public string Message { get; } = message;
        public TextSpan Span { get; } = span;
        public string Text { get; } = text;

        public void Print()
        {
            ConsoleColor originalColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(Text);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(new string(' ', Span.Start) + new string('^', Span.Length));

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string(' ', Span.Start) + $"[Error] {Message}");

            Console.ForegroundColor = originalColor;
        }
    }
}