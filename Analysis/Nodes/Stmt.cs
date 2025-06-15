namespace Delta.Analysis.Nodes
{
    internal abstract class Stmt : Node
    {
    }

    internal class ExprStmt(Expr expr) : Stmt
    {
        public Expr Expr { get; } = expr;

        public override NodeKind Kind => NodeKind.ExprStmt;
        public override TextSpan Span => Expr.Span;
    }

    internal class VarStmt(Token varToken, Token name, Token eqToken, Expr value) : Stmt
    {
        public Token VarToken => varToken;
        public Token Name { get; } = name;
        public Token EqToken { get; } = eqToken;
        public Expr Value => value;
        public override NodeKind Kind => NodeKind.VarStmt;
        public override TextSpan Span => new(varToken.Span.Start, value.Span.End);
    }

    internal class ErrorStmt(params List<Node> nodes) : Stmt
    {
        public List<Node> Nodes { get; } = nodes;
        public override NodeKind Kind => NodeKind.ErrorStmt;
        public override TextSpan Span => new(Nodes.First().Span.Start, Nodes.Last().Span.End);
    }
}