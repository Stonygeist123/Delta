using Delta.Binding.BoundNodes;

namespace Delta.Interpreter
{
    internal class Interpreter()
    {
        private readonly Dictionary<string, object?> _symbolTable = [];

        public void Execute(BoundStmt stmt)
        {
            switch (stmt)
            {
                case BoundExprStmt:
                    ExecuteExpr(((BoundExprStmt)stmt).Expr);
                    break;

                case BoundVarStmt varStmt:
                    string name = varStmt.Name;
                    if (_symbolTable.ContainsKey(name))
                        throw new Exception($"Variable '{name}' already exists.");

                    object? value = ExecuteExpr(varStmt.Value) ?? throw new Exception($"Variable '{name}' has no value.");
                    _symbolTable.Add(name, value);
                    break;

                default:
                    throw new Exception($"Unsupported statement.");
            }
        }

        public object? ExecuteExpr(BoundExpr expr)
        {
            switch (expr)
            {
                case BoundLiteralExpr literalExpr:
                    return literalExpr.Value;

                case BoundBinaryExpr:
                    object? left = ExecuteExpr(((BoundBinaryExpr)expr).Left);
                    object? right = ExecuteExpr(((BoundBinaryExpr)expr).Right);
                    if (left is null || right is null)
                        return null;

                    return ((BoundBinaryExpr)expr).Op.Execute(left, right);

                case BoundUnaryExpr:
                    object? operand = ExecuteExpr(((BoundUnaryExpr)expr).Operand);
                    if (operand is null)
                        return null;

                    return ((BoundUnaryExpr)expr).Op.Execute(operand);

                case BoundGroupingExpr:
                    return ExecuteExpr(((BoundGroupingExpr)expr).Expr);

                case BoundNameExpr:
                    string name = ((BoundNameExpr)expr).Name;
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