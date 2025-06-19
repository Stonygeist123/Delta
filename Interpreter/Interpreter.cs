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

                case BoundVarStmt:
                    string name = ((BoundVarStmt)stmt).Name;
                    if (_symbolTable.ContainsKey(name))
                        throw new Exception($"Variable '{name}' already exists.");

                    object? value = ExecuteExpr(((BoundVarStmt)stmt).Value) ?? throw new Exception($"Variable '{name}' has no value.");
                    _symbolTable.Add(name, value);
                    break;

                case BoundBlockStmt:
                {
                    foreach (BoundStmt childStmt in ((BoundBlockStmt)stmt).Stmts)
                        Execute(childStmt);
                    break;
                }

                case BoundIfStmt:
                {
                    object? conditionValue = ExecuteExpr(((BoundIfStmt)stmt).Condition) ?? throw new Exception($"Condition has no value.");
                    BoundStmt? elseClause = ((BoundIfStmt)stmt).ElseClause;
                    if (conditionValue is bool condition && condition)
                        Execute(((BoundIfStmt)stmt).ThenStmt);
                    else if (elseClause is not null)
                        Execute(elseClause);
                    break;
                }

                case BoundLoopStmt:
                {
                    while (true)
                    {
                        object? conditionValue = ExecuteExpr(((BoundLoopStmt)stmt).Condition);
                        if (conditionValue is not bool condition || !condition)
                            break;
                        Execute(((BoundLoopStmt)stmt).ThenStmt);
                    }
                    break;
                }

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
                    return left is null || right is null ? null : ((BoundBinaryExpr)expr).Op.Execute(left, right);

                case BoundUnaryExpr:
                    object? operand = ExecuteExpr(((BoundUnaryExpr)expr).Operand);
                    return operand is null ? null : ((BoundUnaryExpr)expr).Op.Execute(operand);

                case BoundGroupingExpr:
                    return ExecuteExpr(((BoundGroupingExpr)expr).Expr);

                case BoundNameExpr:
                {
                    string name = ((BoundNameExpr)expr).Symbol.Name;
                    if (!_symbolTable.TryGetValue(name, out object? value))
                        throw new Exception($"Variable '{name}' is not defined.");
                    if (value is null)
                        throw new Exception($"Variable '{name}' has no value.");
                    return value;
                }

                case BoundAssignExpr:
                {
                    string name = ((BoundAssignExpr)expr).Symbol.Name;
                    if (!_symbolTable.ContainsKey(name))
                        throw new Exception($"Variable '{name}' is not defined.");

                    object? assignValue = ExecuteExpr(((BoundAssignExpr)expr).Value) ?? throw new Exception($"Variable '{name}' has no value.");
                    _symbolTable[name] = assignValue;
                    return assignValue;
                }

                default:
                    return null;
            }
        }
    }
}