namespace Delta.Analysis
{
    internal class Lexer(string source)
    {
        private readonly string _source = source;
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
                    AddToken(TokenKind.Plus);
                    break;

                case '-':
                    AddToken(TokenKind.Minus);
                    break;

                case '*':
                    AddToken(TokenKind.Star);
                    break;

                case '/':
                    AddToken(TokenKind.Slash);
                    break;

                default:
                    if (char.IsDigit(c))
                    {
                        while (char.IsDigit(Current()))
                            ++_current;
                        AddToken(TokenKind.Number, false);
                    }
                    else
                        AddToken(TokenKind.Bad);
                    break;
            }
        }

        private void AddToken(TokenKind kind, bool advance = true)
        {
            if (advance)
                ++_current;
            _tokens.Add(new Token(kind, Lexeme(), _start, _current));
        }

        private bool IsAtEnd() => _current >= _source.Length;

        private char Current() => IsAtEnd() ? '\0' : _source[_current];

        private string Lexeme() => _source[_start.._current];
    }
}