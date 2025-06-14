namespace Delta.Analysis.Nodes
{
    internal abstract class Expr : Node
    {
    }

    internal class LiteralExpr(Token token) : Expr
    {
        public override NodeKind Kind => NodeKind.LiteralExpr;

        public Token Token => token;
    }

    internal class BinaryExpr(Expr left, Token op, Expr right) : Expr
    {
        public override NodeKind Kind => NodeKind.BinaryExpr;
        public Expr Left { get; } = left;
        public Token Op { get; } = op;
        public Expr Right { get; } = right;
    }

    internal class UnaryExpr(Token op, Expr operand) : Expr
    {
        public override NodeKind Kind => NodeKind.UnaryExpr;
        public Token Op { get; } = op;
        public Expr Operand { get; } = operand;
    }

    internal class GroupingExpr(Token lParen, Expr expression, Token rParen) : Expr
    {
        public override NodeKind Kind => NodeKind.GroupingExpr;
        public Token LParen { get; } = lParen;
        public Expr Expression { get; } = expression;
        public Token RParen { get; } = rParen;
    }

    internal class NameExpr(Token name) : Expr
    {
        public override NodeKind Kind => NodeKind.NameExpr;
        public Token Name { get; } = name;
    }

    internal class ErrorExpr(params List<Node> nodes) : Expr
    {
        public override NodeKind Kind => NodeKind.ErrorExpr;
        public List<Node> Nodes { get; } = nodes;
    }
}