using System.Collections.Immutable;

namespace Delta.Analysis.Nodes
{
    internal sealed class PropertyDecl(SyntaxTree syntaxTree, List<AttributeNode> attributes, Token? mutToken, Token name, TypeClause? typeClause, Token eqToken, Expr value, Token semicolon) : Node(syntaxTree)
    {
        public List<AttributeNode> Attributes { get; } = attributes;
        public Token? MutToken { get; } = mutToken;
        public Token Name { get; } = name;
        public TypeClause? TypeClause { get; } = typeClause;
        public Token EqToken { get; } = eqToken;
        public Expr Value { get; } = value;
        public Token Semicolon { get; } = semicolon;
        public override NodeKind Kind => NodeKind.PropertyDecl;
    }

    internal sealed class MethodDecl(SyntaxTree syntaxTree, List<AttributeNode> attributes, Token keyword, Token name, ParameterList? parameters, TypeClause returnType, Stmt body) : FnDecl(syntaxTree, keyword, name, parameters, returnType, body)
    {
        public List<AttributeNode> Attributes { get; } = attributes;
        public override NodeKind Kind => NodeKind.MethodDecl;
    }

    internal sealed class CtorDecl(SyntaxTree syntaxTree, List<AttributeNode> attributes, Token keyword, ParameterList? parameters, Stmt body) : MemberNode(syntaxTree)
    {
        public List<AttributeNode> Attributes { get; } = attributes;
        public Token Keyword { get; } = keyword;
        public ParameterList? Parameters { get; } = parameters;
        public Stmt Body { get; } = body;
        public override NodeKind Kind => NodeKind.CtorDecl;
    }

    internal class ParameterList(SyntaxTree syntaxTree, Token lParen, List<Param> paramList, Token rParen) : Node(syntaxTree)
    {
        public Token LParen { get; } = lParen;
        public List<Param> ParamList { get; } = paramList;
        public Token RParen { get; } = rParen;
        public override NodeKind Kind => NodeKind.ParameterList;
    }

    internal class Param(SyntaxTree syntaxTree, Token? comma, Token name, TypeClause typeClause) : Node(syntaxTree)
    {
        public Token? Comma { get; } = comma;
        public Token Name { get; } = name;
        public TypeClause TypeClause { get; } = typeClause;
        public override NodeKind Kind => NodeKind.Param;
    }

    internal class Arg(SyntaxTree syntaxTree, Token? comma, Expr arg) : Node(syntaxTree)
    {
        public Token? Comma = comma;
        public Expr Expr = arg;
        public override NodeKind Kind => NodeKind.Arg;
    }

    internal sealed class TypeClause(SyntaxTree syntaxTree, Token mark, Token type) : Node(syntaxTree)
    {
        public Token Mark { get; } = mark;
        public Token Name { get; } = type;
        public override NodeKind Kind => NodeKind.TypeClause;
    }

    internal sealed class AttributeNode(SyntaxTree syntaxTree, Token token) : Node(syntaxTree)
    {
        public Token Token { get; } = token;
        public override NodeKind Kind => NodeKind.AttributeNode;
    }

    internal sealed class CompilationUnit(SyntaxTree syntaxTree, ImmutableArray<MemberNode> members, Token eofToken) : Node(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.CompilationUnit;
        public ImmutableArray<MemberNode> Members { get; } = members;
        public Token EofToken { get; } = eofToken;
    }
}