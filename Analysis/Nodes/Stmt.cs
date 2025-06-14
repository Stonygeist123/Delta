namespace Delta.Analysis.Nodes
{
    internal abstract class Stmt : Node
    {
    }

    internal class ExprStmt(Expr expr) : Stmt
    {
        public Expr Expr { get; } = expr;

        public override NodeKind Kind => NodeKind.ExprStmt;
    }

    internal class VarStmt(Token varToken, Token name, Token eqToken, Expr value) : Stmt
    {
        public Token VarToken { get; } = varToken;
        public Token Name { get; } = name;
        public Token EqToken { get; } = eqToken;
        public Expr Value { get; } = value;
        public override NodeKind Kind => NodeKind.VarStmt;
    }

    internal class ErrorStmt(params List<Node> nodes) : Stmt
    {
        public List<Node> Nodes { get; } = nodes;
        public override NodeKind Kind => NodeKind.ErrorStmt;
    }
}