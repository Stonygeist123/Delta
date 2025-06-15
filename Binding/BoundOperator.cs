using Delta.Analysis;

namespace Delta.Binding
{
    internal class BoundBinOperator(NodeKind op, BoundType left, BoundType right, BoundType result)
    {
        public NodeKind Op { get; } = op;
        public BoundType Left { get; } = left;
        public BoundType Right { get; } = right;
        public BoundType Result { get; } = result;

        private static readonly List<BoundBinOperator> _operators =
        [
            new BoundBinOperator(NodeKind.Plus, BoundType.Number, BoundType.Number, BoundType.Number),
            new BoundBinOperator(NodeKind.Minus, BoundType.Number, BoundType.Number, BoundType.Number),
            new BoundBinOperator(NodeKind.Star, BoundType.Number, BoundType.Number, BoundType.Number),
            new BoundBinOperator(NodeKind.Slash, BoundType.Number, BoundType.Number, BoundType.Number),
        ];

        private static readonly BoundBinOperator _errorOp = new(NodeKind.Bad, BoundType.Error, BoundType.Error, BoundType.Error);

        public static BoundBinOperator Bind(NodeKind kind, BoundType left, BoundType right, out bool valid)
        {
            BoundBinOperator? op = _operators.Find(op => op.Op == kind && op.Left == left && op.Right == right);
            if (op is null)
            {
                valid = false;
                return _errorOp;
            }
            else
            {
                valid = true;
                return op;
            }
        }
    }

    internal class BoundUnOperator(NodeKind op, BoundType operand, BoundType result)
    {
        public NodeKind Op { get; } = op;
        public BoundType Operand { get; } = operand;
        public BoundType Result { get; } = result;

        private static readonly List<BoundUnOperator> _operators =
        [
            new BoundUnOperator(NodeKind.Plus, BoundType.Number, BoundType.Number),
            new BoundUnOperator(NodeKind.Minus, BoundType.Number, BoundType.Number),
        ];

        private static readonly BoundUnOperator _errorOp = new(NodeKind.Bad, BoundType.Error, BoundType.Error);

        public static BoundUnOperator Bind(NodeKind kind, BoundType operand, out bool valid)
        {
            BoundUnOperator? op = _operators.Find(op => op.Op == kind && op.Operand == operand);
            if (op is null)
            {
                valid = false;
                return _errorOp;
            }
            else
            {
                valid = true;
                return op;
            }
        }
    }
}