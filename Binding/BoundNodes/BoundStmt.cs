using Delta.Symbols;
using System.Collections.Immutable;

namespace Delta.Binding.BoundNodes
{
    internal abstract class BoundStmt : BoundNode
    {
    }

    internal sealed class BoundExprStmt(BoundExpr expr) : BoundStmt
    {
        public BoundExpr Expr { get; } = expr;
    }

    internal sealed class BoundVarStmt(VarSymbol variable, BoundExpr value) : BoundStmt
    {
        public VarSymbol Variable { get; } = variable;
        public BoundExpr Value { get; } = value;
    }

    internal sealed class BoundBlockStmt(ImmutableArray<BoundStmt> stmts) : BoundStmt
    {
        public ImmutableArray<BoundStmt> Stmts { get; } = stmts;
    }

    internal sealed class BoundIfStmt(BoundExpr condition, BoundStmt thenStmt, BoundStmt? elseClause) : BoundStmt
    {
        public BoundExpr Condition { get; } = condition;
        public BoundStmt ThenStmt { get; } = thenStmt;
        public BoundStmt? ElseClause { get; } = elseClause;
    }

    internal sealed class BoundLoopStmt(BoundExpr condition, BoundStmt body, LabelSymbol bodyLabel, LabelSymbol breakLabel, LabelSymbol continueLabel) : BoundStmt
    {
        public BoundExpr Condition { get; } = condition;
        public BoundStmt Body { get; } = body;
        public LabelSymbol BodyLabel { get; } = bodyLabel;
        public LabelSymbol BreakLabel { get; } = breakLabel;
        public LabelSymbol ContinueLabel { get; } = continueLabel;
    }

    internal sealed class BoundForStmt(VarSymbol variable, BoundExpr startValue, BoundExpr endValue, BoundExpr? stepValue, BoundStmt body, LabelSymbol bodyLabel, LabelSymbol breakLabel, LabelSymbol continueLabel) : BoundStmt
    {
        public VarSymbol Variable { get; } = variable;
        public BoundExpr StartValue { get; } = startValue;
        public BoundExpr EndValue { get; } = endValue;
        public BoundExpr? StepValue { get; } = stepValue;
        public BoundStmt Body { get; } = body;
        public LabelSymbol BodyLabel { get; } = bodyLabel;
        public LabelSymbol BreakLabel { get; } = breakLabel;
        public LabelSymbol ContinueLabel { get; } = continueLabel;
    }

    internal sealed class BoundFnDecl(FnSymbol symbol) : BoundStmt
    {
        public FnSymbol Symbol { get; } = symbol;
    }

    internal sealed class BoundRetStmt(BoundExpr? value) : BoundStmt
    {
        public BoundExpr? Value { get; } = value;
    }

    internal sealed class BoundLabelStmt(LabelSymbol label) : BoundStmt
    {
        public LabelSymbol Label { get; } = label;
    }

    internal sealed class BoundGotoStmt(LabelSymbol label) : BoundStmt
    {
        public LabelSymbol Label { get; } = label;
    }

    internal sealed class BoundCondGotoStmt(LabelSymbol label, BoundExpr condition, bool jumpIfTrue = true) : BoundStmt
    {
        public LabelSymbol Label { get; } = label;
        public BoundExpr Condition { get; } = condition;
        public bool JumpIfTrue { get; } = jumpIfTrue;
    }

    internal sealed class BoundErrorStmt() : BoundStmt
    {
    }
}