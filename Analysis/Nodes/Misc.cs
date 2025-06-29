using System.Collections.Immutable;

namespace Delta.Analysis.Nodes
{
    internal abstract class MemberNode(SyntaxTree syntaxTree) : Node(syntaxTree)
    { }

    internal class Arg(SyntaxTree syntaxTree, Token? comma, Expr arg) : Node(syntaxTree)
    {
        public Token? Comma = comma;
        public Expr Expr = arg;
        public override NodeKind Kind => NodeKind.Arg;
        public override TextSpan Span => new((Comma?.Span ?? Expr.Span).Start, Expr.Span.End);
    }

    internal class Param(SyntaxTree syntaxTree, Token? comma, Token name, TypeClause typeClause) : Node(syntaxTree)
    {
        public Token? Comma { get; } = comma;
        public Token Name { get; } = name;
        public TypeClause TypeClause { get; } = typeClause;
        public override NodeKind Kind => NodeKind.Param;
        public override TextSpan Span => new((Comma?.Span ?? Name.Span).Start, TypeClause.Span.End);
    }

    internal class ParameterList(SyntaxTree syntaxTree, Token lParen, List<Param> paramList, Token rParen) : Node(syntaxTree)
    {
        public Token LParen { get; } = lParen;
        public List<Param> ParamList { get; } = paramList;
        public Token RParen { get; } = rParen;
        public override NodeKind Kind => NodeKind.ParameterList;
        public override TextSpan Span => new(LParen.Span.Start, RParen.Span.End);
    }

    internal class TypeClause(SyntaxTree syntaxTree, Token mark, Token type) : Node(syntaxTree)
    {
        public Token Mark { get; } = mark;
        public Token Name { get; } = type;
        public override NodeKind Kind => NodeKind.TypeClause;
        public override TextSpan Span => new(Mark.Span.Start, (Name?.Span ?? Mark.Span).End);
    }

    internal sealed class CompilationUnit(SyntaxTree syntaxTree, ImmutableArray<MemberNode> members, Token eofToken) : Node(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.CompilationUnit;
        public ImmutableArray<MemberNode> Members { get; } = members;
        public Token EofToken { get; } = eofToken;
    }
}