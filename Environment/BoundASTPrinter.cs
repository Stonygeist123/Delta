using Delta.Analysis.Nodes;
using Delta.Binding;
using Delta.Binding.BoundNodes;
using System.Collections;
using System.Reflection;

namespace Delta.Environment
{
    internal static class BoundASTPrinter
    {
        public static void PrintAll(List<BoundStmt> stmts) => stmts.ForEach(s => Print(s));

        public static void Print(BoundStmt stmt, int indent = 0)
        {
            void WriteIndent()
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(new string(' ', indent * 2));
            }

            ConsoleColor defaultColor = Console.ForegroundColor;
            if (stmt == null)
            {
                WriteIndent();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("?");
                Console.ForegroundColor = defaultColor;
                return;
            }

            WriteIndent();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{stmt.GetType().Name}");

            Type type = stmt.GetType();
            List<PropertyInfo> properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.Name != nameof(Node.Kind))
                .ToList();

            foreach (PropertyInfo prop in properties)
            {
                object? value = prop.GetValue(stmt);
                WriteIndent();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"- {prop.Name}: ");

                if (value is BoundStmt childStmt)
                {
                    Console.WriteLine();
                    Print(childStmt, indent + 1);
                }
                else if (value is BoundExpr childExpr)
                {
                    Console.WriteLine();
                    Print(childExpr, indent + 1);
                }
                else if (value is IEnumerable enumerable && value is not string)
                {
                    Console.WriteLine();
                    foreach (object? item in enumerable)
                    {
                        if (item is BoundStmt listNodeStmt)
                            Print(listNodeStmt, indent + 1);
                        if (item is BoundExpr listNodeExpr)
                            Print(listNodeExpr, indent + 1);
                        else
                        {
                            WriteIndent();
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(item?.ToString());
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(value?.ToString() ?? "?");
                }
            }

            Console.ForegroundColor = defaultColor;
        }

        public static void Print(BoundExpr expr, int indent = 0)
        {
            void WriteIndent()
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(new string(' ', indent * 2));
            }

            ConsoleColor defaultColor = Console.ForegroundColor;
            if (expr == null)
            {
                WriteIndent();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("?");
                Console.ForegroundColor = defaultColor;
                return;
            }

            WriteIndent();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{expr.GetType().Name}");

            Type type = expr.GetType();
            List<PropertyInfo> properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.Name != nameof(Node.Kind))
                .ToList();

            foreach (PropertyInfo prop in properties)
            {
                object? value = prop.GetValue(expr);
                WriteIndent();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"- {prop.Name}: ");

                if (value is BoundExpr childExpr)
                {
                    Console.WriteLine();
                    Print(childExpr, indent + 1);
                }
                else if (value is BoundType childType)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(childType);
                }
                else if (value is BoundBinOperator childBinOp)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(childBinOp.OpKind);
                }
                else if (value is BoundUnOperator childUnOp)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(childUnOp.OpKind);
                }
                else if (value is IEnumerable enumerable && value is not string)
                {
                    Console.WriteLine();
                    foreach (object? item in enumerable)
                    {
                        if (item is BoundExpr listNode)
                            Print(listNode, indent + 1);
                        else
                        {
                            WriteIndent();
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(item?.ToString());
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine(value?.ToString() ?? "?");
                }
            }

            Console.ForegroundColor = defaultColor;
        }
    }
}