using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Binding.BoundNodes;
using Delta.Diagnostics;
using Delta.Evaluation;
using Delta.Lowering;
using Delta.Symbols;
using System.Collections.Immutable;
using System.Data;

namespace Delta.Binding
{
    internal class Binder(BoundScope _globalScope, FnSymbol? fnSymbol = null, ClassSymbol? _classSymbol = null)
    {
        private readonly DiagnosticBag _diagnostics = [];
        private BoundScope _scope = _globalScope;
        private readonly FnSymbol? _fn = fnSymbol;
        private readonly ClassSymbol? _class = _classSymbol;
        public DiagnosticBag Diagnostics => _diagnostics;
        private readonly Stack<(LabelSymbol BreakLabel, LabelSymbol ContinueLabel)> _loopStack = new();
        private int _labelCounter = 0;

        public static BoundProgram BindProgram(BoundProgram? previous, BoundGlobalScope globalScope)
        {
            BoundScope parentScope = CreateParentScope(globalScope);
            ImmutableDictionary<FnSymbol, BoundBlockStmt>.Builder fnBodies = ImmutableDictionary.CreateBuilder<FnSymbol, BoundBlockStmt>();
            ImmutableDictionary<ClassSymbol, ClassData>.Builder classes = ImmutableDictionary.CreateBuilder<ClassSymbol, ClassData>();
            DiagnosticBag diagnostics = new();
            BoundGlobalScope scope = globalScope;
            foreach (FnSymbol fn in scope.Functions)
            {
                Binder binder = new(parentScope, fn);
                BoundBlockStmt body = Lowerer.Lower(binder.BindStmt(fn.Decl!.Body));
                if (fn.ReturnType != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(body))
                    binder._diagnostics.Report(fn.Decl.Name.Location, $"All code paths must return a value.");

                fnBodies.Add(fn, body);
                diagnostics.AddRange(binder.Diagnostics);
            }

            foreach (ClassSymbol classSymbol in scope.Classes)
            {
                Binder binder = new(parentScope, null, classSymbol);
                if (classSymbol.Ctor is not null)
                    foreach (ParamSymbol p in classSymbol.Ctor.Parameters)
                        binder._scope.TryDeclareVar(p);
                BoundBlockStmt? ctor = classSymbol.Ctor?.Decl is null ? null : Lowerer.Lower(binder.BindStmt(classSymbol.Ctor.Decl.Body));
                diagnostics.AddRange(binder.Diagnostics);
                ClassData data = new(classSymbol.Name, ctor, classSymbol.Properties, classSymbol.MethodsWithBody!);
                classes.Add(classSymbol, data);
            }

            ImmutableArray<BoundStmt> stmts = globalScope.Stmt.Stmts;
            if (stmts.Length == 1 && stmts.First() is BoundExprStmt es && es.Expr.Type != TypeSymbol.Void)
                stmts = stmts.SetItem(0, new BoundRetStmt(es.Expr));
            fnBodies.Add(globalScope.ScriptFn, new BoundBlockStmt(stmts));
            return new(previous, globalScope.ScriptFn, [.. diagnostics], fnBodies.ToImmutable(), classes.ToImmutable());
        }

        public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, ImmutableArray<SyntaxTree> syntaxTrees)
        {
            BoundScope parentScope = CreateParentScope(previous);
            Binder binder = new(parentScope);
            IEnumerable<FnDecl> fnDecls = syntaxTrees.SelectMany(s => s.Root.Members).OfType<FnDecl>();
            foreach (FnDecl fnDecl in fnDecls)
                binder.BindFnDecl(fnDecl);

            IEnumerable<ClassDecl> classDecls = syntaxTrees.SelectMany(s => s.Root.Members).OfType<ClassDecl>();
            foreach (ClassDecl classDecl in classDecls)
                binder.BindClassDecl(classDecl);

            Stmt?[] firstGlobalStmts = [.. syntaxTrees.Select(st => st.Root.Members.OfType<Stmt>().FirstOrDefault()).Where(g => g is not null)];
            if (firstGlobalStmts.Length > 1)
            {
                foreach (Stmt? globalStmt in firstGlobalStmts)
                    if (globalStmt is not null)
                        binder.Diagnostics.Report(globalStmt.Location, "At most one file can have global statements.");
            }

            IEnumerable<Stmt> stmts = syntaxTrees.SelectMany(s => s.Root.Members).OfType<Stmt>();
            ImmutableArray<BoundStmt> boundStmts = [.. stmts.Select(binder.BindStmt).Select(Lowerer.Lower)];

            FnSymbol scriptFn = new("$eval", TypeSymbol.Void, []);
            ImmutableArray<Diagnostic> diagnostics = [.. binder.Diagnostics];
            if (previous is not null)
                diagnostics.InsertRange(0, previous.Diagnostics);
            return new(previous, scriptFn, new BoundBlockStmt(boundStmts), binder._scope.Variables, binder._scope.Fns, binder._scope.Classes, diagnostics);
        }

        private static BoundScope CreateParentScope(BoundGlobalScope? previous)
        {
            Stack<BoundGlobalScope> stack = new();
            while (previous is not null)
            {
                stack.Push(previous);
                previous = previous.Previous;
            }

            BoundScope parent = GetRootScope();
            while (stack.Count > 0)
            {
                previous = stack.Pop();
                BoundScope scope = new(parent);
                foreach (VarSymbol v in previous.Variables)
                    scope.TryDeclareVar(v);

                foreach (FnSymbol f in previous.Functions)
                    scope.TryDeclareFn(f);

                foreach (ClassSymbol c in previous.Classes)
                    scope.TryDeclareClass(c);
                parent = scope;
            }

            return parent;
        }

        private static BoundScope GetRootScope()
        {
            BoundScope res = new(null);
            foreach (FnSymbol fn in BuiltIn.Fns.Select(f => f.Key))
                res.TryDeclareFn(fn);
            return res;
        }

        private void BindFnDecl(FnDecl decl)
        {
            string name = decl.Name.Lexeme;
            TypeSymbol retType = BindTypeClause(decl.ReturnType, true);
            ImmutableArray<ParamSymbol>.Builder paramListBuilder = ImmutableArray.CreateBuilder<ParamSymbol>();
            if (decl.Parameters is not null)
            {
                HashSet<string> seenParameters = [];
                foreach (Param param in decl.Parameters.ParamList)
                {
                    string pName = param.Name.Lexeme;
                    if (!seenParameters.Add(pName))
                        _diagnostics.Report(param.Name.Location, $"Parameter \"{pName}\" was already declared.");
                    else
                    {
                        TypeSymbol? pType = BindTypeClause(param.TypeClause);
                        if (pType is null)
                            _diagnostics.Report(param.TypeClause.Name.Location, $"Expected type\": <type>\".");
                        else
                            paramListBuilder.Add(new ParamSymbol(pName, pType));
                    }
                }
            }

            ImmutableArray<ParamSymbol> parameters = paramListBuilder.ToImmutableArray();
            Binder binder = new(new(_scope), new(name, retType, parameters, null));
            foreach (ParamSymbol param in parameters)
                binder._scope.TryDeclareVar(param);

            FnSymbol fn = new(name, retType, parameters, decl);
            if (!_scope.TryDeclareFn(fn))
                _diagnostics.Report(decl.Name.Location, $"Function '{name}' is already defined.");
        }

        private void BindClassDecl(ClassDecl decl)
        {
            if (_fn is not null)
            {
                _diagnostics.Report(decl.Keyword.Location, $"Cannot declare class inside of function.");
                return;
            }

            if (_class is not null)
            {
                _diagnostics.Report(decl.Keyword.Location, $"Cannot declare class inside of class.");
                return;
            }

            string name = decl.Name.Lexeme;
            ImmutableArray<ParamSymbol>.Builder ctorParametersBuilder = ImmutableArray.CreateBuilder<ParamSymbol>();
            if (decl.Ctor?.Parameters is not null)
            {
                HashSet<string> seenParameters = [];
                foreach (Param param in decl.Ctor.Parameters.ParamList)
                {
                    string pName = param.Name.Lexeme;
                    if (!seenParameters.Add(pName))
                        _diagnostics.Report(param.Name.Location, $"Parameter \"{pName}\" was already declared.");
                    else
                    {
                        TypeSymbol? pType = BindTypeClause(param.TypeClause);
                        if (pType is null)
                            _diagnostics.Report(param.TypeClause.Name.Location, $"Expected type\": <type>\".");
                        else
                            ctorParametersBuilder.Add(new ParamSymbol(pName, pType));
                    }
                }
            }

            AttributeNode? privAttr = decl.Ctor?.Attributes.FirstOrDefault(a => a.Kind == NodeKind.Priv);
            CtorSymbol? ctor = decl.Ctor is null ? null : new(new(name, privAttr is not null ? Accessibility.Priv : Accessibility.Pub, true), name, ctorParametersBuilder.ToImmutable(), decl.Ctor);
            if (privAttr is not null)
                _diagnostics.Report(privAttr.Location, $"The constructor cannot be private.");
            ImmutableArray<MethodSymbol> methods = [.. decl.Methods.Select(decl =>
                         {
                             Accessibility accessibility = decl.Attributes.Any(a => a.Token.Kind == NodeKind.Pub) ? Accessibility.Pub : Accessibility.Priv;
                             TypeSymbol retType = BindTypeClause(decl.ReturnType, true);
                             return new MethodSymbol(new(decl.Name.Lexeme, accessibility, decl.Attributes.Any(a =>  a.Token.Kind == NodeKind.Static)), decl.Name.Lexeme, retType, decl.Parameters is null ? [] : [.. decl.Parameters.ParamList.Select(p => new ParamSymbol(p.Name.Lexeme, BindTypeClause(p.TypeClause)))]);
                         })];
            ImmutableArray<PropertySymbol> props = [.. decl.Properties.Select(p =>
            {
                Accessibility accessibility = p.Attributes.Any(a => a.Token.Kind == NodeKind.Pub) ? Accessibility.Pub : Accessibility.Priv;
                BoundExpr boundValue = BindExpr(p.Value);
                return new PropertySymbol(new(p.Name.Lexeme, accessibility, p.Attributes.Any(a => a.Token.Kind == NodeKind.Static)), p.Name.Lexeme, p.TypeClause is null ? boundValue.Type : BindTypeClause(p.TypeClause), p.MutToken is not null, boundValue);
            })];

            ClassSymbol classSymbol = new(decl, name, ctor, props, methods.Select(m => new KeyValuePair<MethodSymbol, BoundBlockStmt>(m, new([]))).ToImmutableDictionary());
            Binder binder = new(_scope, null, classSymbol);
            if (decl.Ctor is not null)
            {
                foreach (ParamSymbol p in ctorParametersBuilder)
                    binder._scope.TryDeclareVar(p);
                binder.BindStmt(decl.Ctor.Body);
                _diagnostics.AddRange(binder.Diagnostics);
            }

            ImmutableArray<PropertySymbol>.Builder propertiesBuilder = ImmutableArray.CreateBuilder<PropertySymbol>();
            ImmutableDictionary<MethodSymbol, BoundBlockStmt>.Builder methodsBuilder = ImmutableDictionary.CreateBuilder<MethodSymbol, BoundBlockStmt>();
            foreach (PropertyDecl propDecl in decl.Properties)
            {
                Accessibility accessibility = propDecl.Attributes.Any(a => a.Token.Kind == NodeKind.Pub) ? Accessibility.Pub : Accessibility.Priv;
                BoundExpr boundValue = BindExpr(propDecl.Value);
                string propName = propDecl.Name.Lexeme;
                TypeSymbol type = propDecl.TypeClause is null ? boundValue.Type : BindTypeClause(propDecl.TypeClause);
                if (boundValue.Type != type)
                    _diagnostics.Report(propDecl.Value.Location, $"Cannot assign value of type '{boundValue.Type}' to property '{propName}' of type '{type}'.");

                if (boundValue.Type == TypeSymbol.Void)
                    _diagnostics.Report(propDecl.Value.Location, $"Value assigned to property '{propName}' cannot be of type 'void'.");

                PropertySymbol prop = new(new(propName, accessibility, propDecl.Attributes.Any(a => a.Token.Kind == NodeKind.Static)), propName, type, propDecl.MutToken is not null, BindExpr(propDecl.Value));
                propertiesBuilder.Add(prop);
            }

            foreach (MethodDecl methodDecl in decl.Methods)
            {
                string methodName = methodDecl.Name.Lexeme;
                if (methodsBuilder.Any(m => m.Key.Name == methodName))
                    _diagnostics.Report(methodDecl.Name.Location, $"Method '{methodName}' is already declared.");

                Accessibility accessibility = methodDecl.Attributes.Any(a => a.Token.Kind == NodeKind.Pub) ? Accessibility.Pub : Accessibility.Priv;
                TypeSymbol retType = BindTypeClause(methodDecl.ReturnType, true);
                ImmutableArray<ParamSymbol>.Builder parametersBuilder = ImmutableArray.CreateBuilder<ParamSymbol>();
                if (methodDecl.Parameters is not null)
                {
                    HashSet<string> seenParameters = [];
                    foreach (Param param in methodDecl.Parameters.ParamList)
                    {
                        string pName = param.Name.Lexeme;
                        if (!seenParameters.Add(pName))
                            _diagnostics.Report(param.Name.Location, $"Parameter \"{pName}\" was already declared.");
                        else
                        {
                            TypeSymbol? pType = BindTypeClause(param.TypeClause);
                            if (pType is null)
                                _diagnostics.Report(param.TypeClause.Name.Location, $"Expected type\": <type>\".");
                            else
                                parametersBuilder.Add(new ParamSymbol(pName, pType));
                        }
                    }
                }

                ImmutableArray<ParamSymbol> parameters = parametersBuilder.ToImmutableArray();
                binder = new(new(_scope), new(methodName, retType, parameters, null), classSymbol);
                foreach (ParamSymbol param in parameters)
                    binder._scope.TryDeclareVar(param);
                MethodSymbol method = new(new(methodName, accessibility, methodDecl.Attributes.Any(a => a.Token.Kind == NodeKind.Static)), methodName, retType, parameters, methodDecl);
                BoundBlockStmt body = Lowerer.Lower(binder.BindStmt(methodDecl.Body));
                if (method.ReturnType != TypeSymbol.Void && !ControlFlowGraph.AllPathsReturn(body))
                    _diagnostics.Report(methodDecl.Name.Location, $"All code paths must return a value.");
                methodsBuilder.Add(method, body);
            }

            ClassSymbol symbol = new(decl, name, ctor, propertiesBuilder.ToImmutable(), methodsBuilder.ToImmutable());
            if (!_diagnostics.Any())
                if (!_scope.TryDeclareClass(symbol))
                    _diagnostics.Report(decl.Name.Location, $"Class '{name}' is already declared.");
        }

        public BoundStmt BindStmt(Stmt stmt) => stmt switch
        {
            ExprStmt exprStmt => new BoundExprStmt(BindExpr(exprStmt.Expr)),
            VarStmt => BindVarStmt((VarStmt)stmt),
            BlockStmt => BindBlockStmt((BlockStmt)stmt),
            IfStmt => BindIfStmt((IfStmt)stmt),
            LoopStmt => BindLoopStmt((LoopStmt)stmt),
            ForStmt => BindForStmt((ForStmt)stmt),
            BreakStmt => BindBreakStmt((BreakStmt)stmt),
            ContinueStmt => BindContinueStmt((ContinueStmt)stmt),
            RetStmt => BindRetStmt((RetStmt)stmt),
            _ => throw new NotSupportedException($"Unsupported statement type: {stmt.GetType()}")
        };

        private BoundVarStmt BindVarStmt(VarStmt stmt)
        {
            BoundExpr boundValue = BindExpr(stmt.Value);
            string name = stmt.Name.Lexeme;
            TypeSymbol type = stmt.TypeClause is null ? boundValue.Type : BindTypeClause(stmt.TypeClause);
            VarSymbol symbol = DeclareVar(stmt.Name, type, stmt.MutToken is not null);
            if (boundValue.Type != symbol.Type)
            {
                _diagnostics.Report(stmt.Value.Location, $"Cannot assign value of type '{boundValue.Type}' to variable '{name}' of type '{symbol.Type}'.");
                boundValue = new BoundError();
            }

            if (boundValue.Type == TypeSymbol.Void)
            {
                _diagnostics.Report(stmt.Value.Location, $"Value assigned to '{name}' cannot be of type 'void'.");
                boundValue = new BoundError();
            }

            return new BoundVarStmt(symbol, boundValue);
        }

        private BoundBlockStmt BindBlockStmt(BlockStmt stmt)
        {
            _scope = new BoundScope(_scope);
            ImmutableArray<BoundStmt> stmts = [.. stmt.Stmts.Select(BindStmt)];
            _scope = _scope.Parent!;
            return new BoundBlockStmt(stmts);
        }

        private BoundIfStmt BindIfStmt(IfStmt stmt)
        {
            BoundExpr? condition = stmt.Condition is null ? new BoundLiteralExpr(true, TypeSymbol.Bool) : BindExpr(stmt.Condition);
            if (stmt.Condition is not null && condition.Type != TypeSymbol.Bool)
            {
                _diagnostics.Report(stmt.Condition!.Location, $"Condition must be of type 'bool', but got '{condition.Type}'.");
                condition = new BoundError();
            }

            BoundStmt thenStmt = BindStmt(stmt.Body);
            BoundStmt? elseClause = stmt.ElseBranch is null ? null : BindStmt(stmt.ElseBranch.Body);
            return new BoundIfStmt(condition, thenStmt, elseClause);
        }

        private BoundStmt GetBoundLoopStmt(Stmt stmt, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel)
        {
            bodyLabel = new($"LoopBody_{++_labelCounter}");
            breakLabel = new($"Break_{_labelCounter}");
            continueLabel = new($"Continue_{_labelCounter}");

            _loopStack.Push((breakLabel, continueLabel));
            BoundStmt boundStmt = BindStmt(stmt);
            _loopStack.Pop();
            return boundStmt;
        }

        private BoundLoopStmt BindLoopStmt(LoopStmt stmt)
        {
            BoundExpr? condition = stmt.Condition is null ? new BoundLiteralExpr(true, TypeSymbol.Bool) : BindExpr(stmt.Condition);
            if (stmt.Condition is not null && condition.Type != TypeSymbol.Bool)
            {
                _diagnostics.Report(stmt.Condition.Location, $"Condition must be of type 'bool', but got '{condition.Type}'.");
                condition = new BoundError();
            }

            BoundStmt body = GetBoundLoopStmt(stmt.Body, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            return new BoundLoopStmt(condition, body, bodyLabel, breakLabel, continueLabel);
        }

        private BoundForStmt BindForStmt(ForStmt stmt)
        {
            BoundExpr startValue = BindExpr(stmt.StartValue);
            BoundExpr endValue = BindExpr(stmt.EndValue);
            BoundExpr? stepValue = stmt.StepValue is null ? null : BindExpr(stmt.StepValue);

            if (startValue.Type != TypeSymbol.Number)
            {
                startValue = new BoundError();
                _diagnostics.Report(stmt.StartValue.Location, $"Expected value of type 'number' for start value.");
            }

            if (endValue.Type != TypeSymbol.Number)
            {
                endValue = new BoundError();
                _diagnostics.Report(stmt.EndValue.Location, $"Expected value of type 'number' for end value.");
            }

            if (stepValue is not null && stepValue.Type != TypeSymbol.Number)
            {
                stepValue = new BoundError();
                _diagnostics.Report(stmt.StepValue!.Location, $"Expected value of type 'number' for step value.");
            }

            _scope = new(_scope);
            VarSymbol variable = DeclareVar(stmt.VarName, TypeSymbol.Number, true);
            BoundStmt body = GetBoundLoopStmt(stmt.Body, out LabelSymbol bodyLabel, out LabelSymbol breakLabel, out LabelSymbol continueLabel);
            _scope = _scope.Parent!;
            return new BoundForStmt(variable, startValue, endValue, stepValue, body, bodyLabel, breakLabel, continueLabel);
        }

        private BoundStmt BindBreakStmt(BreakStmt b)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.Report(b.Location, "Can only break inside of a loop.");
                return new BoundErrorStmt();
            }

            return new BoundGotoStmt(_loopStack.Peek().BreakLabel);
        }

        private BoundStmt BindContinueStmt(ContinueStmt c)
        {
            if (_loopStack.Count == 0)
            {
                _diagnostics.Report(c.Location, "Can only continue inside of a loop.");
                return new BoundErrorStmt();
            }

            return new BoundGotoStmt(_loopStack.Peek().ContinueLabel);
        }

        private BoundStmt BindRetStmt(RetStmt retStmt)
        {
            if (_fn is null)
            {
                _diagnostics.Report(retStmt.Keyword.Location, "Return statement can only be used inside of a function.");
                return new BoundErrorStmt();
            }

            BoundExpr? value = retStmt.Value is null ? null : BindExpr(retStmt.Value);
            if (value is null && _fn.ReturnType != TypeSymbol.Void)
            {
                _diagnostics.Report(retStmt.Location, $"Expected return value.");
                value = new BoundError();
            }

            if (value is not null && value.Type != TypeSymbol.Void && _fn.ReturnType == TypeSymbol.Void)
            {
                _diagnostics.Report(retStmt.Value!.Location, $"Cannot return from void {(_class is null ? "function" : "method")} '{_fn.Name}'.");
                value = new BoundError();
            }
            else if (value is not null && value.Type != _fn.ReturnType)
            {
                _diagnostics.Report(retStmt.Value!.Location, $"Cannot return value of type '{value.Type}' from {(_class is null ? "function" : "method")} '{_fn.Name}'. Expected '{_fn.ReturnType}'.");
                value = new BoundError();
            }

            return new BoundRetStmt(value);
        }

        public BoundExpr BindExpr(Expr expr) => expr switch
        {
            LiteralExpr => BindLiteralExpr((LiteralExpr)expr),
            BinaryExpr => BindBinaryExpr((BinaryExpr)expr),
            UnaryExpr => BindUnaryExpr((UnaryExpr)expr),
            GroupingExpr => BindGroupingExpr((GroupingExpr)expr),
            NameExpr => BindNameExpr((NameExpr)expr),
            AssignExpr => BindAssignExpr((AssignExpr)expr),
            GetExpr => BindGetExpr((GetExpr)expr),
            SetExpr => BindSetExpr((SetExpr)expr),
            CallExpr => BindCallExpr((CallExpr)expr),
            MethodExpr => BindMethodExpr((MethodExpr)expr),
            ErrorExpr => new BoundError(),
            _ => throw new NotSupportedException($"Unsupported expression type: {expr.GetType()}")
        };

        private static BoundLiteralExpr BindLiteralExpr(LiteralExpr expr) => expr.Token.Kind switch
        {
            NodeKind.Number => new BoundLiteralExpr(double.Parse(expr.Token.Lexeme), TypeSymbol.Number),
            NodeKind.String => new BoundLiteralExpr(expr.Token.Lexeme[1..^1], TypeSymbol.String),
            NodeKind.True => new BoundLiteralExpr(true, TypeSymbol.Bool),
            NodeKind.False => new BoundLiteralExpr(false, TypeSymbol.Bool),
            _ => throw new Exception($"Unsupported literal type: {expr.Token.Kind}")
        };

        private BoundExpr BindBinaryExpr(BinaryExpr expr)
        {
            BoundExpr left = BindExpr(expr.Left);
            BoundExpr right = BindExpr(expr.Right);
            BoundBinOperator? op = BoundBinOperator.Bind(expr.Op.Kind, left.Type, right.Type);
            if (op is null)
            {
                _diagnostics.Report(expr.Location, $"Invalid binary operator '{expr.Op.Lexeme}' for types '{left.Type}' and '{right.Type}'.");
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
                _diagnostics.Report(expr.Location, $"Invalid unary operator '{expr.Op.Lexeme}' for type '{boundOperand}'.");
                return new BoundError();
            }

            return new BoundUnaryExpr(op, boundOperand);
        }

        private BoundExpr BindGroupingExpr(GroupingExpr expr) => BindExpr(expr.Expression);

        private BoundExpr BindNameExpr(NameExpr expr)
        {
            string name = expr.Name.Lexeme;
            VarSymbol? variable = _class?.Properties.SingleOrDefault(p => p.Name == name);
            if (variable is null && !_scope.TryLookupVar(name, out variable))
            {
                _diagnostics.Report(expr.Name.Location, $"Variable '{name}' is not defined.");
                return new BoundError();
            }

            return new BoundNameExpr(variable);
        }

        private BoundExpr BindAssignExpr(AssignExpr expr)
        {
            string name = expr.Name.Lexeme;
            VarSymbol? variable = _class?.Properties.SingleOrDefault(p => p.Name == name);
            if (variable is null && !_scope.TryLookupVar(name, out variable))
            {
                _diagnostics.Report(expr.Name.Location, $"Variable '{name}' is not defined.");
                return new BoundError();
            }

            BoundExpr value = BindExpr(expr.Value);
            if (!variable.Mutable)
            {
                _diagnostics.Report(expr.Location, $"Cannot reassign to constant '{name}'.");
                return new BoundError();
            }

            if (variable.Type != value.Type)
            {
                _diagnostics.Report(expr.Value.Location, $"Cannot assign value of type '{value.Type}' to variable '{name}' of type '{variable.Type}'.");
                return new BoundError();
            }

            return new BoundAssignExpr(variable, value);
        }

        private BoundExpr BindCallExpr(CallExpr expr)
        {
            string name = expr.Name.Lexeme;
            if (_scope.TryLookupClass(name, out ClassSymbol? classSymbol))
            {
                ImmutableArray<BoundExpr> args = [.. expr.Args.Select(a => BindExpr(a.Expr))];
                if (args.Length != (classSymbol.Ctor?.Parameters.Length ?? 0))
                {
                    _diagnostics.Report(expr.Location, $"Expected '{classSymbol.Ctor?.Parameters.Length ?? 0}' arguments, but got {args.Length}.");
                    return new BoundError();
                }

                if (classSymbol.Ctor is not null)
                    for (int i = 0; i < args.Length; i++)
                        if (args[i].Type != classSymbol.Ctor.Parameters[i].Type)
                            _diagnostics.Report(expr.Args[i].Location, $"Argument '{classSymbol.Ctor.Parameters[i].Name}' of function '{name}' must be of type '{classSymbol.Ctor.Parameters[i].Type}', but got '{args[i].Type}'.");
                return new BoundInstanceExpr(classSymbol, args);
            }
            else
            {
                MethodSymbol? method = null;
                FnSymbol? fn = null;
                if (_class is not null && _class.Methods.Any(m => m.Name == name))
                    method = _class.Methods.Single(m => m.Name == name);

                if (method is null && !_scope.TryLookupFn(name, out fn))
                {
                    _diagnostics.Report(expr.Name.Location, $"Function{(_class is not null ? " or method" : "")} '{name}' is not defined.");
                    return new BoundError();
                }

                ImmutableArray<BoundExpr> args = [.. expr.Args.Select(a => BindExpr(a.Expr))];
                if (args.Length != (method?.Parameters ?? fn!.Parameters).Length)
                {
                    _diagnostics.Report(expr.Location, $"Expected '{(method?.Parameters ?? fn!.Parameters).Length}' arguments, but got {args.Length}.");
                    return new BoundError();
                }

                for (int i = 0; i < args.Length; i++)
                {
                    ParamSymbol p = (method?.Parameters ?? fn!.Parameters)[i];
                    if (args[i].Type != p.Type)
                        _diagnostics.Report(expr.Args[i].Location, $"Argument '{p.Name}' of {(method is not null ? "method" : "function")} '{name}' must be of type '{p.Type}', but got '{args[i].Type}'.");
                }

                return new BoundCallExpr(method ?? fn!, args);
            }
        }

        private BoundExpr BindGetExpr(GetExpr expr)
        {
            BoundExpr? instance;
            if (expr.Instance is NameExpr n && _scope.TryLookupClass(n.Name.Lexeme, out ClassSymbol? classSymbol))
                instance = null;
            else
            {
                instance = BindExpr(expr.Instance);
                classSymbol = instance.Type.ClassSymbol;
            }

            if (classSymbol is null)
            {
                _diagnostics.Report(expr.Instance.Location, $"Either class instance or class name is needed.");
                return new BoundError();
            }

            string propName = expr.PropName.Lexeme;
            PropertySymbol? prop = classSymbol.Properties.SingleOrDefault(p => p.Name == propName);
            if (prop is null)
            {
                _diagnostics.Report(expr.PropName.Location, $"Class '{classSymbol.Name}' does not have property '{propName}'.");
                return new BoundError();
            }

            if (instance is null && !prop.Attributes.IsStatic)
            {
                _diagnostics.Report(expr.Location, $"Cannot access non-static property '{propName}' without instance.");
                return new BoundError();
            }

            if (_class != classSymbol && prop.Attributes.Accessibility == Accessibility.Priv)
                _diagnostics.Report(expr.Location, $"Cannot access private property '{propName}' of class '{classSymbol.Name}' in this context.");
            return instance is null ? new BoundStaticGetExpr(classSymbol, prop) : new BoundGetExpr(instance, prop);
        }

        private BoundExpr BindSetExpr(SetExpr expr)
        {
            BoundExpr? instance;
            if (expr.Instance is NameExpr n && _scope.TryLookupClass(n.Name.Lexeme, out ClassSymbol? classSymbol))
                instance = null;
            else
            {
                instance = BindExpr(expr.Instance);
                classSymbol = instance.Type.ClassSymbol;
            }

            if (classSymbol is null)
            {
                _diagnostics.Report(expr.Instance.Location, $"Either class instance or class name is needed.");
                return new BoundError();
            }

            string propName = expr.PropName.Lexeme;
            PropertySymbol? prop = classSymbol.Properties.SingleOrDefault(p => p.Name == propName);
            if (prop is null)
            {
                _diagnostics.Report(expr.PropName.Location, $"Class '{classSymbol.Name}' does not have property '{propName}'.");
                return new BoundError();
            }

            if (instance is null && !prop.Attributes.IsStatic)
            {
                _diagnostics.Report(expr.Location, $"Cannot access non-static property '{propName}' without instance.");
                return new BoundError();
            }

            if (_class != classSymbol && prop.Attributes.Accessibility == Accessibility.Priv)
                _diagnostics.Report(expr.Location, $"Cannot access private property '{propName}' of class '{classSymbol.Name}' in this context.");

            BoundExpr value = BindExpr(expr.Value);
            if (!prop.Mutable)
            {
                _diagnostics.Report(expr.Location, $"Cannot reassign to constant property '{propName}'.");
                return new BoundError();
            }

            if (prop.Type != value.Type)
            {
                _diagnostics.Report(expr.Value.Location, $"Cannot assign value of type '{value.Type}' to property '{propName}' of type '{prop.Type}'.");
                return new BoundError();
            }

            return instance is null ? new BoundStaticSetExpr(classSymbol, prop, value) : new BoundSetExpr(instance, prop, value);
        }

        private BoundExpr BindMethodExpr(MethodExpr expr)
        {
            BoundExpr? instance;
            if (expr.Instance is NameExpr n && _scope.TryLookupClass(n.Name.Lexeme, out ClassSymbol? classSymbol))
                instance = null;
            else
            {
                instance = BindExpr(expr.Instance);
                classSymbol = instance.Type.ClassSymbol;
            }

            if (classSymbol is null)
            {
                _diagnostics.Report(expr.Instance.Location, $"Either class instance or class name is needed.");
                return new BoundError();
            }

            string methodName = expr.PropName.Lexeme;
            MethodSymbol? method = classSymbol!.Methods.SingleOrDefault(m => m.Name == methodName);
            if (method is null)
            {
                _diagnostics.Report(expr.PropName.Location, $"Class '{classSymbol.Name}' does not have method '{methodName}'.");
                return new BoundError();
            }

            if (instance is null && !method.Attributes.IsStatic)
            {
                _diagnostics.Report(expr.Location, $"Cannot access non-static method '{methodName}' without instance.");
                return new BoundError();
            }

            if (_class != classSymbol && method.Attributes.Accessibility == Accessibility.Priv)
                _diagnostics.Report(expr.Location, $"Cannot access private property '{methodName}' of class '{classSymbol.Name}' in this context.");

            ImmutableArray<BoundExpr> args = [.. expr.Args.Select(a => BindExpr(a.Expr))];
            if (args.Length != method.Parameters.Length)
            {
                _diagnostics.Report(expr.Location, $"Expected '{method.Parameters.Length}' arguments, but got {args.Length}.");
                return new BoundError();
            }

            for (int i = 0; i < args.Length; i++)
                if (args[i].Type != method.Parameters[i].Type)
                    _diagnostics.Report(expr.Args[i].Location, $"Argument '{method.Parameters[i].Name}' of method '{methodName}' must be of type '{method.Parameters[i].Type}', but got '{args[i].Type}'.");
            return instance is null ? new BoundStaticMethodExpr(classSymbol, method, args) : new BoundMethodExpr(instance, method, args);
        }

        private TypeSymbol BindTypeClause(TypeClause typeClause, bool canBeVoid = false)
        {
            TypeSymbol? type = TypeSymbol.Bind(typeClause.Name!.Lexeme);
            if (type is null)
            {
                _diagnostics.Report(typeClause.Name.Location, $"Unknown type '{typeClause.Name.Lexeme}'.");
                return TypeSymbol.Error;
            }

            if (type == TypeSymbol.Void && !canBeVoid)
            {
                _diagnostics.Report(typeClause.Name.Location, "Type 'void' is not allowed here.");
                return TypeSymbol.Error;
            }

            return type;
        }

        private VarSymbol DeclareVar(Token name, TypeSymbol type, bool isMut = false)
        {
            VarSymbol variable = _fn is null ? new GlobalVarSymbol(name.Lexeme, type, isMut) : new LocalVarSymbol(name.Lexeme, type, isMut);
            if (!_scope.TryDeclareVar(variable))
                _diagnostics.Report(name.Location, $"\"{name.Lexeme}\" already exists.");
            return variable;
        }
    }
}