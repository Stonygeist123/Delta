using Delta.Analysis;
using Delta.Binding;
using Delta.Binding.BoundNodes;
using Delta.Symbols;

namespace Delta.Evaluation
{
    internal class Interpreter
    {
        private readonly Stack<Dictionary<VarSymbol, object?>> _locals = new();
        private readonly Dictionary<FnSymbol, BoundBlockStmt> _fns = [];
        private readonly Dictionary<ClassSymbol, ClassData> _classes = [];
        public BoundProgram _program;
        public Dictionary<VarSymbol, object?> _globals;
        private object? _lastValue = null;
        private ClassInstance? _instance = null;

        public Interpreter(BoundProgram program, Dictionary<VarSymbol, object?> variables)
        {
            _program = program;
            _globals = variables;
            _locals.Push([]);
            BoundProgram? current = program;
            KeyValuePair<FnSymbol, BoundBlockStmt>? evalFn = current.Functions.Any(fn => fn.Key.Name == "$eval") ? current.Functions.Single(fn => fn.Key.Name == "$eval") : null;
            if (evalFn.HasValue)
                _fns.Add(evalFn.Value.Key, evalFn.Value.Value);

            while (current is not null)
            {
                foreach ((FnSymbol fn, BoundBlockStmt body) in current.Functions.Where(fn => fn.Key.Name != "$eval"))
                    _fns.TryAdd(fn, body);
                foreach ((ClassSymbol symbol, ClassData data) in current.Classes)
                    _classes.TryAdd(symbol, data);
                current = current.Previous;
            }
        }

        public object? Execute()
        {
            FnSymbol? fn = _program.ScriptFn;
            return fn is null ? null : ExecuteStmt(_fns[fn]);
        }

        public object? ExecuteStmt(BoundBlockStmt stmt)
        {
            Dictionary<LabelSymbol, int> labelToIndex = [];
            for (int i = 0; i < stmt.Stmts.Length; ++i)
                if (stmt.Stmts[i] is BoundLabelStmt l)
                    labelToIndex.Add(l.Label, i + 1);

            int index = 0;
            while (index < stmt.Stmts.Length)
            {
                BoundStmt s = stmt.Stmts[index];
                switch (s)
                {
                    case BoundLabelStmt:
                    {
                        ++index;
                        break;
                    }
                    case BoundGotoStmt g:
                    {
                        index = labelToIndex[g.Label];
                        break;
                    }
                    case BoundCondGotoStmt cg:
                    {
                        bool condition = (bool)ExecuteExpr(cg.Condition)!;
                        if (condition == cg.JumpIfTrue)
                            index = labelToIndex[cg.Label];
                        else
                            ++index;
                        break;
                    }

                    case BoundExprStmt:
                        ExecuteExpr(((BoundExprStmt)s).Expr);
                        ++index;
                        break;

                    case BoundVarStmt:
                    {
                        VarSymbol variable = ((BoundVarStmt)s).Variable;
                        object? value = ExecuteExpr(((BoundVarStmt)s).Value) ?? throw new Exception($"Variable '{variable.Name}' has no value.");
                        if ((variable is GlobalVarSymbol
                                ? _globals
                                : _locals.Peek()).Any(v => v.Key.Name == variable.Name))
                        {
                            foreach (KeyValuePair<VarSymbol, object?> kv in variable is GlobalVarSymbol
                               ? _globals
                               : _locals.Peek())
                                if (kv.Key.Name == variable.Name)
                                    (variable is GlobalVarSymbol
                                  ? _globals
                                  : _locals.Peek()).Remove(kv.Key);
                            (variable is GlobalVarSymbol
                              ? _globals
                              : _locals.Peek()).TryAdd(variable, value);
                        }
                        else
                            (variable is GlobalVarSymbol
                                ? _globals
                                : _locals.Peek()).TryAdd(variable, value);

                        _locals.Peek().Add(variable, value);
                        ++index;
                        break;
                    }

                    case BoundBlockStmt:
                    {
                        _lastValue = ExecuteStmt((BoundBlockStmt)s);
                        ++index;
                        break;
                    }

                    case BoundRetStmt:
                    {
                        BoundExpr? returnValue = ((BoundRetStmt)s).Value;
                        return returnValue is null ? null : ExecuteExpr(returnValue);
                    }

                    default:
                        throw new Exception($"Unsupported statement.");
                }
            }

            return _lastValue;
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
                    VarSymbol symbol = ((BoundNameExpr)expr).Variable;
                    string name = symbol.Name;
                    if (symbol is PropertySymbol p)
                        return _instance!.Properties[p];
                    return (symbol is GlobalVarSymbol ? _globals : _locals.Peek()).Single(v => v.Key.Name == name).Value;
                }

                case BoundAssignExpr:
                {
                    VarSymbol symbol = ((BoundAssignExpr)expr).Variable;
                    string name = symbol.Name;
                    object value = ExecuteExpr(((BoundAssignExpr)expr).Value) ?? throw new Exception($"Value to assign to variable '{name}' has no value.");
                    if (symbol is PropertySymbol p)
                        return _instance!.Properties[p] = value;
                    return (symbol is GlobalVarSymbol
                            ? _globals
                            : _locals.Peek())[symbol] = value;
                }

                case BoundGetExpr:
                {
                    BoundExpr instanceExpr = ((BoundGetExpr)expr).Instance;
                    ClassSymbol classSymbol = ((BoundGetExpr)expr).ClassSymbol;
                    PropertySymbol property = ((BoundGetExpr)expr).Property;
                    ClassInstance instance = (ClassInstance)ExecuteExpr(instanceExpr)!;
                    return instance.Properties[property];
                }

                case BoundSetExpr:
                {
                    BoundExpr instanceExpr = ((BoundSetExpr)expr).Instance;
                    ClassSymbol classSymbol = ((BoundSetExpr)expr).ClassSymbol;
                    PropertySymbol property = ((BoundSetExpr)expr).Property;
                    ClassInstance instance = (ClassInstance)ExecuteExpr(instanceExpr)!;
                    object value = ExecuteExpr(((BoundSetExpr)expr).Value) ?? throw new Exception($"Value to assign to property '{property.Name}' has no value.");
                    return instance.Properties[property] = value;
                }

                case BoundCallExpr:
                {
                    FnSymbol fn = ((BoundCallExpr)expr).Fn;
                    object? result = null;
                    List<object?> args = [.. ((BoundCallExpr)expr).Args.Select(ExecuteExpr)];
                    if (fn is BuiltInFn)
                        result = BuiltIn.Fns.Single(f => f.Key == fn).Value(args);
                    else if (fn is MethodSymbol m)
                    {
                        Dictionary<VarSymbol, object?> locals = [];
                        for (int i = 0; i < args.Count; i++)
                        {
                            ParamSymbol parameter = fn.Parameters[i];
                            locals.Add(parameter, args[i]);
                        }

                        _locals.Push(locals);
                        result = ExecuteStmt(_instance!.Data.Methods[m]);
                    }
                    else
                    {
                        Dictionary<VarSymbol, object?> locals = [];
                        for (int i = 0; i < args.Count; i++)
                        {
                            ParamSymbol parameter = fn.Parameters[i];
                            locals.Add(parameter, args[i]);
                        }

                        _locals.Push(locals);
                        result = ExecuteStmt(_fns[fn]);
                    }

                    return result;
                }

                case BoundMethodExpr:
                {
                    ClassInstance instance = (ClassInstance)ExecuteExpr(((BoundMethodExpr)expr).Instance)!;
                    MethodSymbol methodSymbol = ((BoundMethodExpr)expr).Method;
                    object? result = null;
                    List<object?> args = [.. ((BoundMethodExpr)expr).Args.Select(ExecuteExpr)];
                    Dictionary<VarSymbol, object?> locals = [];
                    for (int i = 0; i < args.Count; i++)
                    {
                        ParamSymbol parameter = methodSymbol.Parameters[i];
                        locals.Add(parameter, args[i]);
                    }

                    _locals.Push(locals);
                    _instance = instance;
                    result = ExecuteStmt(instance.Data.Methods[methodSymbol]);
                    _instance = null;
                    return result;
                }

                case BoundInstanceExpr:
                {
                    ClassSymbol classSymbol = ((BoundInstanceExpr)expr).ClassSymbol;
                    ClassData data = _classes[classSymbol];
                    if (classSymbol.Ctor is not null)
                    {
                        List<object?> args = [.. ((BoundInstanceExpr)expr).Args.Select(ExecuteExpr)];
                        Dictionary<VarSymbol, object?> locals = [];
                        for (int i = 0; i < args.Count; i++)
                        {
                            ParamSymbol parameter = classSymbol.Ctor.Parameters[i];
                            locals.Add(parameter, args[i]);
                        }

                        _locals.Push(locals);
                    }

                    ClassInstance instance = new(data, data.Properties.Select(p => new KeyValuePair<PropertySymbol, object?>(p, ExecuteExpr(p.Value))).ToDictionary());
                    _instance = instance;
                    if (data.Ctor is not null)
                        ExecuteStmt(data.Ctor);
                    _instance = null;
                    return instance;
                }

                default:
                    throw new Exception($"Unsupported expression: {expr.GetType().Name}.");
            }
        }
    }

    internal class RuntimeException(string title, string message, TextLocation location) : Exception(message)
    {
        public string Title { get; } = title;
        public TextLocation Location { get; } = location;

        public void Print() => Console.Out.WriteLine($"{Location.FileName}:{Location.StartLine}{Location.Span.Start}\n{Title}: {Message}");
    }
}