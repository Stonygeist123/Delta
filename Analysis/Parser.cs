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
            {
                switch (Current.Kind)
                {
                    case NodeKind.Var:
                        Token varToken = Advance();
                        Token? mutToken = null;
                        if (Current.Kind == NodeKind.Mut)
                            mutToken = Advance();

                        Token name = Advance();
                        if (name.Kind != NodeKind.Identifier)
                        {
                            _diagnostics.Add(_src, "Expected an identifier.", name.Span);
                            stmts.Add(new ErrorStmt(varToken, name));
                            break;
                        }

                        Token eq = Advance();
                        if (eq.Kind != NodeKind.Eq)
                        {
                            _diagnostics.Add(_src, "Expected '='.", eq.Span);
                            stmts.Add(new ErrorStmt(varToken, name, eq));
                            break;
                        }

                        Expr value = ParseExpr();
                        stmts.Add(new VarStmt(varToken, mutToken, name, eq, value));
                        break;

                    default:
                        Expr expr = ParseExpr();
                        stmts.Add(new ExprStmt(expr));
                        break;
                }
            }

            return stmts;
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

                return expr;
            }

            return expr;
        }

        private Token Current => _tokens[_current];

        private Token Advance() => _tokens[_current++];

        private bool IsAtEnd() => _current >= _tokens.Count || Current.Kind == NodeKind.EOF;
    }
}