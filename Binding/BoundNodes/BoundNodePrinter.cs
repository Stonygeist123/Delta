using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.IO;
using Delta.Symbols;
using System.CodeDom.Compiler;

namespace Delta.Binding.BoundNodes
{
    internal static class BoundNodePrinter
    {
        public static void WriteTo(this BoundNode node, TextWriter writer)
        {
            if (writer is IndentedTextWriter iw)
                node.WriteTo(iw);
            else
                node.WriteTo(new IndentedTextWriter(writer));
        }

        public static void WriteTo(this BoundNode node, IndentedTextWriter writer)
        {
            switch (node)
            {
                case BoundBlockStmt:
                    WriteBlockStmt((BoundBlockStmt)node, writer);
                    break;

                case BoundVarStmt:
                    WriteVarStmt((BoundVarStmt)node, writer);
                    break;

                case BoundIfStmt:
                    WriteIfStmt((BoundIfStmt)node, writer);
                    break;

                case BoundLoopStmt:
                    WriteLoopStmt((BoundLoopStmt)node, writer);
                    break;

                case BoundRetStmt:
                    WriteRetStmt((BoundRetStmt)node, writer);
                    break;

                case BoundExprStmt:
                    WriteExprStmt((BoundExprStmt)node, writer);
                    break;

                case BoundError:
                    WriteErrorExpr(writer);
                    break;

                case BoundLiteralExpr:
                    WriteLiteralExpr((BoundLiteralExpr)node, writer);
                    break;

                case BoundNameExpr:
                    WriteNameExpr((BoundNameExpr)node, writer);
                    break;

                case BoundAssignExpr:
                    WriteAssignExpr((BoundAssignExpr)node, writer);
                    break;

                case BoundUnaryExpr:
                    WriteUnaryExpr((BoundUnaryExpr)node, writer);
                    break;

                case BoundBinaryExpr:
                    WriteBinaryExpr((BoundBinaryExpr)node, writer);
                    break;

                case BoundCallExpr:
                    WriteCallExpr((BoundCallExpr)node, writer);
                    break;

                default:
                    throw new Exception($"Unexpected node: '{node.GetType().Name}'.");
            }
        }

        private static void WriteNestedStmt(this IndentedTextWriter writer, BoundStmt stmt)
        {
            bool needsIndentation = stmt is not BoundBlockStmt;
            if (needsIndentation)
                ++writer.Indent;

            stmt.WriteTo(writer);
            if (needsIndentation)
                --writer.Indent;
        }

        private static void WriteNestedExpr(this IndentedTextWriter writer, int precedence, BoundExpr expr)
        {
            if (expr is BoundUnaryExpr un)
                writer.WriteNestedExpr(precedence, un.Op.NodeKind.GetUnOpPrecedence(), un);
            else if (expr is BoundUnaryExpr bin)
                writer.WriteNestedExpr(precedence, bin.Op.NodeKind.GetUnOpPrecedence(), bin);
            else
                expr.WriteTo(writer);
        }

        private static void WriteNestedExpr(this IndentedTextWriter writer, int parentPrecedence, int curPrecedence, BoundExpr expr)
        {
            bool needsParens = parentPrecedence >= curPrecedence;
            if (needsParens)
                writer.WritePunctuation("(");

            expr.WriteTo(writer);
            if (needsParens)
                writer.WritePunctuation(")");
        }

        private static void WriteBlockStmt(BoundBlockStmt node, IndentedTextWriter writer)
        {
            writer.WritePunctuation(NodeKind.LBrace);
            writer.WriteLine();
            ++writer.Indent;

            foreach (BoundStmt s in node.Stmts)
                s.WriteTo(writer);

            --writer.Indent;
            writer.WritePunctuation(NodeKind.RBrace);
            writer.WriteLine();
        }

        private static void WriteVarStmt(BoundVarStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(NodeKind.Var);
            if (node.Variable.Mutable)
            {
                writer.WriteSpace();
                writer.WriteKeyword(NodeKind.Mut);
            }

            writer.WriteSpace();
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(NodeKind.Eq);
            writer.WriteSpace();
            node.Value.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteIfStmt(BoundIfStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(NodeKind.If);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStmt(node.ThenStmt);

            if (node.ElseClause is not null)
            {
                writer.WriteKeyword(NodeKind.Else);
                writer.WriteLine();
                writer.WriteNestedStmt(node.ElseClause);
            }
        }

        private static void WriteLoopStmt(BoundLoopStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(NodeKind.Loop);
            writer.WriteSpace();
            node.Condition.WriteTo(writer);
            writer.WriteLine();
            writer.WriteNestedStmt(node.Body);
        }

        private static void WriteRetStmt(BoundRetStmt node, IndentedTextWriter writer)
        {
            writer.WriteKeyword(NodeKind.Ret);
            if (node.Value is not null)
            {
                writer.WriteSpace();
                node.Value.WriteTo(writer);
            }

            writer.WritePunctuation(NodeKind.Semicolon);
            writer.WriteLine();
        }

        private static void WriteExprStmt(BoundExprStmt node, IndentedTextWriter writer)
        {
            node.Expr.WriteTo(writer);
            writer.WriteLine();
        }

        private static void WriteErrorExpr(IndentedTextWriter writer) => writer.WriteKeyword("?");

        private static void WriteLiteralExpr(BoundLiteralExpr node, IndentedTextWriter writer)
        {
            string value = node.Value.ToString()!;
            if (node.Type == TypeSymbol.Bool || node.Type == TypeSymbol.Number)
                writer.WriteLiteral(value);
            else if (node.Type == TypeSymbol.String)
                writer.WriteString($"\"{value.Replace("\"", "\"\"")}\"");
            else
                throw new Exception($"Unexpected type {node.Type}");
        }

        private static void WriteNameExpr(BoundNameExpr node, IndentedTextWriter writer) => writer.WriteIdentifier(node.Variable.Name);

        private static void WriteAssignExpr(BoundAssignExpr node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Variable.Name);
            writer.WriteSpace();
            writer.WritePunctuation(NodeKind.Eq);
            writer.WriteSpace();
            node.Value.WriteTo(writer);
        }

        private static void WriteUnaryExpr(BoundUnaryExpr node, IndentedTextWriter writer)
        {
            int precedence = node.Op.NodeKind.GetUnOpPrecedence();
            writer.WritePunctuation(node.Op.NodeKind);
            writer.WriteNestedExpr(precedence, node.Operand);
        }

        private static void WriteBinaryExpr(BoundBinaryExpr node, IndentedTextWriter writer)
        {
            int precedence = node.Op.NodeKind.GetBinOpPrecedence();
            writer.WriteNestedExpr(precedence, node.Left);
            writer.WriteSpace();
            writer.WritePunctuation(node.Op.NodeKind);
            writer.WriteSpace();
            writer.WriteNestedExpr(precedence, node.Right);
        }

        private static void WriteCallExpr(BoundCallExpr node, IndentedTextWriter writer)
        {
            writer.WriteIdentifier(node.Fn.Name);
            writer.WritePunctuation(NodeKind.LParen);

            bool isFirst = true;
            foreach (BoundExpr? arg in node.Args)
            {
                if (isFirst)
                    isFirst = false;
                else
                {
                    writer.WritePunctuation(NodeKind.Comma);
                    writer.WriteSpace();
                }

                arg.WriteTo(writer);
            }

            writer.WritePunctuation(NodeKind.RParen);
        }
    }
}