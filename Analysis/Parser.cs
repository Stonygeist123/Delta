using Delta.Analysis.Nodes;
using Delta.Diagnostics;
using System.Collections.Immutable;

namespace Delta.Analysis
{
    internal class Parser
    {
        private int _current = 0;
        private readonly DiagnosticBag _diagnostics = [];
        private readonly ImmutableArray<Token> _tokens;
        public DiagnosticBag Diagnostics => _diagnostics;
        private readonly SyntaxTree _syntaxTree;

        public Parser(SyntaxTree syntaxTree)
        {
            Lexer lexer = new(syntaxTree);
            _tokens = [.. lexer.Lex().Where(t => t.Kind != NodeKind.Space)];
            _diagnostics.AddRange(lexer.Diagnostics);
            _syntaxTree = syntaxTree;
        }

        public ImmutableArray<Stmt> Parse()
        {
            ImmutableArray<Stmt>.Builder stmts = ImmutableArray.CreateBuilder<Stmt>();
            while (!IsAtEnd())
                stmts.Add(ParseStmt());
            return stmts.ToImmutable();
        }

        public CompilationUnit ParseCompilationUnit()
        {
            ImmutableArray<MemberNode> members = ParseMembers();
            Token eofToken = Advance();
            if (eofToken.Kind != NodeKind.EOF)
                _diagnostics.Report(eofToken.Location, "Expected end of file.");
            return new(_syntaxTree, members, eofToken);
        }

        private ImmutableArray<MemberNode> ParseMembers()
        {
            ImmutableArray<MemberNode>.Builder members = ImmutableArray.CreateBuilder<MemberNode>();
            while (!IsAtEnd())
            {
                Token start = Current;
                if (start.Kind == NodeKind.Fn)
                    members.Add(ParseFnDecl());
                else if (start.Kind == NodeKind.Class)
                    members.Add(ParseClassDecl());
                else
                    members.Add(ParseStmt());
                if (Current == start)
                    Advance();
            }

            return members.ToImmutable();
        }

        private MemberNode ParseFnDecl(bool inClass = false)
        {
            Token? accessibility = inClass ? (Current.Kind == NodeKind.Pub || Current.Kind == NodeKind.Priv ? Advance() : null) : null;
            if (accessibility?.Kind == NodeKind.Priv)
                _diagnostics.Report(accessibility.Location, $"The constructor cannot be private.");

            Token keyword = Advance();
            Token? name = Current;
            if (inClass)
            {
                if (name.Kind != NodeKind.Identifier)
                    name = null;
                else
                    name = Advance();
            }
            else if (!Match(NodeKind.Identifier))
                return new ErrorStmt(_syntaxTree, keyword, name);

            ParameterList? parameters = null;
            Token lParen = Current;
            if (lParen.Kind == NodeKind.LParen)
            {
                ++_current;
                List<Param> paramList = [];
                while (Current.Kind != NodeKind.RParen && !IsAtEnd())
                {
                    Token? comma = null;
                    if (paramList.Count > 0)
                    {
                        comma = Advance();
                        if (comma.Kind != NodeKind.Comma)
                            _diagnostics.Report(comma.Location, "Expected ',' between parameters.");
                    }

                    Token paramName = Current;
                    if (!Match(NodeKind.Identifier))
                        return new ErrorStmt(_syntaxTree, [keyword, name, lParen, .. paramList, comma, paramName]);
                    TypeClause typeClause = ParseTypeClause(NodeKind.Colon);
                    Param param = new(_syntaxTree, comma, paramName, typeClause);
                    paramList.Add(param);
                }

                Token rParen = Current;
                if (!Match(NodeKind.RParen))
                    return new ErrorStmt(_syntaxTree, [keyword, name, lParen, .. paramList, rParen]);
                parameters = new ParameterList(_syntaxTree, lParen, paramList, rParen);
            }

            TypeClause? returnType = null;
            if (name is not null)
                returnType = ParseTypeClause(NodeKind.Arrow);
            Stmt ParseExprStmt()
            {
                Expr expr = ParseExpr();
                Token semicolon = Current;
                if (semicolon.Kind != NodeKind.Semicolon)
                {
                    _diagnostics.Report(semicolon.Location, $"Function body has to be a block or an expression ending with ';'.");
                    return new ErrorStmt(_syntaxTree, expr, semicolon);
                }

                return new ExprStmt(_syntaxTree, expr, semicolon);
            }

            Stmt body = Current.Kind == NodeKind.LBrace ? ParseBlockStmt() : ParseExprStmt();
            if (inClass)
                return name is null ? new CtorDecl(_syntaxTree, accessibility, keyword, parameters, body) : new MethodDecl(_syntaxTree, accessibility, keyword, name, parameters, returnType!, body);
            else
                return new FnDecl(_syntaxTree, keyword, name!, parameters, returnType!, body);
        }

        private MemberNode ParseClassDecl()
        {
            Token keyword = Advance();
            Token name = Current;
            if (!Match(NodeKind.Identifier))
                return new ErrorStmt(_syntaxTree, keyword, name);
            Token lBrace = Current;
            if (!Match(NodeKind.LBrace))
                return new ErrorStmt(_syntaxTree, keyword, name, lBrace);

            ImmutableArray<PropertyDecl>.Builder properties = ImmutableArray.CreateBuilder<PropertyDecl>();
            ImmutableArray<MethodDecl>.Builder methods = ImmutableArray.CreateBuilder<MethodDecl>();
            CtorDecl? ctorDecl = null;
            while (Current.Kind != NodeKind.RBrace && !IsAtEnd())
            {
                Token? accessibility = Current;
                if (accessibility.Kind != NodeKind.Pub && accessibility.Kind != NodeKind.Priv)
                    accessibility = null;
                else
                    ++_current;

                if (Current.Kind == NodeKind.Fn)
                {
                    MemberNode member = ParseFnDecl(true);
                    if (member is MethodDecl method)
                        methods.Add(method);
                    else if (member is CtorDecl)
                        ctorDecl = member as CtorDecl;
                }
                else if (Current.Kind == NodeKind.Identifier)
                {
                    Token? mutToken = null;
                    if (Current.Kind == NodeKind.Mut)
                        mutToken = Advance();

                    Token varName = Current;
                    if (!Match(NodeKind.Identifier))
                        return new ErrorStmt(_syntaxTree, keyword, name);

                    TypeClause? typeClause = ParseOptType();
                    Token eqToken = Current;
                    if (!Match(NodeKind.Eq))
                        return new ErrorStmt(_syntaxTree, keyword, name, eqToken);

                    Expr value = ParseExpr();
                    Token semicolon = Advance();
                    if (semicolon.Kind != NodeKind.Semicolon)
                        _diagnostics.Report(semicolon.Location, $"Expected ';' after property declaration.");

                    PropertyDecl property = new(_syntaxTree, accessibility, mutToken, varName, typeClause, eqToken, value, semicolon);
                    properties.Add(property);
                }
                else
                    _diagnostics.Report(Advance().Location, $"Expected property or method declaration.");
            }

            Token rBrace = Current;
            if (!Match(NodeKind.RBrace))
                return new ErrorStmt(_syntaxTree, keyword, name, lBrace, rBrace);
            return new ClassDecl(_syntaxTree, keyword, name, lBrace, properties.ToImmutable(), methods.ToImmutable(), ctorDecl, rBrace);
        }

        public Stmt ParseStmt()
        {
            switch (Current.Kind)
            {
                case NodeKind.LBrace:
                    return ParseBlockStmt();

                case NodeKind.Var:
                    return ParseVarDecl();

                case NodeKind.If:
                {
                    Token keyword = Advance();
                    Expr? condition = Current.Kind == NodeKind.LBrace ? null : ParseExpr();
                    Stmt body = ParseStmt();
                    if (Current.Kind == NodeKind.Else)
                    {
                        Token elseToken = Advance();
                        Stmt elseClause = ParseStmt();
                        return new IfStmt(_syntaxTree, keyword, condition, body, new ElseStmt(_syntaxTree, elseToken, elseClause));
                    }

                    return new IfStmt(_syntaxTree, keyword, condition, body);
                }

                case NodeKind.Loop:
                {
                    Token keyword = Advance();
                    Expr? condition = Current.Kind == NodeKind.LBrace ? null : ParseExpr();
                    Stmt body = ParseStmt();
                    return new LoopStmt(_syntaxTree, keyword, condition, body);
                }

                case NodeKind.For:
                {
                    Token keyword = Advance();
                    Token varName = Current;
                    if (!Match(NodeKind.Identifier))
                        return new ErrorStmt(_syntaxTree, keyword, varName);

                    Token eqToken = Current;
                    if (!Match(NodeKind.Eq))
                        return new ErrorStmt(_syntaxTree, keyword, varName, eqToken);

                    Expr startValue = ParseExpr();
                    Token arrowToken = Current;
                    if (!Match(NodeKind.Arrow))
                        return new ErrorStmt(_syntaxTree, keyword, varName, eqToken, startValue, arrowToken);

                    Expr endValue = ParseExpr();
                    Token? stepToken = Current.Kind == NodeKind.Step ? Advance() : null;
                    Expr? stepValue = stepToken is null ? null : ParseExpr();
                    Stmt body = ParseStmt();
                    return new ForStmt(_syntaxTree, keyword, varName, eqToken, startValue, arrowToken, endValue, stepToken, stepValue, body);
                }

                case NodeKind.Ret:
                {
                    Token keyword = Advance();
                    Expr? value = Current.Kind == NodeKind.Semicolon ? null : ParseExpr();
                    Token semicolon = Advance();
                    if (semicolon.Kind != (NodeKind.Semicolon))
                        _diagnostics.Report(semicolon.Location, "Expected ';' after return statement.");
                    return new RetStmt(_syntaxTree, keyword, value, semicolon);
                }

                case NodeKind.Break:
                {
                    Token keyword = Advance();
                    Token semicolon = Advance();
                    if (semicolon.Kind != (NodeKind.Semicolon))
                        _diagnostics.Report(semicolon.Location, "Expected ';' after break statement.");
                    return new BreakStmt(_syntaxTree, keyword, semicolon);
                }

                case NodeKind.Continue:
                {
                    Token keyword = Advance();
                    Token semicolon = Advance();
                    if (semicolon.Kind != (NodeKind.Semicolon))
                        _diagnostics.Report(semicolon.Location, "Expected ';' after continue statement.");
                    return new ContinueStmt(_syntaxTree, keyword, semicolon);
                }

                default:
                {
                    Expr expr = ParseExpr();
                    Token? semicolon = null;
                    if (Current.Kind == NodeKind.Semicolon)
                        semicolon = Advance();
                    return new ExprStmt(_syntaxTree, expr, semicolon);
                }
            }
        }

        private Stmt ParseBlockStmt()
        {
            Token lBrace = Current;
            if (!Match(NodeKind.LBrace))
                return new ErrorStmt(_syntaxTree, lBrace);

            List<Stmt> stmts = [];
            while (Current.Kind != NodeKind.RBrace && !IsAtEnd())
                stmts.Add(ParseStmt());

            Token rBrace = Current;
            if (!Match(NodeKind.RBrace))
                return new ErrorStmt(_syntaxTree, [lBrace, .. stmts, rBrace]);
            return new BlockStmt(_syntaxTree, lBrace, stmts, rBrace);
        }

        private Stmt ParseVarDecl()
        {
            Token keyword = Advance();
            Token? mutToken = null;
            if (Current.Kind == NodeKind.Mut)
                mutToken = Advance();

            Token name = Current;
            if (!Match(NodeKind.Identifier))
                return new ErrorStmt(_syntaxTree, keyword, name);

            TypeClause? typeClause = ParseOptType();
            Token eqToken = Current;
            if (!Match(NodeKind.Eq))
                return new ErrorStmt(_syntaxTree, keyword, name, eqToken);

            Expr value = ParseExpr();
            Token? semicolon = null;
            if (Current.Kind == NodeKind.Semicolon)
                semicolon = Advance();
            return new VarStmt(_syntaxTree, keyword, mutToken, name, typeClause, eqToken, value, semicolon);
        }

        public Expr ParseExpr(int parentPrecedence = 0)
        {
            Expr expr;
            if (IsAtEnd())
            {
                _diagnostics.Report(Current.Location, "Expected expression.");
                return new ErrorExpr(_syntaxTree, _tokens.Last());
            }

            Token firstToken = Advance();
            switch (firstToken.Kind)
            {
                case NodeKind.Number:
                case NodeKind.String:
                case NodeKind.True:
                case NodeKind.False:
                    expr = new LiteralExpr(_syntaxTree, firstToken);
                    break;

                case NodeKind.Identifier:
                    if (Current.Kind == NodeKind.Eq)
                    {
                        Token eqToken = Advance();
                        Expr value = ParseExpr();
                        expr = new AssignExpr(_syntaxTree, firstToken, eqToken, value);
                    }
                    else
                        expr = new NameExpr(_syntaxTree, firstToken);
                    break;

                case NodeKind.Plus or NodeKind.Minus:
                    Expr operand = ParseExpr(Utility.GetUnOpPrecedence(firstToken.Kind));
                    expr = new UnaryExpr(_syntaxTree, firstToken, operand);
                    break;

                case NodeKind.LParen:
                    expr = ParseExpr();
                    Token rParen = Advance();
                    if (rParen.Kind != NodeKind.RParen)
                    {
                        _diagnostics.Report(rParen.Location, "Expected ')'.");
                        expr = new ErrorExpr(_syntaxTree, firstToken, expr, rParen);
                    }

                    expr = new GroupingExpr(_syntaxTree, firstToken, expr, rParen);
                    break;

                default:
                    _diagnostics.Report(firstToken.Location, "Expected expression.");
                    expr = new ErrorExpr(_syntaxTree, firstToken);
                    break;
            }

            return CheckExtension(expr, parentPrecedence);
        }

        private Expr CheckExtension(Expr expr, int parentPrecedence = 0)
        {
            if (IsAtEnd())
                return expr;

            Token token = Current;
            if (Utility.GetBinOpPrecedence(token.Kind) > 0)
            {
                int precedence = Utility.GetBinOpPrecedence(Current.Kind);
                while (precedence > parentPrecedence && !IsAtEnd())
                {
                    ++_current;
                    expr = new BinaryExpr(_syntaxTree, expr, token, ParseExpr(precedence));
                    if (!IsAtEnd())
                    {
                        token = Current;
                        precedence = Utility.GetBinOpPrecedence(token.Kind);
                    }
                }
            }
            else if (expr is NameExpr n && token.Kind == NodeKind.LParen)
            {
                ++_current;
                List<Arg> args = [];
                while (!IsAtEnd() && Current.Kind != NodeKind.RParen)
                {
                    if (args.Count == 0)
                        args.Add(new(_syntaxTree, null, ParseExpr()));
                    else
                    {
                        Token comma = Advance();
                        if (comma.Kind != NodeKind.Comma)
                            _diagnostics.Report(comma.Location, "Expected ',' between arguments.");
                        args.Add(new(_syntaxTree, comma, ParseExpr()));
                    }
                }

                if (IsAtEnd())
                {
                    _diagnostics.Report(Current.Location, "Expected ')'.");
                    return new ErrorExpr(_syntaxTree, [n, token, .. args]);
                }

                Token rParen = Current;
                if (!Match(NodeKind.RParen))
                    return new ErrorExpr(_syntaxTree, n, rParen);
                expr = new CallExpr(_syntaxTree, n.Name, token, args, rParen);
            }

            return expr;
        }

        private TypeClause? ParseOptType(NodeKind markKind = NodeKind.Colon) => Current.Kind != markKind ? null : ParseTypeClause(markKind);

        private TypeClause ParseTypeClause(NodeKind markKind = NodeKind.Colon)
        {
            Token mark = Current;
            if (!Match(markKind))
                return new TypeClause(_syntaxTree, mark, Current);
            Token type = Advance();
            if (type.Kind != NodeKind.Identifier)
                _diagnostics.Report(Current.Location, $"Expected type name after '{Utility.GetLexeme(markKind)}'.");
            return new TypeClause(_syntaxTree, mark, type);
        }

        private Token Current => _current >= _tokens.Length ? _tokens.Last() : _tokens[_current];

        private Token Advance() => _current >= _tokens.Length ? _tokens.Last() : _tokens[_current++];

        private bool Match(NodeKind kind)
        {
            Token cur = Advance();
            if (cur.Kind == kind)
                return true;
            else
            {
                _diagnostics.Report(cur.Location, $"Expected '{Utility.GetLexeme(kind)}' but found '{Utility.GetLexeme(cur.Kind)}'.");
                return false;
            }
        }

        private bool IsAtEnd() => _current >= _tokens.Length || Current.Kind == NodeKind.EOF;
    }
}