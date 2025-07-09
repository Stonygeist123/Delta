using System.Collections.Immutable;

namespace Delta.Analysis.Nodes
{
    internal abstract class MemberNode(SyntaxTree syntaxTree) : Node(syntaxTree)
    { }

    internal sealed class FnDecl(SyntaxTree syntaxTree, Token keyword, Token name, ParameterList? parameters, TypeClause returnType, Stmt body) : MemberNode(syntaxTree)
    {
        public Token Keyword { get; } = keyword;
        public Token Name { get; } = name;
        public ParameterList? Parameters { get; } = parameters;
        public TypeClause ReturnType { get; } = returnType;
        public Stmt Body { get; } = body;
        public override NodeKind Kind => NodeKind.FnDecl;
    }

    internal sealed class ClassDecl(SyntaxTree syntaxTree, Token keyword, Token name, Token lBrace, ImmutableArray<PropertyDecl> properties, ImmutableArray<MethodDecl> methods, Token rBrace) : MemberNode(syntaxTree)
    {
        public Token Keyword { get; } = keyword;
        public Token Name { get; } = name;
        public Token LBrace { get; } = lBrace;
        public ImmutableArray<PropertyDecl> Properties { get; } = properties;
        public ImmutableArray<MethodDecl> Methods { get; } = methods;
        public Token RBrace { get; } = rBrace;
        public override NodeKind Kind => NodeKind.ClassDecl;
    }
}