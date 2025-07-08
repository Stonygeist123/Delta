using Delta.Analysis.Nodes;
using Delta.Binding;
using Delta.Binding.BoundNodes;
using Delta.Symbols;
using System.Collections.Immutable;

namespace Delta.Lowerering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount = 0;

        private LabelSymbol GenerateLabel() => new($"Label_{++_labelCount}");

        public static BoundBlockStmt Lower(BoundStmt stmt)
        {
            Lowerer lowerer = new();
            return Flatten(lowerer.RewriteStmt(stmt));
        }

        private static BoundBlockStmt Flatten(BoundStmt stmt)
        {
            ImmutableArray<BoundStmt>.Builder builder = ImmutableArray.CreateBuilder<BoundStmt>();
            Stack<BoundStmt> stack = new();
            stack.Push(stmt);

            while (stack.Count > 0)
            {
                BoundStmt current = stack.Pop();
                if (current is BoundBlockStmt b)
                {
                    foreach (BoundStmt s in b.Stmts.Reverse())
                        stack.Push(s);
                }
                else
                    builder.Add(current);
            }

            return new(builder.ToImmutable());
        }

        protected override BoundStmt RewriteIfStmt(BoundIfStmt node)
        {
            if (node.ElseClause is null)
            {
                LabelSymbol endLabel = GenerateLabel();
                BoundCondGotoStmt gotoFalse = new(endLabel, node.Condition, false);
                BoundLabelStmt endLabelStmt = new(endLabel);
                return RewriteStmt(new BoundBlockStmt([gotoFalse, node.ThenStmt, endLabelStmt]));
            }
            else
            {
                LabelSymbol elseLabel = GenerateLabel();
                LabelSymbol endLabel = GenerateLabel();
                BoundCondGotoStmt gotoFalse = new(elseLabel, node.Condition, false);
                BoundGotoStmt gotoEndStmt = new(endLabel);
                BoundLabelStmt elseLabelStmt = new(elseLabel);
                BoundLabelStmt endLabelStmt = new(endLabel);
                return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(gotoFalse, node.ThenStmt, gotoEndStmt, elseLabelStmt, node.ElseClause, endLabelStmt)));
            }
        }

        protected override BoundStmt RewriteLoopStmt(BoundLoopStmt node)
        {
            BoundGotoStmt gotoContinue = new(node.ContinueLabel);
            BoundLabelStmt bodyLabelStmt = new(node.BodyLabel);
            BoundLabelStmt continueLabelStmt = new(node.ContinueLabel);
            BoundCondGotoStmt gotoTrue = new(node.BodyLabel, node.Condition);
            BoundLabelStmt breakLabelStmt = new(node.BreakLabel);
            return RewriteStmt(new BoundBlockStmt(ImmutableArray.Create(gotoContinue, bodyLabelStmt, node.Body, continueLabelStmt, gotoTrue, breakLabelStmt)));
        }

        protected override BoundStmt RewriteForStmt(BoundForStmt node)
        {
            BoundVarStmt varStmt = new(node.Variable, node.StartValue);
            BoundBinaryExpr descending = new(node.StartValue, BoundBinOperator.Bind(NodeKind.Greater, TypeSymbol.Number, TypeSymbol.Number)!, node.EndValue);
            BoundNameExpr nameExpr = new(node.Variable);
            BoundBinaryExpr newValueIncr = new(nameExpr, BoundBinOperator.Bind(NodeKind.Plus, TypeSymbol.Number, TypeSymbol.Number)!, node.StepValue ?? new BoundLiteralExpr(1d, TypeSymbol.Number));
            BoundBinaryExpr newValueDesc = new(nameExpr, BoundBinOperator.Bind(NodeKind.Minus, TypeSymbol.Number, TypeSymbol.Number)!, node.StepValue ?? new BoundLiteralExpr(1d, TypeSymbol.Number));
            BoundIfStmt incrStmt = new(descending,
                new BoundExprStmt(new BoundAssignExpr(node.Variable, newValueDesc)),
                new BoundExprStmt(new BoundAssignExpr(node.Variable, newValueIncr)));
            BoundLabelStmt continueLabelStmt = new(node.ContinueLabel);
            BoundBlockStmt body = new([node.Body, continueLabelStmt, incrStmt]);

            BoundGotoStmt gotoContinue = new(node.ContinueLabel);
            BoundLabelStmt bodyLabelStmt = new(node.BodyLabel);
            BoundLabelStmt breakLabelStmt = new(node.BreakLabel);

            BoundBinaryExpr conditionIncr = new(nameExpr, BoundBinOperator.Bind(NodeKind.LessEq, TypeSymbol.Number, TypeSymbol.Number)!, node.EndValue);
            BoundBinaryExpr conditionDesc = new(nameExpr, BoundBinOperator.Bind(NodeKind.GreaterEq, TypeSymbol.Number, TypeSymbol.Number)!, node.EndValue);
            BoundIfStmt gotoTrue = new(descending, new BoundCondGotoStmt(node.BodyLabel, conditionDesc), new BoundCondGotoStmt(node.BodyLabel, conditionIncr));
            return RewriteStmt(new BoundBlockStmt([varStmt, gotoContinue, bodyLabelStmt, body, gotoTrue, breakLabelStmt]));
        }
    }
}