using Delta.Analysis.Nodes;

namespace Delta.Binding
{
    internal enum BoundOpKind

    {
        Plus,
        Minus,
        Star,
        Slash
    }

    internal class BoundBinOperator(BoundOpKind op, BoundType left, BoundType right, BoundType result, Func<object, object, object> execute)
    {
        public BoundOpKind OpKind { get; } = op;
        public BoundType Left { get; } = left;
        public BoundType Right { get; } = right;
        public BoundType Result { get; } = result;
        public Func<object, object, object> Execute { get; } = execute;

        public NodeKind NodeKind => OpKind switch
        {
            BoundOpKind.Plus => NodeKind.Plus,
            BoundOpKind.Minus => NodeKind.Minus,
            BoundOpKind.Star => NodeKind.Star,
            BoundOpKind.Slash => NodeKind.Slash,
            _ => throw new ArgumentOutOfRangeException(nameof(OpKind), $"Unknown operator kind: {OpKind}")
        };

        private static readonly List<BoundBinOperator> _operators =
        [
            new BoundBinOperator(BoundOpKind.Plus, BoundType.Number, BoundType.Number, BoundType.Number, (a, b) => (double)a + (double)b),
            new BoundBinOperator(BoundOpKind.Star, BoundType.Number, BoundType.Number, BoundType.Number, (a, b) => (double)a * (double)b),
            new BoundBinOperator(BoundOpKind.Minus, BoundType.Number, BoundType.Number, BoundType.Number, (a, b) => (double)a - (double)b),
            new BoundBinOperator(BoundOpKind.Slash, BoundType.Number, BoundType.Number, BoundType.Number, (a, b) => (double)a / (double)b),

            new BoundBinOperator(BoundOpKind.Plus, BoundType.String, BoundType.String, BoundType.String, (a, b) => (string)a + (string)b),
            new BoundBinOperator(BoundOpKind.Plus, BoundType.String, BoundType.Number, BoundType.String, (a, b) => (string)a + (double)b),
            new BoundBinOperator(BoundOpKind.Plus, BoundType.Number, BoundType.String, BoundType.String, (a, b) => (double)a + (string)b),
        ];

        public static BoundBinOperator? Bind(NodeKind kind, BoundType left, BoundType right)
            => _operators.Find(op => op.NodeKind == kind && op.Left == left && op.Right == right);
    }

    internal class BoundUnOperator(BoundOpKind op, BoundType operand, BoundType result, Func<object, object> execute)
    {
        public BoundOpKind OpKind { get; } = op;
        public BoundType Operand { get; } = operand;
        public BoundType Result { get; } = result;
        public Func<object, object> Execute { get; } = execute;

        public NodeKind NodeKind => OpKind switch
        {
            BoundOpKind.Plus => NodeKind.Plus,
            BoundOpKind.Minus => NodeKind.Minus,
            _ => throw new ArgumentOutOfRangeException(nameof(OpKind), $"Unknown operator kind: {OpKind}")
        };

        private static readonly List<BoundUnOperator> _operators =
        [
            new BoundUnOperator(BoundOpKind.Plus, BoundType.Number, BoundType.Number, (a) => a),
            new BoundUnOperator(BoundOpKind.Minus, BoundType.Number, BoundType.Number, (a) => -(double)a),
        ];

        public static BoundUnOperator? Bind(NodeKind kind, BoundType operand)
            => _operators.Find(op => op.NodeKind == kind && op.Operand == operand);
    }
}