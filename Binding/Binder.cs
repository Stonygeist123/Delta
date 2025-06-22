using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Binding.BoundNodes;
using Delta.Diagnostics;

namespace Delta.Binding
{
    internal class Binder(string _src, BoundScope _globalScope)
    {
        private readonly DiagnosticBag _diagnostics = [];
        private BoundScope _scope = _globalScope;
        public DiagnosticBag Diagnostics => _diagnostics;

        public List<BoundStmt> Bind(List<Stmt> stmts) => stmts.Select(BindStmt).ToList();

        public BoundStmt BindStmt(Stmt stmt) => stmt switch
        {
            ExprStmt exprStmt => new BoundExprStmt(BindExpr(exprStmt.Expr)),
            VarStmt => BindVarStmt((VarStmt)stmt),
            BlockStmt => BindBlockStmt((BlockStmt)stmt),
            IfStmt => BindIfStmt((IfStmt)stmt),
            LoopStmt => BindLoopStmt((LoopStmt)stmt),
            FnDecl => BindFnDecl((FnDecl)stmt),
            _ => throw new NotSupportedException($"Unsupported statement type: {stmt.GetType()}")
        };

        private BoundVarStmt BindVarStmt(VarStmt stmt)
        {
            BoundExpr boundValue = BindExpr(stmt.Value);
            string name = stmt.Name.Lexeme;
            if (!_scope.TryDeclareVar(name, boundValue.Type, stmt.MutToken is not null, out VarSymbol? symbol))
            {
                _diagnostics.Add(_src, $"Variable '{name}' is already defined.", stmt.Name.Span);
                return new BoundVarStmt(new(name, boundValue.Type, stmt.MutToken is not null), boundValue);
            }

            if (boundValue.Type != symbol.Type)
            {
                _diagnostics.Add(_src, $"Cannot assign value of type '{boundValue.Type}' to variable '{name}' of type '{symbol.Type}'.", stmt.Value.Span);
                boundValue = new BoundError();
            }

            if (boundValue.Type == BoundType.Void)
            {
                _diagnostics.Add(_src, $"Variable '{name}' cannot be of type 'void'.", stmt.Value.Span);
                boundValue = new BoundError();
            }

            return new BoundVarStmt(symbol, boundValue);
        }

        private BoundBlockStmt BindBlockStmt(BlockStmt stmt)
        {
            _scope = new BoundScope(_scope);
            List<BoundStmt> boundStmts = stmt.Stmts.Select(BindStmt).ToList();
            _scope = _scope.Parent!;
            return new BoundBlockStmt(boundStmts);
        }

        private BoundIfStmt BindIfStmt(IfStmt stmt)
        {
            BoundExpr? condition = stmt.Condition is null ? new BoundLiteralExpr(true, BoundType.Bool) : BindExpr(stmt.Condition);
            if (stmt.Condition is not null && condition.Type != BoundType.Bool)
            {
                _diagnostics.Add(_src, $"Condition must be of type 'bool', but got '{condition.Type}'.", stmt.Condition!.Span);
                condition = new BoundError();
            }

            BoundStmt thenStmt = BindStmt(stmt.ThenStmt);
            BoundStmt? elseClause = stmt.ElseClause is null ? null : BindStmt(stmt.ElseClause.ThenStmt);
            return new BoundIfStmt(condition, thenStmt, elseClause);
        }

        private BoundLoopStmt BindLoopStmt(LoopStmt stmt)
        {
            BoundExpr? condition = stmt.Condition is null ? new BoundLiteralExpr(true, BoundType.Bool) : BindExpr(stmt.Condition);
            if (stmt.Condition is not null && condition.Type != BoundType.Bool)
            {
                _diagnostics.Add(_src, $"Condition must be of type 'bool', but got '{condition.Type}'.", stmt.Condition!.Span);
                condition = new BoundError();
            }

            BoundStmt thenStmt = BindStmt(stmt.ThenStmt);
            return new BoundLoopStmt(condition, thenStmt);
        }

        private BoundFnDecl BindFnDecl(FnDecl decl)
        {
            string name = decl.Name.Lexeme;
            List<ParamSymbol> paramList = decl.Parameters?.ParamList
                .Select(param => new ParamSymbol(param.Name.Lexeme))
                .ToList() ?? [];
            _scope = new BoundScope(_scope);
            paramList.ForEach(param => _scope.TryDeclareVar(param.Name, param.Type, param.Mutable, out _));
            BoundBlockStmt body = BindBlockStmt(decl.Body);
            _scope = _scope.Parent!;

            if (!_scope.TryDeclareFn(name, BoundType.Void, body, paramList, out FnSymbol? symbol))
                _diagnostics.Add(_src, $"Function '{name}' is already defined.", decl.Name.Span);
            return new BoundFnDecl(symbol ?? new(name, BoundType.Void, paramList, body));
        }

        public BoundExpr BindExpr(Expr expr) => expr switch
        {
            LiteralExpr => BindLiteralExpr((LiteralExpr)expr),
            BinaryExpr => BindBinaryExpr((BinaryExpr)expr),
            UnaryExpr => BindUnaryExpr((UnaryExpr)expr),
            GroupingExpr => BindGroupingExpr((GroupingExpr)expr),
            NameExpr => BindNameExpr((NameExpr)expr),
            AssignExpr => BindAssignExpr((AssignExpr)expr),
            CallExpr => BindCallExpr((CallExpr)expr),
            _ => throw new NotSupportedException($"Unsupported expression type: {expr.GetType()}")
        };

        private static BoundLiteralExpr BindLiteralExpr(LiteralExpr expr) => expr.Token.Kind switch
        {
            NodeKind.Number => new BoundLiteralExpr(double.Parse(expr.Token.Lexeme), BoundType.Number),
            NodeKind.String => new BoundLiteralExpr(expr.Token.Lexeme[1..^1], BoundType.String),
            NodeKind.True => new BoundLiteralExpr(true, BoundType.Bool),
            NodeKind.False => new BoundLiteralExpr(false, BoundType.Bool),
            _ => throw new Exception($"Unsupported literal type: {expr.Token.Kind}")
        };

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
            if (_scope.TryGetVar(name, out VarSymbol? variable))
                return new BoundNameExpr(variable);

            _diagnostics.Add(_src, $"Variable '{name}' is not defined.", expr.Name.Span);
            return new BoundError();
        }

        private BoundExpr BindAssignExpr(AssignExpr expr)
        {
            string name = expr.Name.Lexeme;
            if (!_scope.TryGetVar(name, out VarSymbol? variable))
            {
                _diagnostics.Add(_src, $"Variable '{name}' is not defined.", expr.Name.Span);
                return new BoundError();
            }

            BoundExpr value = BindExpr(expr.Value);
            if (variable.Type != value.Type)
            {
                _diagnostics.Add(_src, $"Cannot assign value of type '{variable.Type}' to variable '{name}' of type '{value.Type}'.", expr.Value.Span);
                return new BoundError();
            }

            if (!variable.Mutable)
            {
                _diagnostics.Add(_src, $"Cannot reassign to constant '{name}'.", new TextSpan(expr.EqToken.Span.Start, expr.Value.Span.End));
                return new BoundError();
            }

            return new BoundAssignExpr(variable, value);
        }

        private BoundExpr BindCallExpr(CallExpr expr)
        {
            string name = expr.Name.Lexeme;
            if (!_scope.TryGetFn(name, out FnSymbol? fn))
            {
                _diagnostics.Add(_src, $"Function '{name}' is not defined.", expr.Name.Span);
                return new BoundError();
            }

            List<BoundExpr> args = expr.Args.Select(a => BindExpr(a.Expr)).ToList();
            if (args.Count != fn.ParamList.Count)
            {
                _diagnostics.Add(_src, $"Function '{name}' expects {fn.ParamList.Count} arguments, but got {args.Count}.", expr.Span);
                return new BoundError();
            }

            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].Type != fn.ParamList[i].Type)
                {
                    _diagnostics.Add(_src, $"Argument {i + 1} of function '{name}' must be of type '{fn.ParamList[i].Type}', but got '{args[i].Type}'.", expr.Args[i].Span);
                    args[i] = new BoundError();
                }
            }

            return new BoundCallExpr(fn, args);
        }
    }
}