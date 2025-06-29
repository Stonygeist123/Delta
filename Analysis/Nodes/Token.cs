namespace Delta.Analysis.Nodes
{
    internal class Token(SyntaxTree syntaxTree, NodeKind kind, string lexeme, TextSpan span) : Node(syntaxTree)
    {
        public override NodeKind Kind => kind;
        public string Lexeme => lexeme;
        public override TextSpan Span { get; } = span;
    }
}