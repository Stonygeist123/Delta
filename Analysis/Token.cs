namespace Delta.Analysis
{
    internal enum TokenKind
    {
        Plus, Minus, Slash, Star,
        Number,
        Bad
    }

    internal readonly struct Token(TokenKind kind, string lexeme, int start, int end)
    {
        public TokenKind Kind { get; } = kind;
        public string Lexeme { get; } = lexeme;
        public int Start { get; } = start;
        public int End { get; } = end;
        public int Length => End - Start;

        public override string ToString() => $"{Kind}: '{Lexeme}' at {Start}-{End}";
    }
}