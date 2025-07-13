using Delta.Analysis.Nodes;
using Delta.Diagnostics;
using System.Collections.Immutable;

namespace Delta.Analysis
{
    internal class Lexer(SyntaxTree syntaxTree)
    {
        private readonly SyntaxTree _syntaxTree = syntaxTree;
        private int _current = 0;
        private int _start = 0;
        private readonly ImmutableArray<Token>.Builder _tokens = ImmutableArray.CreateBuilder<Token>();
        private readonly DiagnosticBag _diagnostics = [];
        public DiagnosticBag Diagnostics => _diagnostics;
        public SourceText Src => _syntaxTree.Source;

        public ImmutableArray<Token> Lex()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                GetToken();
            }

            _tokens.Add(new(_syntaxTree, NodeKind.EOF, "\0", new(_current, 1)));
            return _tokens.ToImmutable();
        }

        public void GetToken()
        {
            char c = Current;
            switch (c)
            {
                case '\0':
                    AddToken(NodeKind.EOF);
                    return;

                case '+':
                    AddToken(NodeKind.Plus);
                    break;

                case '-':
                    if (Next == '>')
                    {
                        ++_current;
                        AddToken(NodeKind.Arrow);
                    }
                    else
                        AddToken(NodeKind.Minus);
                    break;

                case '*':
                    AddToken(NodeKind.Star);
                    break;

                case '/':
                    AddToken(NodeKind.Slash);
                    break;

                case '(':
                    AddToken(NodeKind.LParen);
                    break;

                case ')':
                    AddToken(NodeKind.RParen);
                    break;

                case '{':
                    AddToken(NodeKind.LBrace);
                    break;

                case '}':
                    AddToken(NodeKind.RBrace);
                    break;

                case '=':
                    if (Next == '=')
                    {
                        ++_current;
                        AddToken(NodeKind.EqEq);
                    }
                    else
                        AddToken(NodeKind.Eq);
                    break;

                case '!':
                    if (Next == '=')
                    {
                        ++_current;
                        AddToken(NodeKind.NotEq);
                    }
                    else
                        AddToken(NodeKind.Not);
                    break;

                case '>':
                    if (Next == '=')
                    {
                        ++_current;
                        AddToken(NodeKind.GreaterEq);
                    }
                    else
                        AddToken(NodeKind.Greater);
                    break;

                case '<':
                    if (Next == '=')
                    {
                        ++_current;
                        AddToken(NodeKind.LessEq);
                    }
                    else
                        AddToken(NodeKind.Less);
                    break;

                case '&':
                    if (Next == '&')
                    {
                        ++_current;
                        AddToken(NodeKind.And);
                    }
                    else
                    {
                        AddToken(NodeKind.Bad);
                        _diagnostics.Report(new(Src, GetSpan()), $"Unexpected character '&'.");
                    }
                    break;

                case '|':
                    if (Next == '|')
                    {
                        ++_current;
                        AddToken(NodeKind.Or);
                    }
                    else
                    {
                        AddToken(NodeKind.Bad);
                        _diagnostics.Report(new(Src, GetSpan()), $"Unexpected character '|'.");
                    }
                    break;

                case ',':
                    AddToken(NodeKind.Comma);
                    break;

                case ':':
                    AddToken(NodeKind.Colon);
                    break;

                case ';':
                    AddToken(NodeKind.Semicolon);
                    break;

                case '.':
                    AddToken(NodeKind.Dot);
                    break;

                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    AddToken(NodeKind.Space);
                    break;

                case '"':
                    int count = 0;
                    while (count < 2 && !IsAtEnd())
                    {
                        char cur = Current;
                        ++_current;
                        switch (cur)
                        {
                            case '"':
                                ++count;
                                break;

                            case '\n':
                                _diagnostics.Report(new(Src, GetSpan()), "Unterminated string literal.");
                                break;

                            case '\\':
                                if (IsAtEnd())
                                {
                                    _diagnostics.Report(new(Src, GetSpan()), "Unterminated string literal.");
                                    return;
                                }
                                char next = Current;
                                if (next is 'n' or 't' or '\\' or '"' or '\'')
                                    ++_current;
                                else
                                    _diagnostics.Report(new(Src, GetSpan()), $"Invalid escape sequence: \\{next}");
                                break;
                        }
                    }

                    _tokens.Add(new Token(_syntaxTree, NodeKind.String, Lexeme(), GetSpan()));
                    break;

                default:
                    if (char.IsDigit(c))
                    {
                        while (char.IsDigit(Current))
                            ++_current;
                        _tokens.Add(new Token(_syntaxTree, NodeKind.Number, Lexeme(), GetSpan()));
                    }
                    else if (char.IsLetter(c))
                    {
                        while (char.IsLetterOrDigit(Current))
                            ++_current;
                        string lexeme = Lexeme();
                        _tokens.Add(new Token(_syntaxTree, Utility.Keywords.TryGetValue(lexeme, out NodeKind kind) ? kind : NodeKind.Identifier, lexeme, GetSpan()));
                    }
                    else
                    {
                        AddToken(NodeKind.Bad);
                        _diagnostics.Report(new(Src, GetSpan()), $"Unexpected character.");
                    }
                    break;
            }
        }

        private void AddToken(NodeKind kind)
        {
            ++_current;
            _tokens.Add(new Token(_syntaxTree, kind, Lexeme(), GetSpan()));
        }

        private bool IsAtEnd() => _current >= Src.Length;

        private char Current => IsAtEnd() ? '\0' : Src[_current];
        private char Next => _current + 1 >= Src.Length ? '\0' : Src[_current + 1];

        private string Lexeme() => Src[_start.._current];

        private TextSpan GetSpan() => new(_start, _current - _start);
    }
}