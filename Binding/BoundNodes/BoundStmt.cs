namespace Delta.Binding.BoundNodes
{
    internal abstract class BoundStmt
    {
    }

    internal sealed class BoundExprStmt(BoundExpr expr) : BoundStmt
    {
        public BoundExpr Expr { get; } = expr;
    }

    internal sealed class BoundVarStmt(string name, BoundExpr value) : BoundStmt
    {
        public string Name { get; } = name;
        public BoundExpr Value { get; } = value;
    }
}