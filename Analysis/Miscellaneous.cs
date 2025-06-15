using Delta.Analysis.Nodes;

namespace Delta.Analysis
{
    internal static class Utility
    {
        public static int GetBinOpPrecedence(NodeKind kind) => kind switch
        {
            NodeKind.Plus => 1,
            NodeKind.Minus => 1,
            NodeKind.Star => 2,
            NodeKind.Slash => 2,
            _ => 0
        };

        public static int GetUnOpPrecedence(NodeKind kind) => kind switch
        {
            NodeKind.Plus => 3,
            NodeKind.Minus => 3,
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