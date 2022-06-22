using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Parser;

namespace Ergo.Lang;

public partial class ErgoParser : IDisposable
{
    private InstantiationContext _discardContext;

    public readonly Lexer Lexer;
    protected Dictionary<Type, IAbstractTermParser> AbstractTermParsers { get; private set; } = new();

    public ErgoParser Clone() => new(Lexer) { AbstractTermParsers = new(AbstractTermParsers) };

    public ErgoParser(Lexer lexer)
    {
        Lexer = lexer;
        _discardContext = new(string.Empty);
    }

    public bool TryRemoveAbstractParser<T>()
        where T : IAbstractTerm => AbstractTermParsers.Remove(typeof(T));
    public bool TryAddAbstractParser<T>(AbstractTermParser<T> parser)
        where T : IAbstractTerm
    {
        AbstractTermParsers.Add(typeof(T), parser);
        return true;
    }

    public bool TryGetOperatorsFromFunctor(Atom functor, out IEnumerable<Operator> ops)
    {
        ops = default;
        var match = Lexer.AvailableOperators
            .Where(op => op.Synonyms.Any(s => functor.Equals(s)));
        if (!match.Any())
        {
            return false;
        }

        ops = match;
        return true;
    }

    public bool TryParseAbstract<T>(out T abs)
        where T : IAbstractTerm
    {
        abs = default;
        if (!AbstractTermParsers.TryGetValue(typeof(T), out var parser))
            return false;
        var res = parser.TryParse(this);
        abs = res.Reduce(some => (T)some, () => default);
        return res.HasValue;
    }

    public bool TryParseAtom(out Atom atom)
    {
        atom = default;
        var pos = Lexer.State;
        if (Expect(Lexer.TokenType.String, out string str))
        {
            atom = new Atom(str);
            return true;
        }
        else if (Expect(Lexer.TokenType.Number, out double dec))
        {
            atom = new Atom(dec);
            return true;
        }
        else if (Expect(Lexer.TokenType.Keyword, kw => Lexer.BooleanSymbols.Contains(kw), out string kw))
        {
            atom = new Atom(Lexer.TrueSymbols.Contains(kw));
            return true;
        }
        else if (Expect(Lexer.TokenType.Keyword, kw => Lexer.CutSymbols.Contains(kw), out kw))
        {
            atom = WellKnown.Literals.Cut;
            return true;
        }

        if (Expect(Lexer.TokenType.Term, out string ITerm))
        {
            if (!IsAtomIdentifier(ITerm))
            {
                return Fail(pos);
            }

            atom = new Atom(ITerm);
            return true;
        }

        return Fail(pos);

    }
    public bool TryParseVariable(out Variable var)
    {
        var = default;
        var pos = Lexer.State;
        if (Expect(Lexer.TokenType.Term, out string term))
        {
            if (!IsVariableIdentifier(term))
            {
                return Fail(pos);
            }

            if (term.StartsWith("__K"))
            {
                Throw(pos, ErrorType.TermHasIllegalName, var.Name);
            }

            if (term.Equals(WellKnown.Literals.Discard.Explain()))
            {
                term = $"_{_discardContext.VarPrefix}{_discardContext.GetFreeVariableId()}";
            }

            var = new Variable(term);
            return true;
        }

        return Fail(pos);
    }
    public bool TryParseComplex(out Complex cplx)
    {
        cplx = default;
        var pos = Lexer.State;
        if (!Expect(Lexer.TokenType.Term, out string functor)
            && !Expect(Lexer.TokenType.Operator, out functor)
            && !Expect(Lexer.TokenType.String, out functor))
        {
            return Fail(pos);
        }

        var argParse = new ListParser<NTuple>((h, t) => (new(h)))
            .TryParse(this);
        if (!argParse.HasValue)
            return Fail(pos);

        cplx = new Complex(new Atom(functor), argParse.GetOrThrow().Contents.ToArray());
        return true;
    }
    public bool TryParseTermOrExpression(out ITerm term, out bool parenthesized)
    {
        var pos = Lexer.State;
        if (TryParseExpression(out var expr))
        {
            term = expr.Complex;
            parenthesized = expr.Complex.IsParenthesized;
            if (NTuple.FromQuasiCanonical(term, Maybe.Some(parenthesized), hasEmptyElement: Maybe.Some(false)) is { HasValue: true } tuple)
                term = tuple.GetOrThrow().CanonicalForm;
            return true;
        }

        if (TryParseTerm(out term, out parenthesized))
        {
            return true;
        }

        term = default;
        return Fail(pos);
    }
    public bool TryParseTerm(out ITerm term, out bool parenthesized)
    {
        term = default; parenthesized = false;
        var pos = Lexer.State;
        if (Parenthesized(() => TryParseExpression(out var expr) ? (true, expr) : (false, default), out var expr))
        {
            term = expr.Complex.AsParenthesized(true);
            parenthesized = true;
            return true;
        }

        if (Parenthesized(() => TryParseTerm(out var eval, out _) ? (true, eval) : (false, default), out var eval))
        {
            term = eval;
            parenthesized = true;
            return true;
        }

        if (TryParseTermInner(out var t))
        {
            term = t;
            return true;
        }

        return Fail(pos);

        bool TryParseTermInner(out ITerm ITerm)
        {
            var pos = Lexer.State;
            foreach (var abstractParser in AbstractTermParsers.Values)
            {
                if (abstractParser.TryParse(this) is not { HasValue: true } parsed)
                {
                    Fail(pos);
                    continue;
                }

                ITerm = parsed.GetOrThrow().CanonicalForm;
                return true;
            }

            if (TryParseVariable(out var var))
            {
                ITerm = var;
                return true;
            }

            if (TryParseComplex(out var cplx))
            {
                ITerm = cplx;
                return true;
            }

            if (TryParseAtom(out var atom))
            {
                ITerm = atom;
                return true;
            }

            ITerm = default;
            return false;
        }
    }

    public bool ExpectOperator(Func<Operator, bool> match, out Operator op)
    {
        op = default;
        if (Expect(Lexer.TokenType.Operator, str => TryGetOperatorsFromFunctor(new Atom(str), out var _op) && _op.Any(match)
        , out string str)
            && TryGetOperatorsFromFunctor(new Atom(str), out var ops))
        {
            op = ops.Single(match);
            return true;
        }

        return false;
    }

    public Expression BuildExpression(Operator op, ITerm lhs, Maybe<ITerm> maybeRhs = default, bool exprParenthesized = false)
    {
        return maybeRhs.Reduce(
            rhs => Associate(lhs, rhs),
            () => new Expression(op, lhs, Maybe<ITerm>.None, lhs.IsParenthesized || exprParenthesized)
        );

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
                var lhsRhs = lhsExpr.Right.Reduce(x => x, () => throw new InvalidOperationException());
                var newRhs = BuildExpression(lhsExpr.Operator, lhsRhs, Maybe.Some(rhs), exprParenthesized);
                return BuildExpression(op, lhsExpr.Left, Maybe.Some<ITerm>(newRhs.Complex), exprParenthesized);
            }

            return new Expression(op, lhs, Maybe.Some(rhs), exprParenthesized);
        }

        bool TryConvertExpression(ITerm t, out Expression expr, bool exprParenthesized = false)
        {
            expr = default;
            if (t is not Complex cplx)
                return false;
            if (!TryGetOperatorsFromFunctor(cplx.Functor, out var ops))
                return false;
            var op = ops.Where(op => cplx.Arity switch
            {
                1 => op.Affix != OperatorAffix.Infix
                ,
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

    public bool TryParsePrefixExpression(out Expression expr)
    {
        expr = default; var pos = Lexer.State;
        if (ExpectOperator(op => op.Affix == OperatorAffix.Prefix, out var op)
        && TryParseTerm(out var arg, out var parens)
        && (parens || !arg.IsAbstract<NTuple>(out _)))
        {
            expr = BuildExpression(op, arg, exprParenthesized: parens);
            return true;
        }

        return Fail(pos);
    }

    public bool TryParsePostfixExpression(out Expression expr)
    {
        expr = default; var pos = Lexer.State;
        if (TryParseTerm(out var arg, out var parens)
        && (parens || !arg.IsAbstract<NTuple>(out _))
        && ExpectOperator(op => op.Affix == OperatorAffix.Postfix, out var op))
        {
            expr = BuildExpression(op, arg, exprParenthesized: parens);
            return true;
        }

        return Fail(pos);
    }

    public bool TryParseExpression(out Expression expr)
    {
        expr = default; var pos = Lexer.State;
        if (TryParsePrimary(out var lhs, out _))
        {
            if (WithMinPrecedence(lhs, 0, out expr))
            {
                return true;
            }
            // Special case for unary expressions
            if (lhs is not Complex cplx
                || cplx.Arguments.Length > 1
                || !TryGetOperatorsFromFunctor(cplx.Functor, out var ops))
            {
                return Fail(pos);
            }

            var op = ops.Single(op => op.Affix != OperatorAffix.Infix);
            expr = BuildExpression(op, cplx.Arguments[0], Maybe<ITerm>.None);
            return true;
        }

        return Fail(pos);

        bool WithMinPrecedence(ITerm lhs, int minPrecedence, out Expression expr)
        {
            expr = default; var pos = Lexer.State;
            if (!TryPeekNextOperator(out var lookahead))
            {
                return Fail(pos);
            }

            if (lookahead.Affix != OperatorAffix.Infix || lookahead.Precedence < minPrecedence)
            {
                return Fail(pos);
            }

            while (lookahead.Affix == OperatorAffix.Infix && lookahead.Precedence >= minPrecedence)
            {
                Lexer.TryReadNextToken(out _);
                var op = lookahead;
                if (!TryParsePrimary(out var rhs, out _))
                {
                    return Fail(pos);
                }

                if (!TryPeekNextOperator(out lookahead))
                {
                    expr = BuildExpression(op, lhs, Maybe.Some(rhs));
                    break;
                }

                while (lookahead.Affix == OperatorAffix.Infix && lookahead.Precedence > op.Precedence
                    || lookahead.Associativity == OperatorAssociativity.Right && lookahead.Precedence == op.Precedence)
                {
                    if (!WithMinPrecedence(rhs, op.Precedence + 1, out var newRhs))
                    {
                        break;
                    }

                    rhs = newRhs.Complex;
                    if (!TryPeekNextOperator(out lookahead))
                    {
                        break;
                    }
                }

                lhs = (expr = BuildExpression(op, lhs, Maybe.Some(rhs))).Complex;
            }

            return true;
        }

        bool TryParsePrimary(out ITerm ITerm, out bool parenthesized)
        {
            ITerm = default; var pos = Lexer.State;
            parenthesized = false;
            if (TryParsePrefixExpression(out var prefix))
            {
                ITerm = prefix.Complex;
                return true;
            }

            if (TryParsePostfixExpression(out var postfix))
            {
                ITerm = postfix.Complex;
                return true;
            }

            if (TryParseTerm(out ITerm, out parenthesized))
            {
                return true;
            }

            return Fail(pos);
        }

        bool TryPeekNextOperator(out Operator op)
        {
            op = default;
            if (Lexer.TryPeekNextToken(out var lookahead)
            && TryGetOperatorsFromFunctor(new Atom(lookahead.Value), out var ops))
            {
                op = ops.Where(op => op.Affix == OperatorAffix.Infix).MinBy(x => x.Precedence);
                return true;
            }

            return false;
        }
    }

    public bool TryParseDirective(out Directive directive)
    {
        directive = default;
        var pos = Lexer.State;
        if (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string desc))
        {
            desc = desc[1..].TrimStart();
            while (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string newDesc))
            {
                if (!string.IsNullOrEmpty(newDesc))
                {
                    desc += "\n" + newDesc[1..].TrimStart();
                }
            }
        }

        desc ??= " ";
        if (!TryParseExpression(out var op))
        {
            return Fail(pos);
        }

        if (!WellKnown.Operators.UnaryHorn.Equals(op.Operator))
        {
            return Fail(pos);
        }

        if (!ExpectDelimiter(p => p.Equals("."), out var _))
        {
            Throw(pos, ErrorType.UnterminatedClauseList);
        }

        var lhs = op.Left;
        //if (CommaList.TryUnfold(lhs, out var expr))
        //{
        //    lhs = expr.CanonicalForm;
        //}

        directive = new(lhs, desc);
        return true;
    }

    public bool TryParsePredicate(out Predicate predicate)
    {
        predicate = default;
        var pos = Lexer.State;
        if (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string desc))
        {
            desc = desc[1..].TrimStart();
            while (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string newDesc))
            {
                if (!string.IsNullOrEmpty(newDesc))
                {
                    desc += "\n" + newDesc[1..].TrimStart();
                }
            }
        }

        desc ??= " ";
        if (Lexer.Eof)
            return Fail(pos);
        if (!TryParseExpression(out var op))
        {
            if (TryParseTerm(out var head, out _) && ExpectDelimiter(p => p.Equals("."), out var _))
            {
                return MakePredicate(pos, desc, head, new(ImmutableArray<ITerm>.Empty.Add(WellKnown.Literals.True)), out predicate);
            }

            Throw(pos, ErrorType.ExpectedClauseList);
        }

        if (!ExpectDelimiter(p => p.Equals("."), out var _))
        {
            Throw(pos, ErrorType.UnterminatedClauseList);
        }

        if (!WellKnown.Operators.BinaryHorn.Equals(op.Operator))
        {
            op = new Expression(WellKnown.Operators.BinaryHorn, op.Complex, Maybe.Some<ITerm>(WellKnown.Literals.True), false);
        }

        var rhs = op.Right.Reduce(s => s, () => throw new NotImplementedException());

        if (NTuple.FromQuasiCanonical(rhs, default, hasEmptyElement: Maybe.Some(false)) is not { HasValue: true } contents)
        {
            contents = Maybe.Some(new NTuple(new[] { rhs }));
        }

        return MakePredicate(pos, desc, op.Left, contents.GetOrThrow(), out predicate);

        bool MakePredicate(Lexer.StreamState pos, string desc, ITerm head, Ast.NTuple body, out Predicate c)
        {
            var headVars = head.Variables
                .Where(v => !v.Equals(WellKnown.Literals.Discard));
            var bodyVars = body.Contents.SelectMany(t => t.Variables)
                .Distinct();
            var singletons = headVars.Where(v => !v.Ignored && !bodyVars.Contains(v) && headVars.Count(x => x.Name == v.Name) == 1)
                .Select(v => v.Explain());
            if (singletons.Any())
            {
                Throw(pos, ErrorType.PredicateHasSingletonVariables, head.GetSignature().Explain(), string.Join(", ", singletons));
            }

            c = new Predicate(
                desc
                , WellKnown.Modules.User
                , head
                , body
            );
            return true;
        }
    }

    public bool TryParseProgram(out ErgoProgram program)
    {
        var directives = new List<Directive>();
        var predicates = new List<Predicate>();
        while (TryParseDirective(out var directive))
        {
            directives.Add(directive);
        }

        while (TryParsePredicate(out var predicate))
        {
            predicates.Add(predicate);
        }

        program = new ErgoProgram(directives.ToArray(), predicates.ToArray())
            .AsPartial(false);
        return true;
    }

    public IEnumerable<Operator> ParseOperatorDeclarations()
    {
        var moduleName = WellKnown.Modules.Stdlib;
        var ret = new List<Operator>();
        try
        {
            while (TryParseDirective(out var directive))
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
                    if (!cplx.Arguments[2].IsAbstract<List>(out var syns))
                        continue;
                    ret.Add(new(moduleName, type, precedence, syns.Contents.Cast<Atom>().ToArray()));
                }
            }
        }
        catch
        {
            // The parser reached a point where a newly-declared operator was used. Probably.
        }

        return ret;
    }

    public bool TryParseProgramDirectives(out ErgoProgram program)
    {
        var ret = true;
        program = default;
        var directives = new List<Directive>();
        try
        {
            while (TryParseDirective(out var directive))
            {
                directives.Add(directive);
            }
        }
        catch (LexerException le) when (le.ErrorType == Lexer.ErrorType.UnrecognizedOperator)
        {
            // The parser reached a point where a newly-declared operator was used. Probably.
        }

        if (!ret)
            return false;
        program = new ErgoProgram(directives.ToArray(), Array.Empty<Predicate>())
            .AsPartial(true);
        return true;
    }

    public void Dispose()
    {
        Lexer.Dispose();
        GC.SuppressFinalize(this);
    }
}
