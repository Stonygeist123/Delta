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
        Var, Mut, True, False, If, Else, Loop, Fn, Ret,

        // Others
        LParen, RParen, LBrace, RBrace, Comma, Eq, Colon, Arrow, Semicolon,

        Bad, EOF,

        // Misc
        ParameterList, Param, Arg, TypeClause,

        // Exprs
        LiteralExpr, BinaryExpr, UnaryExpr, GroupingExpr, NameExpr, AssignExpr, CallExpr, ErrorExpr,

        // Stmts
        ExprStmt, VarStmt, BlockStmt, IfStmt, ElseStmt, LoopStmt, FnDecl, RetStmt, ErrorStmt
    }

    internal abstract class Node
    {
        public abstract NodeKind Kind { get; }
        public abstract TextSpan Span { get; }
    }
}