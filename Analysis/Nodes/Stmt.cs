namespace Delta.Analysis.Nodes
{
    internal abstract class Stmt(SyntaxTree syntaxTree) : MemberNode(syntaxTree)
    {
    }

    internal class FnDecl(SyntaxTree syntaxTree, Token fnToken, Token name, ParameterList? parameters, TypeClause returnType, Stmt body) : MemberNode(syntaxTree)
    {
        public Token FnToken { get; } = fnToken;
        public Token Name { get; } = name;
        public ParameterList? Parameters { get; } = parameters;
        public TypeClause ReturnType { get; } = returnType;
        public Stmt Body { get; } = body;
        public override NodeKind Kind => NodeKind.FnDecl;
    }

    internal class ExprStmt(SyntaxTree syntaxTree, Expr expr, Token? semicolon) : Stmt(syntaxTree)
    {
        public Expr Expr { get; } = expr;
        public Token? Semicolon { get; } = semicolon;
        public override NodeKind Kind => NodeKind.ExprStmt;
    }

    internal class VarStmt(SyntaxTree syntaxTree, Token varToken, Token? mutToken, Token name, TypeClause? typeClause, Token eqToken, Expr value, Token? semicolon) : Stmt(syntaxTree)
    {
        public Token VarToken => varToken;
        public Token? MutToken { get; } = mutToken;
        public Token Name { get; } = name;
        public TypeClause? TypeClause { get; } = typeClause;
        public Token EqToken { get; } = eqToken;
        public Token? Semicolon { get; } = semicolon;
        public Expr Value => value;
        public override NodeKind Kind => NodeKind.VarStmt;
    }

    internal sealed class BlockStmt(SyntaxTree syntaxTree, Token lBrace, List<Stmt> stmts, Token rBrace) : Stmt(syntaxTree)
    {
        public Token LBrace { get; } = lBrace;
        public List<Stmt> Stmts { get; } = stmts;
        public Token RBrace { get; } = rBrace;
        public override NodeKind Kind => NodeKind.BlockStmt;
    }

    internal class IfStmt(SyntaxTree syntaxTree, Token ifToken, Expr? condition, Stmt body, ElseStmt? elseBranch = null) : Stmt(syntaxTree)
    {
        public Token IfToken { get; } = ifToken;
        public Expr? Condition { get; } = condition;
        public Stmt Body { get; } = body;
        public ElseStmt? ElseBranch { get; } = elseBranch;
        public override NodeKind Kind => NodeKind.IfStmt;
    }

    internal class ElseStmt(SyntaxTree syntaxTree, Token elseToken, Stmt body) : Stmt(syntaxTree)
    {
        public Token ElseToken { get; } = elseToken;
        public Stmt Body { get; } = body;
        public override NodeKind Kind => NodeKind.ElseStmt;
    }

    internal class LoopStmt(SyntaxTree syntaxTree, Token loopToken, Expr? condition, Stmt bodyStmt) : Stmt(syntaxTree)
    {
        public Token LoopToken { get; } = loopToken;
        public Expr? Condition { get; } = condition;
        public Stmt Body { get; } = bodyStmt;
        public override NodeKind Kind => NodeKind.LoopStmt;
    }

    internal class RetStmt(SyntaxTree syntaxTree, Token retToken, Expr? value, Token semicolon) : Stmt(syntaxTree)
    {
        public Token RetToken { get; } = retToken;
        public Expr? Value { get; } = value;
        public Token Semicolon { get; } = semicolon;
        public override NodeKind Kind => NodeKind.RetStmt;
    }

    internal sealed class BreakStmt(SyntaxTree syntaxTree, Token keyword, Token semicolon) : Stmt(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.BreakStmt;
        public Token Keyword { get; } = keyword;
        public Token Semicolon { get; } = semicolon;
    }

    internal sealed class ContinueStmt(SyntaxTree syntaxTree, Token keyword, Token semicolon) : Stmt(syntaxTree)
    {
        public override NodeKind Kind => NodeKind.ContinueStmt;
        public Token Keyword { get; } = keyword;
        public Token Semicolon { get; } = semicolon;
    }

    internal class ErrorStmt(SyntaxTree syntaxTree, params List<Node?> nodes) : Stmt(syntaxTree)
    {
        public List<Node?> Nodes { get; } = nodes;
        public override NodeKind Kind => NodeKind.ErrorStmt;
    }
}