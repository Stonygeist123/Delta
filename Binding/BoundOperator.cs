using Delta.Analysis.Nodes;

namespace Delta.Binding
{
    internal enum BoundOpKind

    {
        Plus,
        Minus,
        Star,
        Slash,
        EqEq,
        NotEq,
        Greater,
        GreaterEq,
        Less,
        LessEq,
        And,
        Or,
        Not
    }

    internal class BoundBinOperator(BoundOpKind op, BoundType left, BoundType right, BoundType result, Func<object, object, object> execute)
    {
        public BoundBinOperator(BoundOpKind op, BoundType type, Func<object, object, object> execute) : this(op, type, type, type, execute)
        {
        }

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
            BoundOpKind.EqEq => NodeKind.EqEq,
            BoundOpKind.NotEq => NodeKind.NotEq,
            BoundOpKind.Greater => NodeKind.Greater,
            BoundOpKind.GreaterEq => NodeKind.GreaterEq,
            BoundOpKind.Less => NodeKind.Less,
            BoundOpKind.LessEq => NodeKind.LessEq,
            BoundOpKind.And => NodeKind.And,
            BoundOpKind.Or => NodeKind.Or,
            _ => throw new ArgumentOutOfRangeException(nameof(OpKind), $"Unknown operator kind: {OpKind}.")
        };

        private static readonly List<BoundBinOperator> _operators =
        [
            new BoundBinOperator(BoundOpKind.Plus, BoundType.Number, (a, b) => (double)a + (double)b),
            new BoundBinOperator(BoundOpKind.Star,  BoundType.Number, (a, b) => (double)a * (double)b),
            new BoundBinOperator(BoundOpKind.Minus, BoundType.Number, (a, b) =>(double) a -(double) b),
            new BoundBinOperator(BoundOpKind.Slash, BoundType.Number, (a, b) =>(double) a /(double) b),

            new BoundBinOperator(BoundOpKind.Plus, BoundType.String, (a, b) => (string)a + (string)b),
            new BoundBinOperator(BoundOpKind.Plus, BoundType.String, BoundType.Number, BoundType.String, (a, b) => (string)a + (double)b),
            new BoundBinOperator(BoundOpKind.Plus, BoundType.Number, BoundType.String, BoundType.String, (a, b) => (double)a + (string)b),

            new BoundBinOperator(BoundOpKind.EqEq, BoundType.Number, BoundType.Number, BoundType.Bool, (a, b) => (double)a == (double)b),
            new BoundBinOperator(BoundOpKind.NotEq, BoundType.Number, BoundType.Number, BoundType.Bool, (a, b) => (double)a != (double)b),
            new BoundBinOperator(BoundOpKind.Greater, BoundType.Number, BoundType.Number, BoundType.Bool, (a, b) => (double)a > (double)b),
            new BoundBinOperator(BoundOpKind.GreaterEq, BoundType.Number, BoundType.Number, BoundType.Bool, (a, b) => (double)a >= (double)b),
            new BoundBinOperator(BoundOpKind.Less, BoundType.Number, BoundType.Number, BoundType.Bool, (a, b) => (double)a < (double)b),
            new BoundBinOperator(BoundOpKind.LessEq, BoundType.Number, BoundType.Number, BoundType.Bool, (a, b) => (double)a <= (double)b),

            new BoundBinOperator(BoundOpKind.EqEq, BoundType.String, BoundType.String, BoundType.Bool, (a, b) => (string)a == (string)b),
            new BoundBinOperator(BoundOpKind.NotEq, BoundType.String, BoundType.String, BoundType.Bool, (a, b) => (string)a != (string)b),

            new BoundBinOperator(BoundOpKind.EqEq, BoundType.Bool, (a, b) => (bool)a == (bool)b),
            new BoundBinOperator(BoundOpKind.NotEq, BoundType.Bool, (a, b) => (bool)a != (bool)b),

            new BoundBinOperator(BoundOpKind.And, BoundType.Bool, (a, b) => (bool)a && (bool)b),
            new BoundBinOperator(BoundOpKind.Or, BoundType.Bool, (a, b) => (bool)a || (bool)b),
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
            BoundOpKind.Not => NodeKind.Not,
            _ => throw new ArgumentOutOfRangeException(nameof(OpKind), $"Unknown operator kind: {OpKind}")
        };

        private static readonly List<BoundUnOperator> _operators =
        [
            new BoundUnOperator(BoundOpKind.Plus, BoundType.Number, BoundType.Number, (a) => a),
            new BoundUnOperator(BoundOpKind.Minus, BoundType.Number, BoundType.Number, (a) => -(double)a),
            new BoundUnOperator(BoundOpKind.Not, BoundType.Bool, BoundType.Bool, (a) => !(bool)a),
        ];

        public static BoundUnOperator? Bind(NodeKind kind, BoundType operand)
            => _operators.Find(op => op.NodeKind == kind && op.Operand == operand);
    }
}