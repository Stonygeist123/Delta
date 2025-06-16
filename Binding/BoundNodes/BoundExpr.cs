namespace Delta.Binding.BoundNodes
{
    internal abstract class BoundExpr
    {
        public abstract BoundType Type { get; }
    }

    internal sealed class BoundLiteralExpr(object value, BoundType type) : BoundExpr
    {
        public object Value { get; } = value;
        public override BoundType Type { get; } = type;
    }

    internal sealed class BoundBinaryExpr(BoundExpr left, BoundBinOperator op, BoundExpr right) : BoundExpr
    {
        public BoundExpr Left { get; } = left;
        public BoundBinOperator Op { get; } = op;
        public BoundExpr Right { get; } = right;
        public override BoundType Type => Op.Result;
    }

    internal sealed class BoundUnaryExpr(BoundUnOperator op, BoundExpr operand) : BoundExpr
    {
        public BoundUnOperator Op { get; } = op;
        public BoundExpr Operand { get; } = operand;
        public override BoundType Type => Op.Result;
    }

    internal sealed class BoundGroupingExpr(BoundExpr expr) : BoundExpr
    {
        public BoundExpr Expr { get; } = expr;
        public override BoundType Type => Expr.Type;
    }

    internal sealed class BoundNameExpr(BoundVarSymbol symbol) : BoundExpr
    {
        public BoundVarSymbol Symbol { get; } = symbol;
        public override BoundType Type => Symbol.Type;
    }

    internal sealed class BoundAssignExpr(BoundVarSymbol symbol, BoundExpr value) : BoundExpr
    {
        public BoundVarSymbol Symbol { get; } = symbol;
        public BoundExpr Value { get; } = value;
        public override BoundType Type => Symbol.Type;
    }

    internal sealed class BoundError() : BoundExpr
    {
        public override BoundType Type => BoundType.Error;
    }
}