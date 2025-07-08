using System.Reflection;

namespace Delta.Analysis.Nodes
{
    internal enum NodeKind
    {
        // Operators
        Plus, Minus, Slash, Star,

        Not, EqEq, NotEq, Greater, GreaterEq, Less, LessEq, And, Or,

        // Literals
        Number, String, Identifier,

        // Keywords
        Var, Mut, True, False, If, Else, Loop, For, Fn, Ret, Break, Continue, Step,

        // Others
        LParen, RParen, LBrace, RBrace, Comma, Eq, Colon, Arrow, Semicolon,

        Space, Bad, EOF,

        // Misc
        ParameterList, Param, Arg, TypeClause, CompilationUnit,

        // Exprs
        LiteralExpr, BinaryExpr, UnaryExpr, GroupingExpr, NameExpr, AssignExpr, CallExpr, ErrorExpr,

        // Stmts
        ExprStmt, VarStmt, BlockStmt, IfStmt, ElseStmt, LoopStmt, ForStmt, RetStmt, BreakStmt, ContinueStmt, ErrorStmt,

        FnDecl
    }

    internal abstract class Node(SyntaxTree syntaxTree)
    {
        public abstract NodeKind Kind { get; }
        public SyntaxTree SyntaxTree { get; } = syntaxTree;

        public virtual TextSpan Span
        {
            get
            {
                IEnumerable<Node> children = GetChildren();
                return new(children.First().Span.Start, children.Last().Span.End);
            }
        }

        public TextLocation Location => new(SyntaxTree.Source, Span);

        public IEnumerable<Node> GetChildren()
        {
            PropertyInfo[]? properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (typeof(Node).IsAssignableFrom(property.PropertyType))
                {
                    Node? child = (Node?)property.GetValue(this);
                    if (child is not null)
                        yield return child;
                }
                else if (typeof(IEnumerable<Node?>).IsAssignableFrom(property.PropertyType))
                {
                    IEnumerable<Node?>? children = (IEnumerable<Node?>?)property.GetValue(this);
                    if (children is not null)
                        foreach (Node? child in children)
                            if (child is not null)
                                yield return child;
                }
            }
        }

        public Token GetLastToken() => this is Token token ? token : GetChildren().Last().GetLastToken();

        public void WriteTo(TextWriter writer) => Print(writer, this);

        private static void Print(TextWriter writer, Node? node, string indent = "", bool isLast = true)
        {
            bool isConsole = writer == Console.Out;

            if (node is Node n)
            {
                string marker = isLast ? "└──" : "├──";
                writer.Write(indent);
                if (isConsole)
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                writer.Write(marker);
                if (isConsole)
                    Console.ForegroundColor = n is Token ? ConsoleColor.Blue : ConsoleColor.Cyan;

                writer.Write(n.Kind);
                if (n is Token t && (t.Kind == NodeKind.String || t.Kind == NodeKind.Number || t.Kind == NodeKind.True || t.Kind == NodeKind.False))
                    writer.Write($" {t.Lexeme}");

                if (isConsole)
                    Console.ResetColor();

                writer.WriteLine();
                indent += isLast ? "    " : "│   ";
                Node? lastChild = node?.GetChildren().LastOrDefault();

                foreach (Node? child in n.GetChildren())
                    Print(writer, child, indent, child == lastChild);
            }
        }

        public override string ToString()
        {
            using StringWriter writer = new();
            WriteTo(writer);
            return writer.ToString();
        }
    }
}