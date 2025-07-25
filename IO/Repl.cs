﻿using Delta.Analysis;
using Delta.Analysis.Nodes;
using Delta.Evaluation;
using Delta.Symbols;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;

namespace Delta.IO
{
    internal abstract class Repl
    {
        private readonly List<MetaCommand> _metaCmds = [];
        private bool _done = false;

        protected Repl() => InitializeMetaCommand();

        private void InitializeMetaCommand()
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                MetaCommandAttribute? attr = method.GetCustomAttribute<MetaCommandAttribute>();
                if (attr is null)
                    continue;

                MetaCommand metaCmd = new(attr.Name, attr.Description, method);
                _metaCmds.Add(metaCmd);
            }
        }

        public void Run()
        {
            while (true)
            {
                string? text = EditSubmission();
                if (string.IsNullOrEmpty(text))
                    continue;

                if (!text.Contains(Environment.NewLine) && text.StartsWith('#'))
                    EvaluateMetaCommand(text);
                else
                    EvaluateSubmission(text);
            }
        }

        private sealed class SubmissionView
        {
            private readonly Action<string> _lineRenderer;
            private readonly ObservableCollection<string> _document;
            private int _cursorTop;
            private int _renderedLineCount, _currentLine, _currentColumn;

            public SubmissionView(Action<string> lineRenderer, ObservableCollection<string> document)
            {
                _lineRenderer = lineRenderer;
                _document = document;
                _document.CollectionChanged += SubmissionDocumentChanged;
                _cursorTop = Console.CursorTop;
                Render();
            }

            private void SubmissionDocumentChanged(object? sender, NotifyCollectionChangedEventArgs e) => Render();

            private void Render()
            {
                Console.CursorVisible = false;
                int lineCount = 0;
                foreach (string line in _document)
                {
                    if (_cursorTop + lineCount >= Console.WindowHeight)
                    {
                        Console.SetCursorPosition(0, Console.WindowHeight - 1);
                        Console.WriteLine();
                        if (_cursorTop > 0)
                            --_cursorTop;
                    }

                    Console.SetCursorPosition(0, _cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    if (lineCount == 0)
                        Console.Write("» ");
                    else
                        Console.Write("· ");

                    Console.ResetColor();
                    _lineRenderer(line);
                    Console.Write(new string(' ', Console.WindowWidth - line.Length - 2));
                    ++lineCount;
                }

                int numOfBlankLines = _renderedLineCount - lineCount;
                if (numOfBlankLines > 0)
                {
                    string blankLine = new(' ', Console.WindowWidth);
                    for (int i = 0; i < numOfBlankLines; ++i)
                    {
                        Console.SetCursorPosition(0, _cursorTop + lineCount);
                        Console.WriteLine(blankLine);
                    }
                }

                _renderedLineCount = lineCount;
                Console.CursorVisible = true;
                UpdateCursorPosition();
            }

            private void UpdateCursorPosition() => Console.SetCursorPosition(2 + _currentColumn, _cursorTop + _currentLine);

            public int CurrentLine
            {
                get => _currentLine;
                set
                {
                    if (_currentLine != value)
                    {
                        _currentLine = value;
                        _currentColumn = Math.Min(_document[_currentLine].Length, _currentColumn);
                        UpdateCursorPosition();
                    }
                }
            }

            public int CurrentColumn
            {
                get => _currentColumn;
                set
                {
                    if (_currentColumn != value)
                    {
                        _currentColumn = value;
                        UpdateCursorPosition();
                    }
                }
            }
        }

        public string EditSubmission()
        {
            _done = false;
            ObservableCollection<string> document = [""];
            SubmissionView view = new(RenderLine, document);
            while (!_done)
                HandleKey(Console.ReadKey(true), document, view);

            view.CurrentLine = document.Count - 1;
            view.CurrentColumn = document[view.CurrentLine].Length;

            Console.WriteLine();
            return string.Join(Environment.NewLine, document).Replace("\\n", "\n");
        }

        private void HandleKey(ConsoleKeyInfo key, ObservableCollection<string> document, SubmissionView view)
        {
            if (key.Modifiers == default)
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(document, view);
                        break;

                    case ConsoleKey.Enter:
                        HandleEnter(document, view);
                        break;

                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(view);
                        break;

                    case ConsoleKey.RightArrow:
                        HandleRightArrow(document, view);
                        break;

                    case ConsoleKey.UpArrow:
                        HandleUpArrow(view);
                        break;

                    case ConsoleKey.DownArrow:
                        HandleDownArrow(document, view);
                        break;

                    case ConsoleKey.Backspace:
                        HandleBackspace(document, view);
                        break;

                    case ConsoleKey.Delete:
                        HandleDelete(document, view);
                        break;

                    case ConsoleKey.Home:
                        HandleHome(view);
                        break;

                    case ConsoleKey.End:
                        HandleEnd(document, view);
                        break;

                    case ConsoleKey.Tab:
                        HandleTab(document, view);
                        break;
                }
            else if (key.Modifiers == ConsoleModifiers.Control)
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleCtrlEnter();
                        break;
                }
            else if (key.Modifiers == ConsoleModifiers.Shift)
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleShiftEnter(document, view);
                        break;
                }

            if (key.Key != ConsoleKey.Backspace && key.KeyChar >= ' ')
                HandleTyping(document, view, key.KeyChar.ToString());
        }

        private static void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            document.Add(string.Empty);
            view.CurrentLine = 0;
            view.CurrentColumn = 0;
        }

        private void HandleEnter(ObservableCollection<string> document, SubmissionView view)
        {
            string text = string.Join(Environment.NewLine, document);
            if (text.StartsWith('#') || IsCompleteSubmission(text))
            {
                _done = true;
                return;
            }

            InsertLine(document, view);
        }

        private void HandleCtrlEnter() => _done = true;

        private static void HandleShiftEnter(ObservableCollection<string> document, SubmissionView view) => InsertLine(document, view);

        private static void InsertLine(ObservableCollection<string> document, SubmissionView view)
        {
            string remaining = document[view.CurrentLine][view.CurrentColumn..];
            document[view.CurrentLine] = document[view.CurrentLine][..view.CurrentColumn];

            int lineIndex = view.CurrentLine + 1;
            document.Insert(lineIndex, remaining);
            view.CurrentColumn = 0;
            view.CurrentLine = lineIndex;
        }

        private static void HandleLeftArrow(SubmissionView view)
        {
            if (view.CurrentColumn > 0)
                --view.CurrentColumn;
        }

        private static void HandleRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentColumn <= document[view.CurrentLine].Length - 1)
                ++view.CurrentColumn;
        }

        private static void HandleUpArrow(SubmissionView view)
        {
            if (view.CurrentLine > 0)
                --view.CurrentLine;
        }

        private static void HandleDownArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine < document.Count - 1)
                ++view.CurrentLine;
        }

        private static void HandleBackspace(ObservableCollection<string> document, SubmissionView view)
        {
            int start = view.CurrentColumn;
            if (start == 0)
            {
                if (view.CurrentLine == 0)
                    return;

                string curLine = document[view.CurrentLine];
                string prevLine = document[view.CurrentLine - 1];
                document.RemoveAt(view.CurrentLine);
                --view.CurrentLine;
                document[view.CurrentLine] = prevLine + curLine;
                view.CurrentColumn = prevLine.Length;
            }
            else
            {
                int lineIndex = view.CurrentLine;
                string line = document[lineIndex];
                string before = line[..(start - 1)];
                string after = line[start..];
                document[lineIndex] = before + after;
                --view.CurrentColumn;
            }
        }

        private static void HandleDelete(ObservableCollection<string> document, SubmissionView view)
        {
            int lineIndex = view.CurrentLine;
            string line = document[lineIndex];
            int start = view.CurrentColumn;
            if (start >= line.Length)
            {
                if (view.CurrentLine >= document.Count - 1)
                    return;

                string nextLine = document[view.CurrentLine + 1];
                document[view.CurrentLine] = nextLine;
                document.RemoveAt(view.CurrentLine + 1);
            }
            else
            {
                string before = line[..start];
                string after = line[(start + 1)..];
                document[lineIndex] = before + after;
            }
        }

        private static void HandleHome(SubmissionView view) => view.CurrentColumn = 0;

        private static void HandleEnd(ObservableCollection<string> document, SubmissionView view) => view.CurrentColumn = document[view.CurrentLine].Length;

        private static void HandleTab(ObservableCollection<string> document, SubmissionView view)
        {
            const int TabWidth = 4;
            int start = view.CurrentColumn;
            int remainingSpaces = TabWidth - start % TabWidth;
            string line = document[view.CurrentLine];
            document[view.CurrentLine] = line.Insert(start, new string(' ', remainingSpaces));
            view.CurrentColumn += remainingSpaces;
        }

        private static void HandleTyping(ObservableCollection<string> document, SubmissionView view, string text)
        {
            int lineIndex = view.CurrentLine;
            int start = view.CurrentColumn;
            document[lineIndex] = document[lineIndex].Insert(start, text);
            view.CurrentColumn += text.Length;
        }

        protected virtual void RenderLine(string line) => Console.Write(line);

        protected virtual void EvaluateMetaCommand(string input)
        {
            List<string> args = [];
            int position = 1;
            bool inQuotes = false;
            StringBuilder sb = new();
            while (position < input.Length)
            {
                char c = input[position];
                char l = position + 1 >= input.Length ? '\0' : input[position + 1];

                if (char.IsWhiteSpace(c))
                {
                    if (!inQuotes)
                        CommitPendingArg();
                    else
                        sb.Append(c);
                }
                else if (c == '"')
                {
                    if (!inQuotes)
                        inQuotes = true;
                    else if (l == '"')
                    {
                        sb.Append(c);
                        ++position;
                    }
                    else
                        inQuotes = false;
                }
                else
                    sb.Append(c);

                ++position;
            }

            CommitPendingArg();
            void CommitPendingArg()
            {
                string arg = sb.ToString();
                if (!string.IsNullOrWhiteSpace(arg))
                    args.Add(arg);
                sb.Clear();
            }

            string? cmdName = args.FirstOrDefault();
            if (args.Count != 0)
                args.RemoveAt(0);

            MetaCommand? cmd = _metaCmds.SingleOrDefault(cmd => cmd.Name == cmdName);
            if (cmd is null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Invalid command \"{input}\".");
                Console.ResetColor();
                return;
            }

            ParameterInfo[] parameters = cmd.Method.GetParameters();
            if (args.Count != parameters.Length)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Invalid number of arguments; expected \"{parameters.Length}\" - got \"{args.Count}\".");
                Console.WriteLine($"Usage: \"#{cmd.Name} {string.Join(", ", parameters.Select(p => $"<{p.Name}>"))}\"");
                Console.ResetColor();
                return;
            }

            Repl? instance = cmd.Method.IsStatic ? null : this;
            cmd.Method.Invoke(instance, [.. args]);
            Console.Out.WriteLine();
        }

        protected abstract bool IsCompleteSubmission(string text);

        protected abstract void EvaluateSubmission(string text);

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        protected sealed class MetaCommandAttribute(string name, string description) : Attribute
        {
            public string Name { get; } = name;
            public string Description { get; } = description;
        }

        private sealed class MetaCommand(string name, string description, MethodInfo method)
        {
            public string Name { get; } = name;
            public string Description { get; } = description;
            public MethodInfo Method { get; } = method;
        }

        [MetaCommand("help", "Shows help")]
        protected void EvaluateHelp()
        {
            int maxNameLength = _metaCmds.Max(cmd => cmd.Name.Length);
            foreach (MetaCommand metaCmd in _metaCmds.OrderBy(mc => mc.Name))
            {
                ParameterInfo[] metaParams = metaCmd.Method.GetParameters();
                if (metaParams.Length == 0)
                {
                    Console.Out.WritePunctuation("#");
                    Console.Out.WriteIdentifier(metaCmd.Name.PadRight(maxNameLength));
                }
                else
                {
                    Console.Out.WritePunctuation("#");
                    Console.Out.WriteIdentifier(metaCmd.Name);
                    foreach (ParameterInfo p in metaParams)
                    {
                        Console.Out.WriteSpace();
                        Console.Out.WritePunctuation("<");
                        Console.Out.WriteIdentifier(p.Name!);
                        Console.Out.WritePunctuation(">");
                    }

                    Console.Out.WriteLine();
                    Console.Out.WriteSpace();
                    for (int _ = 0; _ < maxNameLength; ++_)
                        Console.Out.WriteSpace();
                }

                Console.Out.WriteSpace();
                Console.Out.WriteSpace();
                Console.Out.WriteSpace();
                Console.Out.WritePunctuation(metaCmd.Description);
                Console.Out.WriteLine();
            }
        }
    }

    internal sealed class DeltaRepl : Repl
    {
        private Compilation? _previous = null;
        private bool _showTree = false, _showProgram = false;
        private readonly Dictionary<VarSymbol, object?> _vars = [];

        protected override void RenderLine(string line)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(SourceText.From(line, "<stdin>"));
            Lexer lexer = new(syntaxTree);
            ImmutableArray<Token> tokens = lexer.Lex();
            foreach (Token token in tokens)
            {
                if (token.Kind == NodeKind.Number || token.Kind == NodeKind.True || token.Kind == NodeKind.False)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else if (Utility.Keywords.ContainsKey(token.Lexeme))
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                else if (token.Kind == NodeKind.Identifier)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else if (token.Kind == NodeKind.String)
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                Console.Write(token.Lexeme);
                Console.ResetColor();
            }
        }

        protected override void EvaluateSubmission(string text)
        {
            SyntaxTree syntaxTree = SyntaxTree.Parse(SourceText.From(text, "<stdin>"));
            Compilation compilation = Compilation.Create(_previous, syntaxTree);
            if (_showTree)
            {
                syntaxTree.Root.WriteTo(Console.Out);
                Console.WriteLine();
            }

            if (_showProgram)
            {
                compilation.EmitTree(Console.Out);
                Console.WriteLine();
            }

            try
            {
                EvaluationResult result = compilation.Evaluate(_vars);
                if (!result.Diagnostics.Any())
                    _previous = compilation;
                else
                    Console.Out.WriteDiagnostics(result.Diagnostics);
                Console.Out.WriteLine();
            }
            catch (RuntimeException ex)
            {
                Console.Out.SetForeground(ConsoleColor.DarkRed);
                Console.Out.WriteLine(ex);
                Console.Out.ResetColor();
                Console.Out.WriteLine();
            }
        }

        protected override bool IsCompleteSubmission(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            if (text.Split(Environment.NewLine).Reverse().Where(string.IsNullOrEmpty).Take(2).Count() == 2)
                return true;

            SyntaxTree syntaxTree = SyntaxTree.Parse(SourceText.From(text, "<stdin>"));
            return !syntaxTree.Diagnostics.Any();
        }

        [MetaCommand("tree", "Shows the abstract syntax tree")]
        public void EvaluateTree() => Console.WriteLine($"{((_showTree = !_showTree) ? "Enabled" : "Disabled")} showing tree.\n");

        [MetaCommand("program", "Shows the bound program")]
        public void EvaluateProgram() => Console.WriteLine($"{((_showProgram = !_showProgram) ? "Enabled" : "Disabled")} showing program.\n");
    }
}