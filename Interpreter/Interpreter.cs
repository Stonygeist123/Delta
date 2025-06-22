using Delta.Binding;
using Delta.Binding.BoundNodes;

namespace Delta.Interpreter
{
    internal class Interpreter(Scope _globals)
    {
        private Scope _scope = _globals;

        public void Execute(BoundStmt stmt)
        {
            switch (stmt)
            {
                case BoundExprStmt:
                    ExecuteExpr(((BoundExprStmt)stmt).Expr);
                    break;

                case BoundVarStmt:
                {
                    VarSymbol symbol = ((BoundVarStmt)stmt).Symbol;
                    object? value = ExecuteExpr(((BoundVarStmt)stmt).Value) ?? throw new Exception($"Variable '{symbol.Name}' has no value.");
                    _scope.TryDeclareVar(symbol.Name, value);
                    break;
                }

                case BoundBlockStmt:
                {
                    _scope = new(_scope);
                    foreach (BoundStmt childStmt in ((BoundBlockStmt)stmt).Stmts)
                        Execute(childStmt);
                    _scope = _scope.Parent!;
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

                case BoundFnDecl:
                {
                    FnSymbol symbol = ((BoundFnDecl)stmt).Symbol;
                    _scope.TryDeclareFn(symbol.Name, symbol.Body);
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
                    if (!_scope.TryGetVar(name, out object? value))
                        throw new Exception($"Variable '{name}' is not defined.");
                    if (value is null)
                        throw new Exception($"Variable '{name}' has no value.");
                    return value;
                }

                case BoundAssignExpr:
                {
                    string name = ((BoundAssignExpr)expr).Symbol.Name;
                    object assignValue = ExecuteExpr(((BoundAssignExpr)expr).Value) ?? throw new Exception($"Value to assign to '{name}' has no value.");
                    if (!_scope.TryAssign(name, assignValue))
                        throw new Exception($"Variable '{name}' is not defined.");
                    return assignValue;
                }

                case BoundCallExpr:
                {
                    FnSymbol symbol = ((BoundCallExpr)expr).Symbol;
                    _scope = new(_scope);
                    object? result = null;
                    Execute(symbol.Body);
                    _scope = _scope.Parent!;
                    return result;
                }

                default:
                    throw new Exception($"Unsupported expression: {expr.GetType().Name}.");
            }
        }
    }
}