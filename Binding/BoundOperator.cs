using Delta.Analysis.Nodes;
using Delta.Symbols;

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

    internal class BoundBinOperator(BoundOpKind op, TypeSymbol left, TypeSymbol right, TypeSymbol result, Func<object, object, object> execute)
    {
        public BoundBinOperator(BoundOpKind op, TypeSymbol type, Func<object, object, object> execute) : this(op, type, type, type, execute)
        {
        }

        public BoundOpKind OpKind { get; } = op;

        public TypeSymbol Left { get; } = left;
        public TypeSymbol Right { get; } = right;
        public TypeSymbol Result { get; } = result;
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
            new BoundBinOperator(BoundOpKind.Plus, TypeSymbol.Number, (a, b) => (double)a + (double)b),
            new BoundBinOperator(BoundOpKind.Star,  TypeSymbol.Number, (a, b) => (double)a * (double)b),
            new BoundBinOperator(BoundOpKind.Minus, TypeSymbol.Number, (a, b) =>(double) a -(double) b),
            new BoundBinOperator(BoundOpKind.Slash, TypeSymbol.Number, (a, b) =>(double) a /(double) b),

            new BoundBinOperator(BoundOpKind.Plus, TypeSymbol.String, (a, b) => (string)a + (string)b),
            new BoundBinOperator(BoundOpKind.Plus, TypeSymbol.String, TypeSymbol.Number, TypeSymbol.String, (a, b) => (string)a + (double)b),
            new BoundBinOperator(BoundOpKind.Plus, TypeSymbol.Number, TypeSymbol.String, TypeSymbol.String, (a, b) => (double)a + (string)b),

            new BoundBinOperator(BoundOpKind.EqEq, TypeSymbol.Number, TypeSymbol.Number, TypeSymbol.Bool, (a, b) => (double)a == (double)b),
            new BoundBinOperator(BoundOpKind.NotEq, TypeSymbol.Number, TypeSymbol.Number, TypeSymbol.Bool, (a, b) => (double)a != (double)b),
            new BoundBinOperator(BoundOpKind.Greater, TypeSymbol.Number, TypeSymbol.Number, TypeSymbol.Bool, (a, b) => (double)a > (double)b),
            new BoundBinOperator(BoundOpKind.GreaterEq, TypeSymbol.Number, TypeSymbol.Number, TypeSymbol.Bool, (a, b) => (double)a >= (double)b),
            new BoundBinOperator(BoundOpKind.Less, TypeSymbol.Number, TypeSymbol.Number, TypeSymbol.Bool, (a, b) => (double)a < (double)b),
            new BoundBinOperator(BoundOpKind.LessEq, TypeSymbol.Number, TypeSymbol.Number, TypeSymbol.Bool, (a, b) => (double)a <= (double)b),

            new BoundBinOperator(BoundOpKind.EqEq, TypeSymbol.String, TypeSymbol.String, TypeSymbol.Bool, (a, b) => (string)a == (string)b),
            new BoundBinOperator(BoundOpKind.NotEq, TypeSymbol.String, TypeSymbol.String, TypeSymbol.Bool, (a, b) => (string)a != (string)b),

            new BoundBinOperator(BoundOpKind.EqEq, TypeSymbol.Bool, (a, b) => (bool)a == (bool)b),
            new BoundBinOperator(BoundOpKind.NotEq, TypeSymbol.Bool, (a, b) => (bool)a != (bool)b),

            new BoundBinOperator(BoundOpKind.And, TypeSymbol.Bool, (a, b) => (bool)a && (bool)b),
            new BoundBinOperator(BoundOpKind.Or, TypeSymbol.Bool, (a, b) => (bool)a || (bool)b),
        ];

        public static BoundBinOperator? Bind(NodeKind kind, TypeSymbol left, TypeSymbol right)
            => _operators.Find(op => op.NodeKind == kind && op.Left == left && op.Right == right);
    }

    internal class BoundUnOperator(BoundOpKind op, TypeSymbol operand, TypeSymbol result, Func<object, object> execute)
    {
        public BoundOpKind OpKind { get; } = op;
        public TypeSymbol Operand { get; } = operand;
        public TypeSymbol Result { get; } = result;
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
            new BoundUnOperator(BoundOpKind.Plus, TypeSymbol.Number, TypeSymbol.Number, (a) => a),
            new BoundUnOperator(BoundOpKind.Minus, TypeSymbol.Number, TypeSymbol.Number, (a) => -(double)a),
            new BoundUnOperator(BoundOpKind.Not, TypeSymbol.Bool, TypeSymbol.Bool, (a) => !(bool)a),
        ];

        public static BoundUnOperator? Bind(NodeKind kind, TypeSymbol operand)
            => _operators.Find(op => op.NodeKind == kind && op.Operand == operand);
    }
}