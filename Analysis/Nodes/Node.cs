namespace Delta.Analysis.Nodes
{
    internal enum NodeKind
    {
        // Operators
        Plus, Minus, Slash, Star,

        // Literals
        Number, String, Identifier,

        // Keywords
        Var,

        // Others
        LParen, RParen,

        Eq,

        Bad, EOF,

        // Exprs
        LiteralExpr, BinaryExpr, UnaryExpr, GroupingExpr, NameExpr, ErrorExpr,

        // Stmts
        ExprStmt, VarStmt, ErrorStmt
    }

    internal abstract class Node
    {
        public abstract NodeKind Kind { get; }
        public abstract TextSpan Span { get; }
    }
}