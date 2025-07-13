using Delta.Analysis.Nodes;
using System.Collections.Immutable;

namespace Delta.Analysis
{
    internal static class Utility
    {
        public static int GetBinOpPrecedence(this NodeKind kind) => kind switch
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

        public static int GetUnOpPrecedence(this NodeKind kind) => kind switch
        {
            NodeKind.Plus => 7,
            NodeKind.Minus => 7,
            NodeKind.Not => 7,
            _ => 0
        };

        public static string? GetLexeme(this NodeKind kind) => kind switch
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
            NodeKind.Colon => ":",
            NodeKind.Arrow => "->",
            NodeKind.Semicolon => ";",
            NodeKind.Dot => ".",
            NodeKind.True => "true",
            NodeKind.False => "false",
            NodeKind.Var => "var",
            NodeKind.Mut => "mut",
            NodeKind.If => "if",
            NodeKind.Else => "else",
            NodeKind.Loop => "loop",
            NodeKind.For => "for",
            NodeKind.Fn => "fn",
            NodeKind.Class => "class",
            NodeKind.Ret => "ret",
            NodeKind.Break => "break",
            NodeKind.Continue => "continue",
            NodeKind.Step => "step",
            NodeKind.Pub => "pub",
            NodeKind.Priv => "priv",
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
            { "for", NodeKind.For },
            { "fn", NodeKind.Fn },
            { "class", NodeKind.Class },
            { "ret", NodeKind.Ret },
            { "break", NodeKind.Break },
            { "continue", NodeKind.Continue },
            { "step", NodeKind.Step },
            { "pub", NodeKind.Pub },
            { "priv", NodeKind.Priv },
        };
    }

    internal readonly struct TextSpan(int start, int length)
    {
        public int Start { get; } = start;
        public int Length { get; } = length;
        public int End => Start + Length;

        public static TextSpan From(int start, int end) => new(start, end - start);
    }

    internal sealed class SourceText
    {
        public ImmutableArray<TextLine> Lines { get; }
        private readonly string _text;

        private SourceText(string text, string fileName)
        {
            Lines = ParseLines(this, text);
            _text = text;
            FileName = fileName;
        }

        public static SourceText From(string text, string fileName = "") => new(text, fileName);

        private static ImmutableArray<TextLine> ParseLines(SourceText source, string text)
        {
            ImmutableArray<TextLine>.Builder result = ImmutableArray.CreateBuilder<TextLine>();

            int position = 0;
            int lineStart = 0;
            while (position < text.Length)
            {
                int lineBreakWidth = GetLineBreakWidth(text, position);
                if (lineBreakWidth == 0)
                    ++position;
                else
                {
                    AddLine(result, source, position, lineStart, lineBreakWidth);
                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (position >= lineStart)
                AddLine(result, source, position, lineStart, 0);

            return result.ToImmutable();
        }

        public int GetLineIndex(int position)
        {
            int lower = 0, upper = Lines.Length - 1;
            while (lower <= upper)
            {
                int index = lower + (upper - lower) / 2;
                int start = Lines[index].Start;

                if (start == position)
                    return index;
                else if (start > position)
                    upper = index - 1;
                else
                    lower = index + 1;
            }

            return lower - 1;
        }

        private static void AddLine(ImmutableArray<TextLine>.Builder result, SourceText source, int position, int lineStart, int lineBreakWidth)
            => result.Add(new TextLine(source, lineStart, position - lineStart, position - lineStart + lineBreakWidth));

        private static int GetLineBreakWidth(string text, int i)
        {
            char c = text[i];
            char l = i + 1 >= text.Length ? '\0' : text[i + 1];
            if (c == '\r' && l == '\n')
                return 2;
            else if (c == '\r' || l == '\n')
                return 1;
            return 0;
        }

        public override string ToString() => _text;

        public string ToString(int start, int length) => _text.Substring(start, length);

        public string ToString(TextSpan span) => _text.Substring(span.Start, span.Length);

        public string this[Range range] => _text[range];
        public char this[int position] => _text[position];
        public int Length => _text.Length;
        public string FileName { get; }
    }

    internal sealed class TextLocation(SourceText source, TextSpan span)
    {
        public SourceText Source { get; } = source;
        public string Text => Source[Span.Start..Span.End];
        public string FileName => Source.FileName;
        public TextSpan Span { get; } = span;
        public int StartLine => Source.GetLineIndex(Span.Start);
        public int StartColumn => Span.Start - Source.Lines[StartLine].Start;
        public int EndLine => Source.GetLineIndex(Span.End);
        public int EndColumn => Span.End - Source.Lines[EndLine].Start;
    }

    internal sealed class TextLine(SourceText source, int start, int length, int lengthWithLineBreak)
    {
        public SourceText Source { get; } = source;
        public int Start { get; } = start;
        public int Length { get; } = length;
        public int End => Start + Length;
        public int LengthWithLineBreak { get; } = lengthWithLineBreak;
        public TextSpan Span => new(Start, Length);
        public TextSpan SpanWithLineBreak => new(Start, LengthWithLineBreak);

        public override string ToString() => Source.ToString(Span);
    }
}