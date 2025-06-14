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
        }

        public Expr Parse(int parentPrecedence = 0)
        {
            Expr expr;
            if (IsAtEnd())
            {
                _diagnostics.Add(_src, "Unexpected expr.", Current.Span);
                return new ErrorExpr(_tokens.Last());
            }

            Token token = Advance();
            switch (token.Kind)
            {
                case NodeKind.Number:
                    if (token.Kind != NodeKind.Number)
                    {
                        _diagnostics.Add(_src, "Expected a literal.", token.Span);
                        expr = new ErrorExpr(token);
                        break;
                    }

                    expr = new LiteralExpr(token);
                    break;

                case NodeKind.Plus or NodeKind.Minus:
                    if (token.Kind != NodeKind.Plus && token.Kind != NodeKind.Minus)
                    {
                        _diagnostics.Add(_src, "Expected an unary operator.", token.Span);
                        expr = new ErrorExpr(token);
                        break;
                    }

                    Expr operand = Parse(Utility.GetUnOpPrecedence(token.Kind));
                    expr = new UnaryExpr(token, operand);
                    break;

                case NodeKind.LParen:
                    Token lParen = Advance();
                    if (lParen.Kind != NodeKind.LParen)
                    {
                        _diagnostics.Add(_src, "Expected '('.", lParen.Span);
                        expr = new ErrorExpr(lParen);
                        break;
                    }

                    expr = Parse();
                    Token rParen = Advance();
                    if (rParen.Kind != NodeKind.RParen)
                    {
                        _diagnostics.Add(_src, "Expected ')'.", lParen.Span);
                        expr = new ErrorExpr(lParen, expr, rParen);
                    }

                    expr = new GroupingExpr(lParen, expr, rParen);
                    break;

                default:
                    _diagnostics.Add(_src, "Unexpected expr.", Current.Span);
                    expr = new ErrorExpr(token);
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
                        precedence = Utility.GetBinOpPrecedence(Current.Kind);
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