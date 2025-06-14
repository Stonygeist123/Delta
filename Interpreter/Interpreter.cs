using Delta.Analysis;

namespace Delta.Interpreter
{
    internal class Interpreter()
    {
        public static object? Execute(Expr expr)
        {
            switch (expr)
            {
                case LiteralExpr literalExpr:
                    return double.Parse(literalExpr.Token.Lexeme);

                case BinaryExpr binaryExpr:
                    object? left = Execute(binaryExpr.Left);
                    object? right = Execute(binaryExpr.Right);
                    if (left is null || right is null)
                        return null;

                    return binaryExpr.Op.Kind switch
                    {
                        NodeKind.Plus => (double)left + (double)right,
                        NodeKind.Minus => (double)left - (double)right,
                        NodeKind.Star => (double)left * (double)right,
                        NodeKind.Slash => (double)left / (double)right,
                        _ => throw new InvalidOperationException($"Unsupported operator: {binaryExpr.Op.Lexeme}")
                    };

                case UnaryExpr unaryExpr:
                    object? operand = Execute(unaryExpr.Operand);
                    if (operand is null)
                        return null;
                    return unaryExpr.Op.Kind switch
                    {
                        NodeKind.Minus => -(double)operand,
                        NodeKind.Plus => operand,
                        _ => throw new InvalidOperationException($"Unsupported operator: {unaryExpr.Op.Lexeme}")
                    };

                case GroupingExpr groupingExpr:
                    return Execute(groupingExpr.Expression);

                default:
                    return null;
            }
        }
    }
}