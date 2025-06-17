using Delta.Analysis.Nodes;

namespace Delta.Analysis
{
    internal static class Utility
    {
        public static int GetBinOpPrecedence(NodeKind kind) => kind switch
        {
            NodeKind.Star => 6,
            NodeKind.Slash => 6,
            NodeKind.Plus => 5,
            NodeKind.Minus => 5,
            NodeKind.Greater => 4,
            NodeKind.GreaterEq => 4,
            NodeKind.Less => 4,
            NodeKind.LessEq => 4,
            NodeKind.EqEq => 3,
            NodeKind.NotEq => 3,
            NodeKind.And => 2,
            NodeKind.Or => 1,
            _ => 0
        };

        public static int GetUnOpPrecedence(NodeKind kind) => kind switch
        {
            NodeKind.Plus => 7,
            NodeKind.Minus => 7,
            NodeKind.Not => 7,
            _ => 0
        };
    }

    internal readonly struct TextSpan(int start, int end)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
        public int Length => End - Start;
    }
}