namespace Delta.Analysis.Nodes
{
    internal enum NodeKind
    {
        // Operators
        Plus, Minus, Slash, Star,

        Not, EqEq, NotEq, Greater, GreaterEq, Less, LessEq, And, Or,

        // Literals
        Number, String, Identifier,

        // Keywords
        Var, Mut, True, False, If, Else, Loop,

        // Others
        LParen, RParen, LBrace, RBrace,

        Eq,

        Bad, EOF,

        // Exprs
        LiteralExpr, BinaryExpr, UnaryExpr, GroupingExpr, NameExpr, AssignExpr, ErrorExpr,

        // Stmts
        ExprStmt, VarStmt, BlockStmt, IfStmt, ElseStmt, LoopStmt, ErrorStmt
    }

    internal abstract class Node
    {
        public abstract NodeKind Kind { get; }
        public abstract TextSpan Span { get; }
    }
}