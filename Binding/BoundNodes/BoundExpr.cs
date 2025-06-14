using Delta.Analysis;

namespace Delta.Binding.BoundNodes
{
    internal abstract class BoundExpr
    {
    }

    internal sealed class BoundLiteralExpr(object value) : BoundExpr
    {
        public object Value { get; } = value;
    }

    internal sealed class BoundBinaryExpr(BoundExpr left, NodeKind op, BoundExpr right) : BoundExpr
    {
        public BoundExpr Left { get; } = left;
        public NodeKind Op { get; } = op;
        public BoundExpr Right { get; } = right;
    }

    internal sealed class BoundUnaryExpr(NodeKind op, BoundExpr operand) : BoundExpr
    {
        public NodeKind Op { get; } = op;
        public BoundExpr Operand { get; } = operand;
    }

    internal sealed class BoundGroupingExpr(BoundExpr expression) : BoundExpr
    {
        public BoundExpr Expression { get; } = expression;
    }

    internal sealed class BoundVariableExpr(string name) : BoundExpr
    {
        public string Name { get; } = name;
    }

    internal sealed class BoundErrorExpr() : BoundExpr
    {
    }
}