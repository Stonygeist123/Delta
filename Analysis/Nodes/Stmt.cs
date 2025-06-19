namespace Delta.Analysis.Nodes
{
    internal abstract class Stmt : Node
    {
    }

    internal class ExprStmt(Expr expr) : Stmt
    {
        public Expr Expr { get; } = expr;

        public override NodeKind Kind => NodeKind.ExprStmt;
        public override TextSpan Span => Expr.Span;
    }

    internal class VarStmt(Token varToken, Token? mutToken, Token name, Token eqToken, Expr value) : Stmt
    {
        public Token VarToken => varToken;
        public Token? MutToken { get; } = mutToken;
        public Token Name { get; } = name;
        public Token EqToken { get; } = eqToken;
        public Expr Value => value;
        public override NodeKind Kind => NodeKind.VarStmt;
        public override TextSpan Span => new(varToken.Span.Start, value.Span.End);
    }

    internal sealed class BlockStmt(Token lBrace, List<Stmt> stmts, Token rBrace) : Stmt
    {
        public Token LBrace { get; } = lBrace;
        public List<Stmt> Stmts { get; } = stmts;
        public Token RBrace { get; } = rBrace;
        public override NodeKind Kind => NodeKind.BlockStmt;
        public override TextSpan Span => new(LBrace.Span.Start, RBrace.Span.End);
    }

    internal class IfStmt(Token ifToken, Expr? condition, Stmt thenStmt, ElseStmt? elseStmt = null) : Stmt
    {
        public Token IfToken { get; } = ifToken;
        public Expr? Condition { get; } = condition;
        public Stmt ThenStmt { get; } = thenStmt;
        public ElseStmt? ElseClause { get; } = elseStmt;
        public override NodeKind Kind => NodeKind.IfStmt;
        public override TextSpan Span => new(IfToken.Span.Start, ElseClause is null ? ThenStmt.Span.End : ElseClause.Span.End);
    }

    internal class ElseStmt(Token elseToken, Stmt thenStmt) : Stmt
    {
        public Token ElseToken { get; } = elseToken;
        public Stmt ThenStmt { get; } = thenStmt;
        public override NodeKind Kind => NodeKind.ElseStmt;
        public override TextSpan Span => new(ElseToken.Span.Start, ThenStmt.Span.End);
    }

    internal class LoopStmt(Token loopToken, Expr? condition, Stmt thenStmt) : Stmt
    {
        public Token LoopToken { get; } = loopToken;
        public Expr? Condition { get; } = condition;
        public Stmt ThenStmt { get; } = thenStmt;
        public override NodeKind Kind => NodeKind.LoopStmt;
        public override TextSpan Span => new(LoopToken.Span.Start, ThenStmt.Span.End);
    }

    internal class ErrorStmt(params List<Node?> nodes) : Stmt
    {
        public List<Node?> Nodes { get; } = nodes;
        public override NodeKind Kind => NodeKind.ErrorStmt;
        public override TextSpan Span => new(Nodes.First(n => n is not null)!.Span.Start, Nodes.Last(n => n is not null)!.Span.End);
    }
}