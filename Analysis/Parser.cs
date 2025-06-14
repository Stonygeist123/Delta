namespace Delta.Analysis
{
    internal class Parser(List<Token> _tokens)
    {
        private int _current = 0;

        public Expr Parse(int parentPrecedence = 0) => CheckExtension(Current.Kind switch
        {
            NodeKind.Number => ParseLiteral(),
            NodeKind.Plus or NodeKind.Minus => ParseUnary(),
            NodeKind.LParen => ParseGrouping(),
            _ => new ErrorExpr(Advance()),
        }, parentPrecedence);

        private Expr ParseLiteral()
        {
            if (Current.Kind != NodeKind.Number)
                return new ErrorExpr(Advance());
            return new LiteralExpr(Advance());
        }

        private Expr ParseUnary()
        {
            if (Current.Kind != NodeKind.Plus && Current.Kind != NodeKind.Minus)
                return new ErrorExpr(Advance());

            Token op = Advance();
            Expr operand = Parse(Utility.GetUnOpPrecedence(op.Kind));
            return new UnaryExpr(op, operand);
        }

        private Expr ParseGrouping()
        {
            Token lParen = Advance();
            if (lParen.Kind != NodeKind.LParen)
                return new ErrorExpr(lParen);

            Expr expr = Parse();
            Token rParen = Advance();
            if (rParen.Kind != NodeKind.RParen)
                return new ErrorExpr(rParen);

            return new GroupingExpr(lParen, expr, rParen);
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

        private bool IsAtEnd() => _current >= _tokens.Count;
    }
}