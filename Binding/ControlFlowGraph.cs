using Delta.Analysis.Nodes;
using Delta.Binding.BoundNodes;
using Delta.Symbols;
using System.CodeDom.Compiler;

namespace Delta.Binding
{
    internal sealed class ControlFlowGraph
    {
        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> blocks, List<BasicBlockBranch> edges)
        {
            Start = start;
            End = end;
            Blocks = blocks;
            Branches = edges;
        }

        public BasicBlock Start { get; }
        public BasicBlock End { get; }
        public List<BasicBlock> Blocks { get; }
        public List<BasicBlockBranch> Branches { get; }

        public sealed class BasicBlock
        {
            public BasicBlock()
            { }

            public BasicBlock(bool isStart)
            {
                IsStart = isStart;
                IsEnd = !isStart;
            }

            public bool IsStart { get; }
            public bool IsEnd { get; }
            public List<BoundStmt> Stmts { get; } = [];
            public List<BasicBlockBranch> Incoming { get; } = [];
            public List<BasicBlockBranch> Outgoing { get; } = [];

            public override string ToString()
            {
                if (IsStart)
                    return "<Start>";

                if (IsEnd)
                    return "<End>";

                using StringWriter sw = new();
                using IndentedTextWriter itw = new(sw);
                foreach (BoundStmt stmt in Stmts)
                    stmt.WriteTo(itw);

                return itw.ToString()!;
            }
        }

        public sealed class BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpr? condition)
        {
            public BasicBlock From { get; } = from;
            public BasicBlock To { get; } = to;
            public BoundExpr? Condition { get; } = condition;

            public override string ToString() => Condition is null ? string.Empty : Condition.ToString();
        }

        public sealed class BasicBlockBuilder
        {
            private readonly List<BoundStmt> _stmts = [];
            private readonly List<BasicBlock> _blocks = [];

            public List<BasicBlock> Build(BoundBlockStmt blocks)
            {
                foreach (BoundStmt stmt in blocks.Stmts)
                {
                    switch (stmt)
                    {
                        case BoundLabelStmt:
                            StartBlock();
                            _stmts.Add(stmt);
                            break;

                        case BoundCondGotoStmt:
                        case BoundGotoStmt:
                        case BoundRetStmt:
                            _stmts.Add(stmt);
                            StartBlock();
                            break;

                        case BoundVarStmt:
                        case BoundExprStmt:
                            _stmts.Add(stmt);
                            break;

                        default:
                            throw new Exception($"Unexpected statement \"${stmt.GetType().Name}\".");
                    }
                }

                EndBlock();

                return [.. _blocks];
            }

            private void StartBlock() => EndBlock();

            private void EndBlock()
            {
                if (_stmts.Count > 0)
                {
                    BasicBlock block = new();
                    block.Stmts.AddRange(_stmts);
                    _blocks.Add(block);
                    _stmts.Clear();
                }
            }
        }

        public sealed class GraphBuilder
        {
            private readonly Dictionary<BoundStmt, BasicBlock> _blockFromStmt = [];
            private readonly Dictionary<LabelSymbol, BasicBlock> _blockFromLabel = [];
            private readonly List<BasicBlockBranch> _branches = [];
            private readonly BasicBlock _start = new(true), _end = new(false);

            public ControlFlowGraph Build(List<BasicBlock> blocks)
            {
                if (blocks.Count != 0)
                    Connect(_start, blocks.First());
                else
                    Connect(_start, _end);

                foreach ((BasicBlock block, BoundStmt stmt) in from BasicBlock block in blocks
                                                               from BoundStmt stmt in block.Stmts
                                                               select (block, stmt))
                {
                    _blockFromStmt.Add(stmt, block);
                    if (stmt is BoundLabelStmt l)
                        _blockFromLabel.Add(l.Label, block);
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    BasicBlock current = blocks[i];
                    BasicBlock next = i == blocks.Count - 1 ? _end : blocks[i + 1];

                    foreach (BoundStmt stmt in current.Stmts)
                    {
                        bool isLastStmt = stmt == current.Stmts.Last();
                        switch (stmt)
                        {
                            case BoundCondGotoStmt:
                                BoundCondGotoStmt cgs = (BoundCondGotoStmt)stmt;

                                BoundExpr negCond = Negate(cgs.Condition);
                                BoundExpr thenCond = cgs.JumpIfTrue ? cgs.Condition : negCond;
                                BoundExpr elseCond = cgs.JumpIfTrue ? negCond : cgs.Condition;

                                Connect(current, _blockFromLabel[cgs.Label], thenCond);
                                Connect(current, next, elseCond);
                                break;

                            case BoundGotoStmt:
                                Connect(current, _blockFromLabel[((BoundGotoStmt)stmt).Label]);
                                break;

                            case BoundRetStmt:
                                Connect(current, _end);
                                break;

                            case BoundLabelStmt:
                            case BoundVarStmt:
                            case BoundExprStmt:
                                if (isLastStmt)
                                    Connect(current, next);
                                break;

                            default:
                                throw new Exception($"Unexpected statement \"${stmt.GetType().Name}\".");
                        }
                    }
                }

            ScanAgain:
                foreach (BasicBlock block in blocks)
                {
                    if (block.Incoming.Count == 0)
                    {
                        RemoveBlock(ref blocks, block);
                        goto ScanAgain;
                    }
                }

                blocks.Insert(0, _start);
                blocks.Add(_end);
                return new(_start, _end, blocks, _branches);
            }

            private void RemoveBlock(ref List<BasicBlock> blocks, BasicBlock block)
            {
                foreach (BasicBlockBranch branch in block.Incoming)
                {
                    branch.From.Outgoing.Remove(branch);
                    _branches.Remove(branch);
                }

                foreach (BasicBlockBranch branch in block.Outgoing)
                {
                    branch.To.Incoming.Remove(branch);
                    _branches.Remove(branch);
                }

                blocks.Remove(block);
            }

            private static BoundExpr Negate(BoundExpr condition)
            {
                if (condition is BoundLiteralExpr l)
                    return new BoundLiteralExpr(!(bool)l.Value, TypeSymbol.Bool);

                BoundUnOperator unOp = BoundUnOperator.Bind(NodeKind.Not, TypeSymbol.Bool)!;
                return new BoundUnaryExpr(unOp, condition);
            }

            private void Connect(BasicBlock from, BasicBlock to, BoundExpr? condition = null)
            {
                if (condition is BoundLiteralExpr l)
                {
                    if ((bool)l.Value)
                        condition = null;
                    else
                        return;
                }

                BasicBlockBranch branch = new(from, to, condition);
                from.Outgoing.Add(branch);
                to.Incoming.Add(branch);
                _branches.Add(branch);
            }
        }

        public void WriteTo(TextWriter writer)
        {
            static string Quote(string text) => "\"" + text.TrimEnd().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace(Environment.NewLine, "\\l") + "\"";

            writer.WriteLine("digraph G {");
            Dictionary<BasicBlock, string> blockIds = [];
            for (int i = 0; i < Blocks.Count; ++i)
                blockIds.Add(Blocks[i], $"N{i}");

            foreach (BasicBlock block in Blocks)
            {
                string id = blockIds[block];
                string label = Quote(block.ToString());
                writer.WriteLine($"    {id} [label = {label}, shape = box]");
            }

            foreach (BasicBlockBranch branch in Branches)
            {
                string fromId = blockIds[branch.From];
                string toId = blockIds[branch.To];
                string label = Quote(branch.Condition?.ToString() ?? "");
                writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
            }

            writer.WriteLine("}");
        }

        public static ControlFlowGraph CreateGraph(BoundBlockStmt body)
        {
            BasicBlockBuilder blockBuilder = new();
            List<BasicBlock> blocks = blockBuilder.Build(body);
            GraphBuilder graphBuilder = new();
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStmt body)
        {
            ControlFlowGraph graph = CreateGraph(body);
            foreach (BasicBlockBranch branch in graph.End.Incoming)
            {
                BoundStmt? lastStmt = branch.From.Stmts.LastOrDefault();
                if (lastStmt is null || lastStmt is not BoundRetStmt)
                    return false;
            }

            return true;
        }
    }
}