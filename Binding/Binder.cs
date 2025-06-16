using Delta.Analysis.Nodes;
using Delta.Binding.BoundNodes;
using Delta.Diagnostics;

namespace Delta.Binding
{
    internal class Binder(string _src)
    {
        private readonly Dictionary<string, BoundVarSymbol> _symbolTable = [];
        private readonly DiagnosticBag _diagnostics = [];
        public DiagnosticBag Diagnostics => _diagnostics;

        public List<BoundStmt> Bind(List<Stmt> stmts) => stmts.Select(BindStmt).ToList();

        public BoundStmt BindStmt(Stmt stmt)
        {
            return stmt switch
            {
                ExprStmt exprStmt => new BoundExprStmt(BindExpr(exprStmt.Expr)),
                VarStmt => BindVarStmt((VarStmt)stmt),
                _ => throw new NotSupportedException($"Unsupported statement type: {stmt.GetType()}")
            };
        }

        private BoundVarStmt BindVarStmt(VarStmt stmt)
        {
            BoundExpr boundValue = BindExpr(stmt.Value);
            string name = stmt.Name.Lexeme;
            if (_symbolTable.ContainsKey(name))
            {
                _diagnostics.Add(_src, $"Variable '{name}' is already defined.", stmt.Name.Span);
                return new BoundVarStmt(name, boundValue);
            }

            _symbolTable[name] = new(name, boundValue.Type, stmt.MutToken is not null);
            return new BoundVarStmt(name, boundValue);
        }

        public BoundExpr BindExpr(Expr expr)
        {
            return expr switch
            {
                LiteralExpr => BindLiteralExpr((LiteralExpr)expr),
                BinaryExpr => BindBinaryExpr((BinaryExpr)expr),
                UnaryExpr => BindUnaryExpr((UnaryExpr)expr),
                GroupingExpr => BindGroupingExpr((GroupingExpr)expr),
                NameExpr => BindNameExpr((NameExpr)expr),
                AssignExpr => BindAssignExpr((AssignExpr)expr),
                _ => throw new NotSupportedException($"Unsupported expression type: {expr.GetType()}")
            };
        }

        private static BoundLiteralExpr BindLiteralExpr(LiteralExpr expr)
        {
            if (expr.Token.Kind == NodeKind.Number)
                return new BoundLiteralExpr(double.Parse(expr.Token.Lexeme), BoundType.Number);
            else if (expr.Token.Kind == NodeKind.String)
                return new BoundLiteralExpr(expr.Token.Lexeme[1..^1], BoundType.String);
            throw new Exception($"Unsupported literal type: {expr.Token.Kind}");
        }

        private BoundExpr BindBinaryExpr(BinaryExpr expr)
        {
            BoundExpr left = BindExpr(expr.Left);
            BoundExpr right = BindExpr(expr.Right);
            BoundBinOperator? op = BoundBinOperator.Bind(expr.Op.Kind, left.Type, right.Type);
            if (op is null)
            {
                _diagnostics.Add(_src, $"Invalid binary operator '{expr.Op.Lexeme}' for types '{left.Type}' and '{right.Type}'.", expr.Span);
                return new BoundError();
            }

            return new BoundBinaryExpr(
                                left,
                                op,
                                right);
        }

        private BoundExpr BindUnaryExpr(UnaryExpr expr)
        {
            BoundExpr boundOperand = BindExpr(expr.Operand);
            BoundUnOperator? op = BoundUnOperator.Bind(expr.Op.Kind, boundOperand.Type);
            if (op is null)
            {
                _diagnostics.Add(_src, $"Invalid unary operator '{expr.Op.Lexeme}' for type '{boundOperand}'.", expr.Span);
                return new BoundError();
            }

            return new BoundUnaryExpr(op, boundOperand);
        }

        private BoundExpr BindGroupingExpr(GroupingExpr expr) => BindExpr(expr.Expression);

        private BoundExpr BindNameExpr(NameExpr expr)
        {
            string name = expr.Name.Lexeme;
            if (_symbolTable.TryGetValue(name, out BoundVarSymbol? symbol))
                return new BoundNameExpr(symbol);
            _diagnostics.Add(_src, $"Variable '{name}' is not defined.", expr.Name.Span);
            return new BoundError();
        }

        private BoundExpr BindAssignExpr(AssignExpr expr)
        {
            string name = expr.Name.Lexeme;
            if (!_symbolTable.TryGetValue(name, out BoundVarSymbol? symbol))
            {
                _diagnostics.Add(_src, $"Variable '{name}' is not defined.", expr.Name.Span);
                return new BoundError();
            }

            BoundExpr value = BindExpr(expr.Value);
            if (symbol.Type != value.Type)
            {
                _diagnostics.Add(_src, $"Cannot assign value of type '{symbol.Type}' to variable '{name}' of type '{value}'.", expr.Value.Span);
                return new BoundError();
            }

            if (!symbol.Mutable)
            {
                _diagnostics.Add(_src, $"Cannot re-assign constant '{name}'.", expr.Value.Span);
                return new BoundError();
            }

            return new BoundAssignExpr(symbol, value);
        }
    }
}