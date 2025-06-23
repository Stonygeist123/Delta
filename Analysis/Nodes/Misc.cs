namespace Delta.Analysis.Nodes
{
    internal class Arg(Token? comma, Expr arg) : Node
    {
        public Token? Comma = comma;
        public Expr Expr = arg;
        public override NodeKind Kind => NodeKind.Arg;
        public override TextSpan Span => new((Comma?.Span ?? Expr.Span).Start, Expr.Span.End);
    }

    internal class Param(Token? comma, Token name, TypeClause typeClause) : Node
    {
        public Token? Comma { get; } = comma;
        public Token Name { get; } = name;
        public TypeClause TypeClause { get; } = typeClause;
        public override NodeKind Kind => NodeKind.Param;
        public override TextSpan Span => new((Comma?.Span ?? Name.Span).Start, TypeClause.Span.End);
    }

    internal class ParameterList(Token lParen, List<Param> paramList, Token rParen) : Node
    {
        public Token LParen { get; } = lParen;
        public List<Param> ParamList { get; } = paramList;
        public Token RParen { get; } = rParen;
        public override NodeKind Kind => NodeKind.ParameterList;
        public override TextSpan Span => new(LParen.Span.Start, RParen.Span.End);
    }

    internal class TypeClause(Token mark, Token? type) : Node
    {
        public Token Mark { get; } = mark;
        public Token? Type { get; } = type;
        public override NodeKind Kind => NodeKind.TypeClause;
        public override TextSpan Span => new(Mark.Span.Start, (Type?.Span ?? Mark.Span).End);
    }
}