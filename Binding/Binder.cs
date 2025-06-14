using Delta.Analysis.Nodes;
using Delta.Binding.BoundNodes;
using Delta.Diagnostics;

namespace Delta.Binding
{
    internal class Binder(string _src)
    {
        private readonly List<string> _symbolTable = [];
        public List<string> SymbolTable => _symbolTable;
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
            if (_symbolTable.Contains(stmt.Name.Lexeme))
            {
                _diagnostics.Add(_src, $"Variable '{stmt.Name.Lexeme}' is already defined.", stmt.Name.Span);
                return new BoundVarStmt(stmt.Name.Lexeme, boundValue);
            }

            return new BoundVarStmt(stmt.Name.Lexeme, boundValue);
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
                _ => throw new NotSupportedException($"Unsupported expression type: {expr.GetType()}")
            };
        }

        private BoundBinaryExpr BindBinaryExpr(BinaryExpr binaryExpr)
        {
            BoundExpr left = BindExpr(binaryExpr.Left);
            BoundExpr right = BindExpr(binaryExpr.Right);
            return new BoundBinaryExpr(
                                left,
                                binaryExpr.Op.Kind,
                                right);
        }

        private BoundExpr BindLiteralExpr(LiteralExpr expr)
        {
            if (expr.Token.Kind == Analysis.NodeKind.Number)
                return new BoundLiteralExpr(double.Parse(expr.Token.Lexeme));
            throw new Exception($"Unsupported literal type: {expr.Token.Kind}");
        }

        private BoundExpr BindUnaryExpr(UnaryExpr expr)
        {
            BoundExpr boundOperand = BindExpr(expr.Operand);
            return new BoundUnaryExpr(expr.Op.Kind, boundOperand);
        }

        private BoundExpr BindGroupingExpr(GroupingExpr expr) => BindExpr(expr.Expression);

        private BoundExpr BindNameExpr(NameExpr expr)
        {
            string name = expr.Name.Lexeme;
            if (_symbolTable.Contains(name))
                return new BoundVariableExpr(name);
            _diagnostics.Add(_src, $"Variable '{name}' is not defined.", expr.Name.Span);
            return new BoundErrorExpr();
        }
    }
}