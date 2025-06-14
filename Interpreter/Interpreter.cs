using Delta.Analysis;
using Delta.Analysis.Nodes;

namespace Delta.Interpreter
{
    internal class Interpreter()
    {
        private readonly Dictionary<string, object?> _symbolTable = [];

        public void Execute(Stmt stmt)
        {
            switch (stmt)
            {
                case ExprStmt exprStmt:
                    ExecuteExpr(exprStmt.Expr);
                    break;

                case VarStmt varStmt:
                    string name = varStmt.Name.Lexeme;
                    if (_symbolTable.ContainsKey(name))
                        throw new Exception($"Variable '{name}' already exists.");

                    object? value = ExecuteExpr(varStmt.Value) ?? throw new Exception($"Variable '{name}' has no value.");
                    _symbolTable.Add(name, value);
                    break;

                default:
                    throw new Exception($"Unsupported statement.");
            }
        }

        public object? ExecuteExpr(Expr expr)
        {
            switch (expr)
            {
                case LiteralExpr literalExpr:
                    return double.Parse(literalExpr.Token.Lexeme);

                case BinaryExpr binaryExpr:
                    object? left = ExecuteExpr(binaryExpr.Left);
                    object? right = ExecuteExpr(binaryExpr.Right);
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
                    object? operand = ExecuteExpr(unaryExpr.Operand);
                    if (operand is null)
                        return null;
                    return unaryExpr.Op.Kind switch
                    {
                        NodeKind.Minus => -(double)operand,
                        NodeKind.Plus => operand,
                        _ => throw new InvalidOperationException($"Unsupported operator: {unaryExpr.Op.Lexeme}")
                    };

                case GroupingExpr groupingExpr:
                    return ExecuteExpr(groupingExpr.Expression);

                case NameExpr nameExpr:
                    string name = nameExpr.Name.Lexeme;
                    if (!_symbolTable.TryGetValue(name, out object? value))
                        throw new Exception($"Variable '{name}' is not defined.");
                    if (value is null)
                        throw new Exception($"Variable '{name}' has no value.");
                    return value;

                default:
                    return null;
            }
        }
    }
}