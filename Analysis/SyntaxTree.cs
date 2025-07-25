﻿using Delta.Analysis.Nodes;
using Delta.Diagnostics;
using System.Collections.Immutable;

namespace Delta.Analysis
{
    internal class SyntaxTree
    {
        private SyntaxTree(SourceText source, ParseHandler handler)
        {
            Source = source;
            handler(this, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics);
            Diagnostics = diagnostics;
            Root = root;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public SourceText Source { get; }
        public CompilationUnit Root { get; }

        private delegate void ParseHandler(SyntaxTree syntaxTree, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics);

        public static SyntaxTree Load(string fileName)
        {
            string text = File.ReadAllText(fileName);
            return Parse(SourceText.From(text, fileName));
        }

        private static void Parse(SyntaxTree syntaxTree, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics)
        {
            Parser parser = new(syntaxTree);
            root = parser.ParseCompilationUnit();
            diagnostics = [.. parser.Diagnostics];
        }

        public static SyntaxTree Parse(string text) => Parse(SourceText.From(text));

        public static SyntaxTree Parse(SourceText source) => new(source, Parse);

        public static ImmutableArray<Token> ParseTokens(string line) => ParseTokens(line, out _);

        public static ImmutableArray<Token> ParseTokens(SourceText source) => ParseTokens(source, out _);

        public static ImmutableArray<Token> ParseTokens(string line, out ImmutableArray<Diagnostic> diagnostics) => ParseTokens(SourceText.From(line), out diagnostics);

        public static ImmutableArray<Token> ParseTokens(SourceText source, out ImmutableArray<Diagnostic> diagnostics)
        {
            ImmutableArray<Token> tokens = [];
            void ParseTokens(SyntaxTree syntaxTree, out CompilationUnit root, out ImmutableArray<Diagnostic> diagnostics)
            {
                Lexer lexer = new(syntaxTree);
                tokens = lexer.Lex();
                root = new(syntaxTree, [], tokens[^2]);
                diagnostics = [.. lexer.Diagnostics];
            }

            SyntaxTree syntaxTree = new(source, ParseTokens);
            diagnostics = syntaxTree.Diagnostics;
            return tokens;
        }
    }
}