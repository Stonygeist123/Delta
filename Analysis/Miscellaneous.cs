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

        public static string? GetLexeme(NodeKind kind) => kind switch
        {
            NodeKind.Plus => "+",
            NodeKind.Minus => "-",
            NodeKind.Slash => "/",
            NodeKind.Star => "*",
            NodeKind.Not => "!",
            NodeKind.EqEq => "==",
            NodeKind.NotEq => "!=",
            NodeKind.Greater => ">",
            NodeKind.GreaterEq => ">=",
            NodeKind.Less => "<",
            NodeKind.LessEq => "<=",
            NodeKind.And => "&&",
            NodeKind.Or => "||",
            NodeKind.LParen => "(",
            NodeKind.RParen => ")",
            NodeKind.LBrace => "{",
            NodeKind.RBrace => "}",
            NodeKind.Eq => "=",
            NodeKind.Comma => ",",
            NodeKind.True => "true",
            NodeKind.False => "false",
            NodeKind.Var => "var",
            NodeKind.Mut => "mut",
            NodeKind.If => "if",
            NodeKind.Else => "else",
            NodeKind.Loop => "loop",
            NodeKind.Fn => "fn",
            _ => null,
        };

        public static readonly Dictionary<string, NodeKind> Keywords = new()
        {
            { "var", NodeKind.Var },
            { "mut", NodeKind.Mut },
            { "true", NodeKind.True },
            { "false", NodeKind.False },
            { "if", NodeKind.If },
            { "else", NodeKind.Else },
            { "loop", NodeKind.Loop },
            { "fn", NodeKind.Fn },
        };
    }

    internal readonly struct TextSpan(int start, int end)
    {
        public int Start { get; } = start;
        public int End { get; } = end;
        public int Length => End - Start;
    }
}