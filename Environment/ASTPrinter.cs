using Delta.Analysis.Nodes;
using System.Collections;
using System.Reflection;

namespace Delta.Environment
{
    internal static class ASTPrinter
    {
        public static void Print(Node node, int indent = 0)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            void WriteIndent()
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(new string(' ', indent * 2));
            }

            if (node == null)
            {
                WriteIndent();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("?");
                Console.ForegroundColor = defaultColor;
                return;
            }

            WriteIndent();
            Console.ForegroundColor = ConsoleColor.Magenta;
            if (node is Token token)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{token.Kind} [{token.Span.Start}-{token.Span.End}]: {token.Lexeme}");
                Console.ForegroundColor = defaultColor;
                return;
            }
            else
                Console.WriteLine($"{node.GetType().Name}");

            Type type = node.GetType();
            List<PropertyInfo> properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.Name != nameof(Node.Kind))
                .ToList();

            foreach (PropertyInfo prop in properties)
            {
                object? value = prop.GetValue(node);
                WriteIndent();
                Console.ForegroundColor = ConsoleColor.Yellow;
                if (node is not LiteralExpr)
                    Console.Write($"- {prop.Name}: ");

                if (value is Node childNode)
                {
                    if (node is not LiteralExpr)
                        Console.WriteLine();
                    Print(childNode, indent + 1);
                }
                else if (value is IEnumerable enumerable && value is not string)
                {
                    Console.WriteLine();
                    foreach (object? item in enumerable)
                    {
                        if (item is Node listNode)
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
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(value?.ToString() ?? "?");
                }
            }

            Console.ForegroundColor = defaultColor;
        }
    }
}