namespace Delta.Analysis
{
    internal class Token(NodeKind kind, string lexeme, TextSpan span) : Node
    {
        public override NodeKind Kind => kind;
        public string Lexeme => lexeme;
        public TextSpan Span { get; } = span;

        public override string ToString() => $"{Kind} [{Span.Start}-{Span.End}]: '{Lexeme}'";
    }
}