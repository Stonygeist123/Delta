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

        public Expr Parse(int parentPrecedence = 0)
        {
            Expr expr;
            if (IsAtEnd())
            {
                _diagnostics.Add(_src, "Unexpected expr.", Current.Span);
                return new ErrorExpr(_tokens.Last());
            }

            Token firstToken = Advance();
            switch (firstToken.Kind)
            {
                case NodeKind.Number:
                    if (firstToken.Kind != NodeKind.Number)
                    {
                        _diagnostics.Add(_src, "Expected a literal.", firstToken.Span);
                        expr = new ErrorExpr(firstToken);
                        break;
                    }

                    expr = new LiteralExpr(firstToken);
                    break;

                case NodeKind.Plus or NodeKind.Minus:
                    if (firstToken.Kind != NodeKind.Plus && firstToken.Kind != NodeKind.Minus)
                    {
                        _diagnostics.Add(_src, "Expected an unary operator.", firstToken.Span);
                        expr = new ErrorExpr(firstToken);
                        break;
                    }

                    Expr operand = Parse(Utility.GetUnOpPrecedence(firstToken.Kind));
                    expr = new UnaryExpr(firstToken, operand);
                    break;

                case NodeKind.LParen:
                    if (firstToken.Kind != NodeKind.LParen)
                    {
                        _diagnostics.Add(_src, "Expected '('.", firstToken.Span);
                        expr = new ErrorExpr(firstToken);
                        break;
                    }

                    expr = Parse();
                    Token rParen = Advance();
                    if (rParen.Kind != NodeKind.RParen)
                    {
                        _diagnostics.Add(_src, "Expected ')'.", rParen.Span);
                        expr = new ErrorExpr(firstToken, expr, rParen);
                    }

                    expr = new GroupingExpr(firstToken, expr, rParen);
                    break;

                default:
                    _diagnostics.Add(_src, "Unexpected expr.", firstToken.Span);
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
                    expr = new BinaryExpr(expr, token, Parse(precedence));
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