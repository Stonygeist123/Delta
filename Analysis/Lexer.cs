using Delta.Analysis.Nodes;
using Delta.Diagnostics;

namespace Delta.Analysis
{
    internal class Lexer(string _src)
    {
        private int _current = 0;
        private int _start = 0;
        private readonly List<Token> _tokens = [];
        private readonly DiagnosticBag _diagnostics = [];
        public DiagnosticBag Diagnostics => _diagnostics;

        public List<Token> Lex()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                GetToken();
            }

            _tokens.Add(new(NodeKind.EOF, Lexeme(), new(_current, _current + 1)));
            return _tokens;
        }

        private void GetToken()
        {
            char c = Current;
            switch (c)
            {
                case '+':
                    AddToken(NodeKind.Plus);
                    break;

                case '-':
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
                        _diagnostics.Add(_src, $"Unexpected character '&'.", GetSpan());
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
                        _diagnostics.Add(_src, $"Unexpected character '|'.", GetSpan());
                    }
                    break;

                case ',':
                    AddToken(NodeKind.Comma);
                    break;

                case ' ':
                case '\t':
                case '\n':
                    ++_current;
                    return;

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
                                _diagnostics.Add(_src, "Unterminated string literal.", GetSpan());
                                break;

                            case '\\':
                                if (IsAtEnd())
                                {
                                    _diagnostics.Add(_src, "Unterminated string literal.", GetSpan());
                                    return;
                                }
                                char next = Current;
                                if (next is 'n' or 't' or '\\' or '"' or '\'')
                                    ++_current;
                                else
                                    _diagnostics.Add(_src, $"Invalid escape sequence: \\{next}", GetSpan());
                                break;
                        }
                    }

                    _tokens.Add(new Token(NodeKind.String, Lexeme(), GetSpan()));
                    break;

                default:
                    if (char.IsDigit(c))
                    {
                        while (char.IsDigit(Current))
                            ++_current;
                        _tokens.Add(new Token(NodeKind.Number, Lexeme(), GetSpan()));
                    }
                    else if (char.IsLetter(c))
                    {
                        while (char.IsLetterOrDigit(Current))
                            ++_current;
                        string lexeme = Lexeme();
                        _tokens.Add(new Token(Utility.Keywords.TryGetValue(lexeme, out NodeKind kind) ? kind : NodeKind.Identifier, lexeme, GetSpan()));
                    }
                    else
                    {
                        AddToken(NodeKind.Bad);
                        _diagnostics.Add(_src, $"Unexpected character.", GetSpan());
                    }
                    break;
            }
        }

        private void AddToken(NodeKind kind)
        {
            ++_current;
            _tokens.Add(new Token(kind, Lexeme(), GetSpan()));
        }

        private bool IsAtEnd() => _current >= _src.Length;

        private char Current => IsAtEnd() ? '\0' : _src[_current];
        private char Next => _current - 1 >= _src.Length ? '\0' : _src[_current + 1];

        private string Lexeme() => _src[_start.._current];

        private TextSpan GetSpan() => new(_start, _current);
    }
}