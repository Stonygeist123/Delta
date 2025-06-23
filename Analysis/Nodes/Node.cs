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
        Var, Mut, True, False, If, Else, Loop, Fn,

        // Others
        LParen, RParen, LBrace, RBrace, Comma, Eq, Colon,

        Bad, EOF,

        // Misc
        ParameterList, Param, Arg, TypeClause,

        // Exprs
        LiteralExpr, BinaryExpr, UnaryExpr, GroupingExpr, NameExpr, AssignExpr, CallExpr, ErrorExpr,

        // Stmts
        ExprStmt, VarStmt, BlockStmt, IfStmt, ElseStmt, LoopStmt, FnDecl, ErrorStmt
    }

    internal abstract class Node
    {
        public abstract NodeKind Kind { get; }
        public abstract TextSpan Span { get; }
    }
}