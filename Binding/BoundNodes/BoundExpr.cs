using Delta.Symbols;
using System.Collections.Immutable;

namespace Delta.Binding.BoundNodes
{
    internal abstract class BoundExpr : BoundNode
    {
        public abstract TypeSymbol Type { get; }
    }

    internal sealed class BoundLiteralExpr(object value, TypeSymbol type) : BoundExpr
    {
        public object Value { get; } = value;
        public override TypeSymbol Type { get; } = type;
    }

    internal sealed class BoundBinaryExpr(BoundExpr left, BoundBinOperator op, BoundExpr right) : BoundExpr
    {
        public BoundExpr Left { get; } = left;
        public BoundBinOperator Op { get; } = op;
        public BoundExpr Right { get; } = right;
        public override TypeSymbol Type => Op.Result;
    }

    internal sealed class BoundUnaryExpr(BoundUnOperator op, BoundExpr operand) : BoundExpr
    {
        public BoundUnOperator Op { get; } = op;
        public BoundExpr Operand { get; } = operand;
        public override TypeSymbol Type => Op.Result;
    }

    internal sealed class BoundGroupingExpr(BoundExpr expr) : BoundExpr
    {
        public BoundExpr Expr { get; } = expr;
        public override TypeSymbol Type => Expr.Type;
    }

    internal sealed class BoundNameExpr(VarSymbol variable) : BoundExpr
    {
        public VarSymbol Variable { get; } = variable;
        public override TypeSymbol Type => Variable.Type;
    }

    internal sealed class BoundAssignExpr(VarSymbol variable, BoundExpr value) : BoundExpr
    {
        public VarSymbol Variable { get; } = variable;
        public BoundExpr Value { get; } = value;
        public override TypeSymbol Type => Value.Type;
    }

    internal sealed class BoundCallExpr(FnSymbol fn, ImmutableArray<BoundExpr> Args) : BoundExpr
    {
        public FnSymbol Fn { get; } = fn;
        public ImmutableArray<BoundExpr> Args { get; } = Args;
        public override TypeSymbol Type => Fn.ReturnType;
    }

    internal sealed class BoundGetExpr(BoundExpr instance, PropertySymbol property) : BoundExpr
    {
        public BoundExpr Instance { get; } = instance;
        public PropertySymbol Property { get; } = property;
        public override TypeSymbol Type => Property.Type;
    }

    internal sealed class BoundSetExpr(BoundExpr instance, PropertySymbol property, BoundExpr value) : BoundExpr
    {
        public BoundExpr Instance { get; } = instance;
        public PropertySymbol Property { get; } = property;
        public BoundExpr Value { get; } = value;
        public override TypeSymbol Type => Value.Type;
    }

    internal sealed class BoundMethodExpr(BoundExpr instance, MethodSymbol method, ImmutableArray<BoundExpr> Args) : BoundExpr
    {
        public BoundExpr Instance { get; } = instance;
        public MethodSymbol Method { get; } = method;
        public ImmutableArray<BoundExpr> Args { get; } = Args;
        public override TypeSymbol Type => Method.ReturnType;
    }

    internal sealed class BoundStaticGetExpr(ClassSymbol classSymbol, PropertySymbol property) : BoundExpr
    {
        public ClassSymbol ClassSymbol => classSymbol;
        public PropertySymbol Property { get; } = property;
        public override TypeSymbol Type => Property.Type;
    }

    internal sealed class BoundStaticSetExpr(ClassSymbol classSymbol, PropertySymbol property, BoundExpr value) : BoundExpr
    {
        public ClassSymbol ClassSymbol => classSymbol;
        public PropertySymbol Property { get; } = property;
        public BoundExpr Value { get; } = value;
        public override TypeSymbol Type => Value.Type;
    }

    internal sealed class BoundStaticMethodExpr(ClassSymbol classSymbol, MethodSymbol method, ImmutableArray<BoundExpr> Args) : BoundExpr
    {
        public ClassSymbol ClassSymbol { get; } = classSymbol;
        public MethodSymbol Method { get; } = method;
        public ImmutableArray<BoundExpr> Args { get; } = Args;
        public override TypeSymbol Type => Method.ReturnType;
    }

    internal sealed class BoundInstanceExpr(ClassSymbol classSymbol, ImmutableArray<BoundExpr> Args) : BoundExpr
    {
        public ClassSymbol ClassSymbol { get; } = classSymbol;
        public ImmutableArray<BoundExpr> Args { get; } = Args;
        public override TypeSymbol Type => new(ClassSymbol.Name, ClassSymbol);
    }

    internal sealed class BoundError() : BoundExpr
    {
        public override TypeSymbol Type => TypeSymbol.Error;
    }
}