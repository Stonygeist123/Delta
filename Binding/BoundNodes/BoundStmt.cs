namespace Delta.Binding.BoundNodes
{
    internal abstract class BoundStmt
    {
    }

    internal sealed class BoundExprStmt(BoundExpr expr) : BoundStmt
    {
        public BoundExpr Expr { get; } = expr;
    }

    internal sealed class BoundVarStmt(VarSymbol symbol, BoundExpr value) : BoundStmt
    {
        public VarSymbol Symbol { get; } = symbol;
        public BoundExpr Value { get; } = value;
    }

    internal sealed class BoundBlockStmt(List<BoundStmt> stmts) : BoundStmt
    {
        public List<BoundStmt> Stmts { get; } = stmts;
    }

    internal sealed class BoundIfStmt(BoundExpr condition, BoundStmt thenStmt, BoundStmt? elseClause) : BoundStmt
    {
        public BoundExpr Condition { get; } = condition;
        public BoundStmt ThenStmt { get; } = thenStmt;
        public BoundStmt? ElseClause { get; } = elseClause;
    }

    internal sealed class BoundLoopStmt(BoundExpr condition, BoundStmt thenStmt) : BoundStmt
    {
        public BoundExpr Condition { get; } = condition;
        public BoundStmt ThenStmt { get; } = thenStmt;
    }

    internal sealed class BoundFnDecl(FnSymbol symbol) : BoundStmt
    {
        public FnSymbol Symbol { get; } = symbol;
    }
}