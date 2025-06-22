using Delta.Analysis.Nodes;
using Delta.Diagnostics;

namespace Delta.Analysis
{
    internal class Parser
    {
        private readonly string _src;
        private int _current = 0;
        private readonly DiagnosticBag _diagnostics = [];
        private readonly List<Token> _tokens = [];
        public DiagnosticBag Diagnostics => _diagnostics;

        public Parser(string src)
        {
            _src = src;
            Lexer lexer = new(_src);
            _tokens = lexer.Lex();
            _diagnostics.AddAll(lexer.Diagnostics);
        }

        public List<Stmt> Parse()
        {
            List<Stmt> stmts = [];
            while (!IsAtEnd())
                stmts.Add(ParseStmt());
            return stmts;
        }

        public Stmt ParseStmt()
        {
            switch (Current.Kind)
            {
                case NodeKind.Var:
                {
                    Token varToken = Advance();
                    Token? mutToken = null;
                    if (Current.Kind == NodeKind.Mut)
                        mutToken = Advance();

                    Token name = Current;
                    if (!Match(NodeKind.Identifier))
                        return new ErrorStmt(varToken, name);

                    Token eqToken = Current;
                    if (!Match(NodeKind.Eq))
                        return new ErrorStmt(varToken, name, eqToken);

                    Expr value = ParseExpr();
                    return new VarStmt(varToken, mutToken, name, eqToken, value);
                }

                case NodeKind.LBrace:
                    return ParseBlockStmt();

                case NodeKind.If:
                {
                    Token ifToken = Advance();
                    Expr? condition = Current.Kind == NodeKind.LBrace ? null : ParseExpr();
                    Stmt thenStmt = ParseStmt();

                    if (Current.Kind == NodeKind.Else)
                    {
                        Token elseToken = Advance();
                        Stmt elseClause = ParseStmt();
                        return new IfStmt(ifToken, condition, thenStmt, new ElseStmt(elseToken, elseClause));
                    }

                    return new IfStmt(ifToken, condition, thenStmt);
                }

                case NodeKind.Loop:
                {
                    Token ifToken = Advance();
                    Expr? condition = Current.Kind == NodeKind.LBrace ? null : ParseExpr();
                    Stmt thenStmt = ParseStmt();
                    return new LoopStmt(ifToken, condition, thenStmt);
                }

                case NodeKind.Fn:
                {
                    Token fnToken = Advance();
                    Token name = Current;
                    if (!Match(NodeKind.Identifier))
                        return new ErrorStmt(fnToken, name);
                    ParameterList? parameters = null;
                    Token lParen = Current;
                    if (lParen.Kind == NodeKind.LParen)
                    {
                        ++_current;
                        List<Param> paramList = [];
                        while (Current.Kind != NodeKind.RParen && !IsAtEnd())
                        {
                            Token? comma = null;
                            if (paramList.Count > 0)
                            {
                                comma = Advance();
                                if (comma.Kind != NodeKind.Comma)
                                    _diagnostics.Add(_src, "Expected ',' between parameters.", comma.Span);
                            }

                            Token paramName = Current;
                            Param param = new(comma, paramName);
                            if (!Match(NodeKind.Identifier))
                                return new ErrorStmt([fnToken, name, lParen, .. paramList, param]);
                            paramList.Add(param);
                        }

                        Token rParen = Current;
                        parameters = new ParameterList(lParen, paramList, rParen);
                        if (!Match(NodeKind.RParen))
                            return new ErrorStmt([fnToken, name, parameters]);
                    }

                    Stmt body = ParseBlockStmt();
                    return body is BlockStmt block
                        ? new FnDecl(fnToken, name, parameters, block)
                        : new ErrorStmt(fnToken, name, body);
                }

                default:
                    Expr expr = ParseExpr();
                    return new ExprStmt(expr);
            }
        }

        private Stmt ParseBlockStmt()
        {
            Token lBrace = Current;
            if (!Match(NodeKind.LBrace))
                return new ErrorStmt(lBrace);

            List<Stmt> stmts = [];
            while (Current.Kind != NodeKind.RBrace && !IsAtEnd())
                stmts.Add(ParseStmt());

            Token rBrace = Current;
            if (!Match(NodeKind.RBrace))
                return new ErrorStmt([lBrace, .. stmts, rBrace]);
            return new BlockStmt(lBrace, stmts, rBrace);
        }

        public Expr ParseExpr(int parentPrecedence = 0)
        {
            Expr expr;
            if (IsAtEnd())
            {
                _diagnostics.Add(_src, "Expected expression.", Current.Span);
                return new ErrorExpr(_tokens.Last());
            }

            Token firstToken = Advance();
            switch (firstToken.Kind)
            {
                case NodeKind.Number:
                case NodeKind.String:
                case NodeKind.True:
                case NodeKind.False:
                    expr = new LiteralExpr(firstToken);
                    break;

                case NodeKind.Identifier:
                    if (Current.Kind == NodeKind.Eq)
                    {
                        Token eqToken = Advance();
                        Expr value = ParseExpr();
                        expr = new AssignExpr(firstToken, eqToken, value);
                    }
                    else
                        expr = new NameExpr(firstToken);
                    break;

                case NodeKind.Plus or NodeKind.Minus:
                    Expr operand = ParseExpr(Utility.GetUnOpPrecedence(firstToken.Kind));
                    expr = new UnaryExpr(firstToken, operand);
                    break;

                case NodeKind.LParen:
                    expr = ParseExpr();
                    Token rParen = Advance();
                    if (rParen.Kind != NodeKind.RParen)
                    {
                        _diagnostics.Add(_src, "Expected ')'.", rParen.Span);
                        expr = new ErrorExpr(firstToken, expr, rParen);
                    }

                    expr = new GroupingExpr(firstToken, expr, rParen);
                    break;

                default:
                    _diagnostics.Add(_src, "Expected expression.", firstToken.Span);
                    expr = new ErrorExpr(firstToken);
                    break;
            };

            return CheckExtension(expr, parentPrecedence);
        }

        private Expr CheckExtension(Expr expr, int parentPrecedence = 0)
        {
            if (IsAtEnd())
                return expr;

            Token token = Current;
            if (Utility.GetBinOpPrecedence(token.Kind) > 0)
            {
                int precedence = Utility.GetBinOpPrecedence(Current.Kind);
                while (precedence > parentPrecedence && !IsAtEnd())
                {
                    ++_current;
                    expr = new BinaryExpr(expr, token, ParseExpr(precedence));
                    if (!IsAtEnd())
                    {
                        token = Current;
                        precedence = Utility.GetBinOpPrecedence(token.Kind);
                    }
                }
            }
            else if (expr is NameExpr n && token.Kind == NodeKind.LParen)
            {
                ++_current;
                List<Arg> args = [];
                while (!IsAtEnd() && Current.Kind != NodeKind.RParen)
                {
                    if (args.Count == 0)
                        args.Add(new(null, ParseExpr()));
                    else
                    {
                        Token comma = Advance();
                        if (comma.Kind != NodeKind.Comma)
                            _diagnostics.Add(_src, "Expected ',' between arguments.", comma.Span);
                        args.Add(new(comma, ParseExpr()));
                    }
                }

                if (IsAtEnd())
                {
                    _diagnostics.Add(_src, "Expected ')'.", Current.Span);
                    return new ErrorExpr([n, token, .. args]);
                }

                Token rParen = Current;
                if (!Match(NodeKind.RParen))
                    return new ErrorExpr(n, rParen);
                expr = new CallExpr(n.Name, token, args, rParen);
            }

            return expr;
        }

        private Token Current => _tokens[_current];

        private Token Advance() => _tokens[_current++];

        private bool Match(NodeKind kind)
        {
            Token cur = Advance();
            if (cur.Kind == kind)
                return true;
            else
            {
                _diagnostics.Add(_src, $"Expected '{Utility.GetLexeme(kind)}' but found '{Utility.GetLexeme(cur.Kind)}'.", cur.Span);
                return false;
            }
        }

        private bool IsAtEnd() => _current >= _tokens.Count || Current.Kind == NodeKind.EOF;
    }
}