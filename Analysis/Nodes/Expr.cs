namespace Delta.Analysis.Nodes
{
    internal abstract class Expr : Node
    {
    }

    internal class LiteralExpr(Token token) : Expr
    {
        public override NodeKind Kind => NodeKind.LiteralExpr;
        public Token Token => token;
        public override TextSpan Span => token.Span;
    }

    internal class BinaryExpr(Expr left, Token op, Expr right) : Expr
    {
        public override NodeKind Kind => NodeKind.BinaryExpr;
        public Expr Left { get; } = left;
        public Token Op { get; } = op;
        public Expr Right { get; } = right;
        public override TextSpan Span => new(Left.Span.Start, Right.Span.End);
    }

    internal class UnaryExpr(Token op, Expr operand) : Expr
    {
        public override NodeKind Kind => NodeKind.UnaryExpr;
        public Token Op { get; } = op;
        public Expr Operand { get; } = operand;
        public override TextSpan Span => new(Op.Span.Start, Operand.Span.End);
    }

    internal class GroupingExpr(Token lParen, Expr expression, Token rParen) : Expr
    {
        public override NodeKind Kind => NodeKind.GroupingExpr;
        public Token LParen { get; } = lParen;
        public Expr Expression { get; } = expression;
        public Token RParen { get; } = rParen;
        public override TextSpan Span => new(LParen.Span.Start, RParen.Span.End);
    }

    internal class NameExpr(Token name) : Expr
    {
        public override NodeKind Kind => NodeKind.NameExpr;
        public Token Name { get; } = name;
        public override TextSpan Span => Name.Span;
    }

    internal class AssignExpr(Token name, Token eqToken, Expr value) : Expr
    {
        public override NodeKind Kind => NodeKind.AssignExpr;
        public Token Name { get; } = name;
        public Token EqToken { get; } = eqToken;
        public Expr Value { get; } = value;
        public override TextSpan Span => new(Name.Span.Start, Value.Span.End);
    }

    internal class Arg(Token? comma, Expr arg) : Node
    {
        public Token? Comma = comma;
        public Expr Expr = arg;
        public override NodeKind Kind => NodeKind.Arg;
        public override TextSpan Span => new((Comma?.Span ?? Expr.Span).Start, Expr.Span.End);
    }

    internal class CallExpr(Token name, Token lParen, List<Arg> args, Token rParen) : Expr
    {
        public override NodeKind Kind => NodeKind.CallExpr;
        public Token Name { get; } = name;
        public Token LParen { get; } = lParen;
        public List<Arg> Args { get; } = args;
        public Token RParen { get; } = rParen;
        public override TextSpan Span => new(Name.Span.Start, RParen.Span.End);
    }

    internal class ErrorExpr(params List<Node> nodes) : Expr
    {
        public override NodeKind Kind => NodeKind.ErrorExpr;
        public List<Node> Nodes { get; } = nodes;

        public override TextSpan Span => new(Nodes.First().Span.Start, Nodes.Last().Span.End);
    }
}