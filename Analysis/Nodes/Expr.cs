namespace Delta.Analysis.Nodes
{
    internal abstract class Expr(SyntaxTree syntaxTree) : Node(syntaxTree)
    {
    }

    internal class LiteralExpr(SyntaxTree syntaxTree, Token token) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.LiteralExpr;
        public Token Token => token;
        public override TextSpan Span => token.Span;
    }

    internal class BinaryExpr(SyntaxTree syntaxTree, Expr left, Token op, Expr right) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.BinaryExpr;
        public Expr Left { get; } = left;
        public Token Op { get; } = op;
        public Expr Right { get; } = right;
    }

    internal class UnaryExpr(SyntaxTree syntaxTree, Token op, Expr operand) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.UnaryExpr;
        public Token Op { get; } = op;
        public Expr Operand { get; } = operand;
    }

    internal class GroupingExpr(SyntaxTree syntaxTree, Token lParen, Expr expression, Token rParen) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.GroupingExpr;
        public Token LParen { get; } = lParen;
        public Expr Expression { get; } = expression;
        public Token RParen { get; } = rParen;
    }

    internal class NameExpr(SyntaxTree syntaxTree, Token name) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.NameExpr;
        public Token Name { get; } = name;
    }

    internal class AssignExpr(SyntaxTree syntaxTree, Token name, Token eqToken, Expr value) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.AssignExpr;
        public Token Name { get; } = name;
        public Token EqToken { get; } = eqToken;
        public Expr Value { get; } = value;
    }

    internal class CallExpr(SyntaxTree syntaxTree, Token name, Token lParen, List<Arg> args, Token rParen) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.CallExpr;
        public Token Name { get; } = name;
        public Token LParen { get; } = lParen;
        public List<Arg> Args { get; } = args;
        public Token RParen { get; } = rParen;
    }

    internal class ErrorExpr(SyntaxTree syntaxTree, params List<Node> nodes) : Expr(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.ErrorExpr;
        public List<Node> Nodes { get; } = nodes;
    }
}