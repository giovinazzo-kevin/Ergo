using Ergo.Facade;
using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Parser;

namespace Ergo.Lang;

public partial class ErgoParser : IDisposable
{
    private InstantiationContext _discardContext;

    public readonly ErgoLexer Lexer;
    public readonly ErgoFacade Facade;

    protected Dictionary<Type, IAbstractTermParser> AbstractTermParsers { get; private set; } = new();

    internal ErgoParser(ErgoFacade facade, ErgoLexer lexer)
    {
        Facade = facade;
        Lexer = lexer;
        _discardContext = new(string.Empty);
    }

    public bool RemoveAbstractParser<T>(out IAbstractTermParser<T> parser)
        where T : IAbstractTerm
    {
        parser = default;
        if (!AbstractTermParsers.Remove(typeof(T), out var parser_))
            return false;
        parser = (IAbstractTermParser<T>)parser_;
        return true;
    }
    public void AddAbstractParser<T>(IAbstractTermParser<T> parser)
        where T : IAbstractTerm => AbstractTermParsers.Add(typeof(T), parser);

    public Maybe<IEnumerable<Operator>> GetOperatorsFromFunctor(Atom functor)
    {
        var match = Lexer.AvailableOperators
            .Where(op => op.Synonyms.Any(s => functor.Equals(s)));
        if (!match.Any())
        {
            return default;
        }

        return Maybe.Some(match);
    }

    public Maybe<T> Abstract<T>()
        where T : IAbstractTerm => Abstract(typeof(T)).Select(x => (T)x);

    public Maybe<IAbstractTerm> Abstract(Type type)
    {
        if (!AbstractTermParsers.TryGetValue(type, out var parser))
            return default;
        return parser.Parse(this);
    }

    public Maybe<Atom> Atom()
    {
        var pos = Lexer.State;
        return Expect<string>(ErgoLexer.TokenType.String)
                .Select(x => new Atom(x))
            .Or(() => Expect<double>(ErgoLexer.TokenType.Number)
                .Select(x => new Atom(x)))
            .Or(() => Expect<string>(ErgoLexer.TokenType.Keyword, kw => ErgoLexer.BooleanSymbols.Contains(kw))
                .Select(x => new Atom(ErgoLexer.TrueSymbols.Contains(x))))
            .Or(() => Expect<string>(ErgoLexer.TokenType.Keyword, kw => ErgoLexer.CutSymbols.Contains(kw))
                .Select(x => WellKnown.Literals.Cut))
            .Or(() => Expect<string>(ErgoLexer.TokenType.Term)
                .Where(x => IsAtomIdentifier(x))
                .Select(x => new Atom(x)))
            .Or(() => Fail<Atom>(pos))
            ;

    }
    public Maybe<Variable> Variable()
    {
        var pos = Lexer.State;
        return Expect<string>(ErgoLexer.TokenType.Term)
            .Where(term => IsVariableIdentifier(term))
            .Map(term => Maybe.Some(term)
                .Where(term => !term.StartsWith("__K"))
                .Do(none: () => Throw(pos, ErrorType.TermHasIllegalName, term))
                .Where(term => !term.Equals(WellKnown.Literals.Discard.Explain()))
                .Or(() => $"_{_discardContext.VarPrefix}{_discardContext.GetFreeVariableId()}"))
            .Select(t => new Variable(t))
            .Or(() => Fail<Variable>(pos))
            ;
    }

    public Maybe<Complex> Complex()
    {
        var pos = Lexer.State;
        return Atom()
            .Map(functor => Abstract<NTuple>()
                .Select(args => new Complex(functor, args.Contents.ToArray())))
            .Or(() => Fail<Complex>(pos))
            ;
    }

    public Maybe<ITerm> ExpressionOrTerm()
    {
        var pos = Lexer.State;
        return Expression()
            .Select<ITerm>(e => e.Complex)
            .Or(() => Term())
            .Or(() => Fail<ITerm>(pos))
            ;
    }

    public Maybe<ITerm> Term()
    {
        var pos = Lexer.State;
        return
            Parenthesized("(", ")", () => Expression())
                .Select<ITerm>(x => x.Complex.AsParenthesized(true))
            .Or(() => Parenthesized("(", ")", () => Inner())
                .Select(x => x.AsParenthesized(true)))
            .Or(() => Inner())
            .Or(() => Fail<ITerm>(pos))
            ;

        Maybe<ITerm> Inner()
        {
            var pos = Lexer.State;
            var primary = () => Variable().Select(x => (ITerm)x)
                .Or(() => Complex().Select(x => (ITerm)x))
                .Or(() => Atom().Select(x => (ITerm)x))
                .Or(() => Fail<ITerm>(pos));
            if (AbstractTermParsers.Values.Any())
            {
                var parsers = AbstractTermParsers.Values.ToArray();
                var abstractFold = parsers.Skip(1)
                    .Aggregate(parsers.First().Parse(this).Or(() => Fail<IAbstractTerm>(pos)),
                        (a, b) => a.Or(() => b.Parse(this).Or(() => Fail<IAbstractTerm>(pos))))
                    .Select(x => x.CanonicalForm);
                return abstractFold
                    .Or(primary);
            }

            return primary();
        }
    }

    public Maybe<Operator> ExpectOperator(Func<Operator, bool> match)
    {
        return Expect<string>(ErgoLexer.TokenType.Operator)
            .Map(str => GetOperatorsFromFunctor(new Atom(str)))
            .Where(ops => ops.Any(match))
            .Select(ops => ops.Single(match));
    }

    public Expression BuildExpression(Operator op, ITerm lhs, Maybe<ITerm> maybeRhs = default, bool exprParenthesized = false)
    {
        return maybeRhs
            .Select(rhs => Associate(lhs, rhs))
            .GetOr(new Expression(op, lhs, Maybe<ITerm>.None, lhs.IsParenthesized || exprParenthesized));

        Expression Associate(ITerm lhs, ITerm rhs)
        {
            // When the lhs represents an expression with the same precedence as this (and thus associativity, by design)
            // and right associativity, we have to swap the arguments around until they look right.
            if (!lhs.IsParenthesized
            && TryConvertExpression(lhs, out var lhsExpr, exprParenthesized)
            && lhsExpr.Operator.Affix == OperatorAffix.Infix
            && lhsExpr.Operator.Associativity == OperatorAssociativity.Right
            && lhsExpr.Operator.Precedence == op.Precedence)
            {
                // a, b, c -> ','(','(','(a, b), c)) -> ','(a, ','(b, ','(c))
                var lhsRhs = lhsExpr.Right.GetOrThrow(new InvalidOperationException());
                var newRhs = BuildExpression(lhsExpr.Operator, lhsRhs, Maybe.Some(rhs), exprParenthesized);
                return BuildExpression(op, lhsExpr.Left, Maybe.Some<ITerm>(newRhs.Complex), exprParenthesized);
            }
            // Special case for comma-lists: always parse them as NTuples
            if (WellKnown.Operators.Conjunction.Equals(op))
            {
                var list = (Complex)new NTuple(new[] { lhs, rhs }).CanonicalForm;
                return new Expression(list);
            }
            return new Expression(op, lhs, Maybe.Some(rhs), exprParenthesized);
        }

        bool TryConvertExpression(ITerm t, out Expression expr, bool exprParenthesized = false)
        {
            expr = default;
            if (t is not Complex cplx)
                return false;
            if (!GetOperatorsFromFunctor(cplx.Functor).TryGetValue(out var ops))
                return false;
            var op = ops.Where(op => cplx.Arity switch
            {
                1 => op.Affix != OperatorAffix.Infix,
                _ => op.Affix == OperatorAffix.Infix
            }).MinBy(x => x.Precedence);
            if (cplx.Arguments.Length == 1)
            {
                expr = BuildExpression(op, cplx.Arguments[0], exprParenthesized: exprParenthesized);
                return true;
            }

            if (cplx.Arguments.Length == 2)
            {
                expr = BuildExpression(op, cplx.Arguments[0], Maybe.Some(cplx.Arguments[1]), exprParenthesized: exprParenthesized);
                return true;
            }

            return false;
        }
    }

    public Maybe<Expression> Prefix()
    {
        var pos = Lexer.State;
        return ExpectOperator(op => op.Affix == OperatorAffix.Prefix)
            .Map(op => Term()
                .Select(arg => BuildExpression(op, arg, exprParenthesized: arg.IsParenthesized)))
            .Or(() => Fail<Expression>(pos));
    }

    public Maybe<Expression> Postfix()
    {
        var pos = Lexer.State;
        return Term()
            .Map(arg => ExpectOperator(op => op.Affix == OperatorAffix.Postfix)
                .Select(op => BuildExpression(op, arg, exprParenthesized: arg.IsParenthesized)))
            .Or(() => Fail<Expression>(pos))
            ;
    }

    public Maybe<Expression> Expression()
    {
        var pos = Lexer.State;
        if (Primary().TryGetValue(out var lhs))
        {
            if (WithMinPrecedence(lhs, 0).TryGetValue(out var expr))
            {
                return Maybe.Some(expr);
            }
            // Special case for unary expressions
            if (lhs is not Complex cplx
                || cplx.Arguments.Length > 1
                || !GetOperatorsFromFunctor(cplx.Functor).TryGetValue(out var ops))
            {
                return Fail<Expression>(pos);
            }

            var op = ops.Single(op => op.Affix != OperatorAffix.Infix);
            expr = BuildExpression(op, cplx.Arguments[0], Maybe<ITerm>.None);
            return Maybe.Some(expr);
        }

        return Fail<Expression>(pos);

        Maybe<Expression> WithMinPrecedence(ITerm lhs, int minPrecedence)
        {
            var pos = Lexer.State;
            if (!PeekNextOperator().TryGetValue(out var lookahead))
            {
                return Fail<Expression>(pos);
            }

            if (lookahead.Affix != OperatorAffix.Infix || lookahead.Precedence < minPrecedence)
            {
                return Fail<Expression>(pos);
            }

            var expr = default(Expression);
            while (lookahead.Affix == OperatorAffix.Infix && lookahead.Precedence >= minPrecedence)
            {
                Lexer.ReadNext();
                var op = lookahead;

                if (!Primary().TryGetValue(out var rhs))
                {
                    return Fail<Expression>(pos);
                }

                if (!PeekNextOperator().TryGetValue(out lookahead))
                {
                    expr = BuildExpression(op, lhs, Maybe.Some(rhs));
                    break;
                }

                while (lookahead.Affix == OperatorAffix.Infix && lookahead.Precedence > op.Precedence
                    || lookahead.Associativity == OperatorAssociativity.Right && lookahead.Precedence == op.Precedence)
                {
                    if (!WithMinPrecedence(rhs, op.Precedence + 1).TryGetValue(out var newRhs))
                    {
                        break;
                    }

                    rhs = newRhs.Complex;
                    if (!PeekNextOperator().TryGetValue(out lookahead))
                    {
                        break;
                    }
                }

                lhs = (expr = BuildExpression(op, lhs, Maybe.Some(rhs))).Complex;
            }

            return expr;
        }

        Maybe<ITerm> Primary()
        {
            var pos = Lexer.State;
            return Prefix().Select<ITerm>(p => p.Complex)
                .Or(() => Postfix().Select<ITerm>(p => p.Complex))
                .Or(() => Term())
                .Or(() => Fail<ITerm>(pos));
        }

        Maybe<Operator> PeekNextOperator()
        {
            return Lexer.PeekNext()
                .Map(lookahead => GetOperatorsFromFunctor(new Atom(lookahead.Value))
                    .Select(ops => ops.Where(op => op.Affix == OperatorAffix.Infix).MinBy(x => x.Precedence)));
        }
    }

    public Maybe<Directive> Directive()
    {
        var pos = Lexer.State;
        if (Expect<string>(ErgoLexer.TokenType.Comment, p => p.StartsWith(":")).TryGetValue(out var desc))
        {
            desc = desc[1..].TrimStart();
            while (Expect<string>(ErgoLexer.TokenType.Comment, p => p.StartsWith(":")).TryGetValue(out var newDesc))
            {
                if (!string.IsNullOrEmpty(newDesc))
                {
                    desc += "\n" + newDesc[1..].TrimStart();
                }
            }
        }

        if (Lexer.Eof)
            return Fail<Directive>(pos);

        desc ??= " ";
        return Expression()
            .Where(op => WellKnown.Operators.UnaryHorn.Equals(op.Operator))
            .Map(op => ExpectDelimiter(p => p.Equals("."))
                .Do(none: () => Throw(pos, ErrorType.UnterminatedClauseList))
                .Select(_ => op))
            .Select(op => new Directive(op.Left, desc))
            .Or(() => Fail<Directive>(pos))
            ;
    }

    public Maybe<Predicate> Predicate()
    {
        var pos = Lexer.State;
        if (Expect<string>(ErgoLexer.TokenType.Comment, p => p.StartsWith(":")).TryGetValue(out var desc))
        {
            desc = desc[1..].TrimStart();
            while (Expect<string>(ErgoLexer.TokenType.Comment, p => p.StartsWith(":")).TryGetValue(out var newDesc))
            {
                if (!string.IsNullOrEmpty(newDesc))
                {
                    desc += "\n" + newDesc[1..].TrimStart();
                }
            }
        }

        if (Lexer.Eof)
            return Fail<Predicate>(pos);

        desc ??= " ";
        return Expression()
            .Map(op => Maybe.Some(op)
                .Where(op => WellKnown.Operators.BinaryHorn.Equals(op.Operator))
                .Or(() => new Expression(WellKnown.Operators.BinaryHorn, op.Complex, Maybe.Some<ITerm>(WellKnown.Literals.True), false)))
            .Map(op => Maybe.Some(op.Right.GetOrThrow(new InvalidOperationException()))
                .Map(rhs => NTuple.FromPseudoCanonical(rhs, default, hasEmptyElement: false)
                    .Or(() => new NTuple(new[] { rhs })))
                .Select(body => (head: op.Left, body)))
            .Or(() => Term()
                .Select(head => (head, body: new NTuple(new ITerm[] { WellKnown.Literals.True }))))
            .Do(none: () => Throw(pos, ErrorType.ExpectedClauseList))
            .Map(x => MakePredicate(pos, desc, x.head, x.body))
            .Map(p => ExpectDelimiter(p => p.Equals("."))
                .Do(none: () => Throw(pos, ErrorType.UnterminatedClauseList))
                .Select(_ => p))
            ;

        Maybe<Predicate> MakePredicate(ErgoLexer.StreamState pos, string desc, ITerm head, NTuple body)
        {
            var headVars = head.Variables
                .Where(v => !v.Equals(WellKnown.Literals.Discard));
            var bodyVars = body.Contents.SelectMany(t => t.Variables)
                .Distinct();
            var singletons = headVars.Where(v => !v.Ignored && !bodyVars.Contains(v) && headVars.Count(x => x.Name == v.Name) == 1)
                .Select(v => v.Explain());
            if (singletons.Any())
            {
                Throw(pos, ErrorType.PredicateHasSingletonVariables, head.GetSignature().Explain(), singletons.Join());
            }

            return new Predicate(desc, WellKnown.Modules.User, head, body, false, false);
        }
    }

    public Maybe<ErgoProgram> Program()
    {
        var directives = new List<Directive>();
        var predicates = new List<Predicate>();
        while (Directive().TryGetValue(out var directive))
        {
            directives.Add(directive);
        }

        while (Predicate().TryGetValue(out var predicate))
        {
            predicates.Add(predicate);
        }

        var moduleArgs = directives.Single(x => x.Body.GetFunctor().GetOr(default).Equals(new Atom("module")))
            .Body.GetArguments();

        if (moduleArgs.Length < 2 || !moduleArgs[1].IsAbstract<List>().TryGetValue(out var exported))
        {
            exported = List.Empty;
        }

        var exportedPredicates = predicates.Select(p =>
        {
            var sign = p.Head.GetSignature();
            var form = new Complex(WellKnown.Functors.Arity.First(), sign.Functor, new Atom((decimal)sign.Arity.GetOrThrow(new NotSupportedException())))
                .AsOperator(OperatorAffix.Infix);
            if (exported.Contents.Any(x => x.Equals(form)))
                return p.Exported();
            return p;
        });
        return new ErgoProgram(directives.ToArray(), exportedPredicates.ToArray())
            .AsPartial(false);
    }

    public IEnumerable<Operator> OperatorDeclarations()
    {
        var pos = Lexer.State;
        var moduleName = WellKnown.Modules.Stdlib;
        var ret = new List<Operator>();
        try
        {
            while (Directive().TryGetValue(out var directive))
            {
                if (directive.Body is not Complex cplx)
                    continue;
                if (cplx.Functor.Equals(new Atom("module")))
                    moduleName = (Atom)cplx.Arguments[0];

                if (cplx.Functor.Equals(new Atom("op")))
                {
                    if (!cplx.Arguments[0].Matches<int>(out var precedence))
                        continue;
                    if (!cplx.Arguments[1].Matches<OperatorType>(out var type))
                        continue;
                    if (!cplx.Arguments[2].IsAbstract<List>().TryGetValue(out var syns))
                        continue;
                    ret.Add(new(moduleName, type, precedence, syns.Contents.Cast<Atom>().ToHashSet()));
                }
            }
        }
        catch
        {
            // The parser reached a point where a newly-declared operator was used. Probably.
        }

        Lexer.Seek(pos);
        return ret;
    }

    public Maybe<ErgoProgram> ProgramDirectives()
    {
        var ret = true;
        var directives = new List<Directive>();
        try
        {
            while (Directive().TryGetValue(out var directive))
            {
                directives.Add(directive);
            }
        }
        catch (LexerException le) when (le.ErrorType == ErgoLexer.ErrorType.UnrecognizedOperator)
        {
            // The parser reached a point where a newly-declared operator was used. Probably.
        }

        if (!ret)
            return default;
        return new ErgoProgram(directives.ToArray(), Array.Empty<Predicate>())
            .AsPartial(true);
    }

    public void Dispose()
    {
        Lexer.Dispose();
        GC.SuppressFinalize(this);
    }
}
