namespace Delta.Analysis
{
    internal class Token(NodeKind kind, string lexeme, int start, int end) : Node
    {
        public override NodeKind Kind => kind;
        public string Lexeme => lexeme;
        public int Start => start;
        public int End => end;
        public int Length => End - Start;

        public override string ToString() => $"{Kind}: '{Lexeme}' at {Start}-{End}";
    }
}