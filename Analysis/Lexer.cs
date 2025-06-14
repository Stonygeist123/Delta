namespace Delta.Analysis
{
    internal class Lexer(string _source)
    {
        private int _current = 0;
        private int _start = 0;
        private readonly List<Token> _tokens = [];

        public List<Token> Lex()
        {
            while (!IsAtEnd())
            {
                _start = _current;
                GetToken();
            }

            return _tokens;
        }

        private void GetToken()
        {
            char c = Current();
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

                case ' ':
                case '\t':
                case '\n':
                    ++_current;
                    return;

                default:
                    if (char.IsDigit(c))
                    {
                        while (char.IsDigit(Current()))
                            ++_current;
                        _tokens.Add(new Token(NodeKind.Number, Lexeme(), _start, _current));
                    }
                    else
                        AddToken(NodeKind.Bad);
                    break;
            }
        }

        private void AddToken(NodeKind kind)
        {
            ++_current;
            _tokens.Add(new Token(kind, Lexeme(), _start, _current));
        }

        private bool IsAtEnd() => _current >= _source.Length;

        private char Current() => IsAtEnd() ? '\0' : _source[_current];

        private string Lexeme() => _source[_start.._current];
    }
}