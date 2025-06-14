namespace Delta.Analysis
{
    internal enum NodeKind
    {
        // Operators
        Plus, Minus, Slash, Star,

        // Literals
        Number,

        // Others
        LParen, RParen, Bad, EOF,

        // Exprs
        LiteralExpr, BinaryExpr, UnaryExpr, GroupingExpr, ErrorExpr,
    }

    internal static class Utility
    {
        public static int GetBinOpPrecedence(NodeKind kind) => kind switch
        {
            NodeKind.Plus => 1,
            NodeKind.Minus => 1,
            NodeKind.Star => 2,
            NodeKind.Slash => 2,
            _ => 0
        };

        public static int GetUnOpPrecedence(NodeKind kind) => kind switch
        {
            NodeKind.Plus => 3,
            NodeKind.Minus => 3,
            _ => 0
        };
    }
}