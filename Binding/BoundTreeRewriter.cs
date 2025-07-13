using Delta.Binding.BoundNodes;
using System.Collections.Immutable;

namespace Delta.Binding
{
    internal abstract class BoundTreeRewriter
    {
        public virtual BoundStmt RewriteStmt(BoundStmt node) => node switch
        {
            BoundExprStmt => RewriteExpressionStmt((BoundExprStmt)node),
            BoundBlockStmt => RewriteBlockStmt((BoundBlockStmt)node),
            BoundVarStmt => RewriteVarStmt((BoundVarStmt)node),
            BoundIfStmt => RewriteIfStmt((BoundIfStmt)node),
            BoundLoopStmt => RewriteLoopStmt((BoundLoopStmt)node),
            BoundForStmt => RewriteForStmt((BoundForStmt)node),
            BoundLabelStmt => RewriteLabelStmt((BoundLabelStmt)node),
            BoundGotoStmt => RewriteGotoStmt((BoundGotoStmt)node),
            BoundCondGotoStmt => RewriteCondGotoStmt((BoundCondGotoStmt)node),
            BoundRetStmt => RewriteRetStmt((BoundRetStmt)node),
            _ => throw new Exception($"Unexpected node to lower: \"{node.GetType().Name}\"."),
        };

        public virtual BoundExpr RewriteExpr(BoundExpr node) => node switch
        {
            BoundLiteralExpr => RewriteLiteralExpr((BoundLiteralExpr)node),
            BoundUnaryExpr => RewriteUnaryExpr((BoundUnaryExpr)node),
            BoundBinaryExpr => RewriteBinaryExpr((BoundBinaryExpr)node),
            BoundNameExpr => RewriteNameExpr((BoundNameExpr)node),
            BoundAssignExpr => RewriteAssignExpr((BoundAssignExpr)node),
            BoundGetExpr => RewriteGetExpr((BoundGetExpr)node),
            BoundSetExpr => RewriteSetExpr((BoundSetExpr)node),
            BoundCallExpr => RewriteCallExpr((BoundCallExpr)node),
            BoundMethodExpr => RewriteMethodExpr((BoundMethodExpr)node),
            BoundInstanceExpr => RewriteInstanceExpr((BoundInstanceExpr)node),
            BoundError => RewriteErrorExpr((BoundError)node),
            _ => throw new Exception($"Unexpected node to lower: \"{node.GetType().Name}\"."),
        };

        protected virtual BoundStmt RewriteExpressionStmt(BoundExprStmt node)
        {
            BoundExpr expr = RewriteExpr(node.Expr);
            if (expr == node.Expr)
                return node;
            return new BoundExprStmt(expr);
        }

        protected virtual BoundStmt RewriteBlockStmt(BoundBlockStmt node)
        {
            ImmutableArray<BoundStmt>.Builder? builder = null;
            for (int i = 0; i < node.Stmts.Length; i++)
            {
                BoundStmt stmt = node.Stmts[i];
                BoundStmt oldStmt = stmt;
                BoundStmt newStmt = RewriteStmt(oldStmt);
                if (newStmt != oldStmt)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundStmt>(node.Stmts.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Stmts[j]);
                    }
                }

                builder?.Add(newStmt);
            }

            if (builder is null)
                return node;
            return new BoundBlockStmt(builder.ToImmutable());
        }

        protected virtual BoundStmt RewriteVarStmt(BoundVarStmt node)
        {
            BoundExpr value = RewriteExpr(node.Value);
            if (value == node.Value)
                return node;

            return new BoundVarStmt(node.Variable, value);
        }

        protected virtual BoundStmt RewriteIfStmt(BoundIfStmt node)
        {
            BoundExpr condition = RewriteExpr(node.Condition);
            BoundStmt thenBranch = RewriteStmt(node.ThenStmt);
            BoundStmt? elseClause = node.ElseClause is not null ? RewriteStmt(node.ElseClause) : null;
            if (condition == node.Condition && thenBranch == node.ThenStmt && elseClause == node.ElseClause)
                return node;
            return new BoundIfStmt(condition, thenBranch, elseClause);
        }

        protected virtual BoundStmt RewriteLoopStmt(BoundLoopStmt node)
        {
            BoundExpr condition = RewriteExpr(node.Condition);
            BoundStmt stmt = RewriteStmt(node.Body);
            if (condition == node.Condition && stmt == node.Body)
                return node;
            return new BoundLoopStmt(condition, stmt, node.BodyLabel, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStmt RewriteForStmt(BoundForStmt node)
        {
            BoundExpr startValue = RewriteExpr(node.StartValue);
            BoundExpr endValue = RewriteExpr(node.EndValue);
            BoundExpr? stepValue = node.StepValue is null ? null : RewriteExpr(node.StepValue);
            BoundStmt stmt = RewriteStmt(node.Body);
            if (startValue == node.StartValue && endValue == node.EndValue && stepValue == node.StepValue && stmt == node.Body)
                return node;
            return new BoundForStmt(node.Variable, startValue, endValue, stepValue, stmt, node.BodyLabel, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundStmt RewriteLabelStmt(BoundLabelStmt node) => node;

        protected virtual BoundStmt RewriteGotoStmt(BoundGotoStmt node) => node;

        protected virtual BoundStmt RewriteCondGotoStmt(BoundCondGotoStmt node)
        {
            BoundExpr condition = RewriteExpr(node.Condition);
            if (condition == node.Condition)
                return node;

            return new BoundCondGotoStmt(node.Label, condition, node.JumpIfTrue);
        }

        protected virtual BoundStmt RewriteRetStmt(BoundRetStmt node)
        {
            if (node.Value is null)
                return node;

            BoundExpr value = RewriteExpr(node.Value);
            if (value == node.Value)
                return node;

            return new BoundRetStmt(value);
        }

        protected virtual BoundExpr RewriteLiteralExpr(BoundLiteralExpr node) => node;

        protected virtual BoundExpr RewriteUnaryExpr(BoundUnaryExpr node)
        {
            BoundExpr operand = RewriteExpr(node.Operand);
            if (operand == node.Operand)
                return node;

            return new BoundUnaryExpr(node.Op, operand);
        }

        protected virtual BoundExpr RewriteBinaryExpr(BoundBinaryExpr node)
        {
            BoundExpr left = RewriteExpr(node.Left);
            BoundExpr right = RewriteExpr(node.Right);
            if (left == node.Left && right == node.Right)
                return node;

            return new BoundBinaryExpr(left, node.Op, right);
        }

        protected virtual BoundExpr RewriteNameExpr(BoundNameExpr node) => node;

        protected virtual BoundExpr RewriteAssignExpr(BoundAssignExpr node)
        {
            BoundExpr value = RewriteExpr(node.Value);
            if (value == node.Value)
                return node;
            return new BoundAssignExpr(node.Variable, value);
        }

        protected virtual BoundExpr RewriteGetExpr(BoundGetExpr node)
        {
            BoundExpr instance = RewriteExpr(node.Instance);
            if (instance == node.Instance)
                return node;
            return new BoundGetExpr(instance, node.Property);
        }

        protected virtual BoundExpr RewriteSetExpr(BoundSetExpr node)
        {
            BoundExpr value = RewriteExpr(node.Value);
            BoundExpr instance = RewriteExpr(node.Instance);
            if (value == node.Value && instance == node.Instance)
                return node;
            return new BoundSetExpr(instance, node.Property, value);
        }

        protected virtual BoundExpr RewriteCallExpr(BoundCallExpr node)
        {
            ImmutableArray<BoundExpr>.Builder? builder = null;
            for (int i = 0; i < node.Args.Length; ++i)
            {
                BoundExpr stmt = node.Args[i];
                BoundExpr oldExpr = stmt;
                BoundExpr newExpr = RewriteExpr(oldExpr);
                if (newExpr != oldExpr)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpr>(node.Args.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Args[j]);
                    }
                }

                builder?.Add(newExpr);
            }

            if (builder is null)
                return node;
            return new BoundCallExpr(node.Fn, builder.MoveToImmutable());
        }

        protected virtual BoundExpr RewriteMethodExpr(BoundMethodExpr node)
        {
            BoundExpr instance = RewriteExpr(node.Instance);
            ImmutableArray<BoundExpr>.Builder? builder = null;
            for (int i = 0; i < node.Args.Length; ++i)
            {
                BoundExpr stmt = node.Args[i];
                BoundExpr oldExpr = stmt;
                BoundExpr newExpr = RewriteExpr(oldExpr);
                if (newExpr != oldExpr)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpr>(node.Args.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Args[j]);
                    }
                }

                builder?.Add(newExpr);
            }

            if (builder is null && instance == node.Instance)
                return node;
            return new BoundMethodExpr(instance, node.Method, builder?.MoveToImmutable() ?? node.Args);
        }

        protected virtual BoundExpr RewriteInstanceExpr(BoundInstanceExpr node)
        {
            ImmutableArray<BoundExpr>.Builder? builder = null;
            for (int i = 0; i < node.Args.Length; ++i)
            {
                BoundExpr stmt = node.Args[i];
                BoundExpr oldExpr = stmt;
                BoundExpr newExpr = RewriteExpr(oldExpr);
                if (newExpr != oldExpr)
                {
                    if (builder is null)
                    {
                        builder = ImmutableArray.CreateBuilder<BoundExpr>(node.Args.Length);
                        for (int j = 0; j < i; ++j)
                            builder.Add(node.Args[j]);
                    }
                }

                builder?.Add(newExpr);
            }

            if (builder is null)
                return node;

            return new BoundInstanceExpr(node.ClassSymbol, builder.MoveToImmutable());
        }

        protected virtual BoundError RewriteErrorExpr(BoundError expr) => expr;
    }
}