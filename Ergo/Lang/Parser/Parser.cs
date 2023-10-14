using Ergo.Facade;
using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Parser;
using Ergo.Lang.Utils;

namespace Ergo.Lang;

public partial class ErgoParser : IDisposable
{
    private static readonly Comparer<IAbstractTermParser> _absComparer =
        Comparer<IAbstractTermParser>.Create((x, y) => x.ParsePriority.CompareTo(y.ParsePriority));

    private InstantiationContext _discardContext;
    private HashSet<long> _memoizationFailures = new();
    private Dictionary<long, object> _memoizationTable = new();


    private readonly DiagnosticProbe Probe = new()
    {
#if !ERGO_PARSER_DIAGNOSTICS
        IsEnabled = false,
#endif
    };
    protected Dictionary<Type, IAbstractTermParser> AbstractTermParsers { get; private set; } = new();
    protected List<IAbstractTermParser> SortedAbstractTermParsers { get; private set; } = new();

    public readonly ErgoLexer Lexer;
    public readonly ErgoFacade Facade;

    public ParserScope GetScope() => new(Lexer.State);
    private long GetMemoKey(ErgoLexer.StreamState state, string callerName)
    {
        long hash = state.GetHashCode();
        hash = (hash << 5) - hash + callerName.GetHashCode();
        return hash;
    }

    private bool IsFailureMemoized(ErgoLexer.StreamState state, [CallerMemberName] string callerName = "")
    {
#if !ERGO_PARSER_DISABLE_MEMOIZATION
        if (_memoizationFailures.Contains(GetMemoKey(state, callerName)))
        {
            Probe.Count("MEMO_FAIL_HIT", 1, callerName);
            return true;
        }
#endif
        return false;
    }

    private void MemoizeFailure(ErgoLexer.StreamState state, [CallerMemberName] string callerName = "")
    {
#if !ERGO_PARSER_DISABLE_MEMOIZATION
        var key = GetMemoKey(state, callerName);
        if (!_memoizationFailures.Contains(key))
        {
            _memoizationFailures.Add(key);
            Probe.Count("MEMO_FAIL_NEW", 1, callerName);
        }
#endif
    }

    private void Memoize<T>(T data, ErgoLexer.StreamState state, [CallerMemberName] string callerName = "")
    {
#if !ERGO_PARSER_DISABLE_MEMOIZATION
        var key = GetMemoKey(state, callerName);
        if (!_memoizationTable.ContainsKey(key))
        {
            _memoizationTable[key] = data;
            Probe.Count("MEMO_SUCC_NEW", 1, callerName);
        }
#endif
    }

    private Maybe<T> Memoized<T>(ErgoLexer.StreamState state, [CallerMemberName] string callerName = "")
    {
#if !ERGO_PARSER_DISABLE_MEMOIZATION
        var key = GetMemoKey(state, callerName);
        if (_memoizationTable.TryGetValue(key, out var memo))
        {
            Probe.Count("MEMO_SUCC_HIT", 1, callerName);
            return (T)memo;
        }
#endif
        return default;
    }

    public Maybe<T> MemoizeFailureAndFail<T>(ErgoLexer.StreamState state, [CallerMemberName] string callerName = "")
    {
        MemoizeFailure(state, callerName);
        return Fail<T>(state);
    }

    internal ErgoParser(ErgoFacade facade, ErgoLexer lexer)
    {
        Facade = facade;
        Lexer = lexer;
        _discardContext = new(string.Empty);
    }

    public bool RemoveAbstractParser<T>(out IAbstractTermParser<T> parser)
        where T : AbstractTerm
    {
        parser = default;
        if (!AbstractTermParsers.Remove(typeof(T), out var parser_))
            return false;
        SortedAbstractTermParsers.Remove(parser);
        parser = (IAbstractTermParser<T>)parser_;
        return true;
    }
    public void AddAbstractParser<T>(IAbstractTermParser<T> parser)
        where T : AbstractTerm
    {
        AbstractTermParsers.Add(typeof(T), parser);
        SortedAbstractTermParsers.Add(parser);
        SortedAbstractTermParsers.Sort(_absComparer);
    }

    public Maybe<IEnumerable<Operator>> GetOperatorsFromFunctor(Atom functor)
    {
        if (functor.Value is not string f)
            return default;
        if (!Lexer.OperatorLookup.TryGetValue(f, out var ops))
            return default;
        if (!ops.Any())
            return default;
        return Maybe.Some((IEnumerable<Operator>)ops);
    }

    public Maybe<T> Abstract<T>()
        where T : AbstractTerm => Abstract(typeof(T)).Select(x => (T)x);

    public Maybe<AbstractTerm> Abstract(Type type)
    {
        var watch = Probe.Enter();
        if (!AbstractTermParsers.TryGetValue(type, out var parser))
        {
            Probe.Leave(watch);
            return default;
        }
        return parser.Parse(this)
            .Do(() => Probe.Leave(watch));
    }

    public Maybe<Atom> Atom()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
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
            .Or(() => MemoizeFailureAndFail<Atom>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            .Select(x => x.WithScope(scope))
            ;

    }
    public Maybe<Variable> Variable()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
        return Expect<string>(ErgoLexer.TokenType.Term)
        .Where(term => IsVariableIdentifier(term))
        .Map(term => Maybe.Some(term)
            //.Where(term => !term.StartsWith("__K"))
            .Do(none: () => Throw(scope.LexerState, ErrorType.TermHasIllegalName, term))
            .Where(term => !term.Equals(WellKnown.Literals.Discard.Explain()))
            .Or(() => $"_{_discardContext.VarPrefix}{_discardContext.GetFreeVariableId()}"))
        .Select(t => new Variable(t))
        .Or(() => MemoizeFailureAndFail<Variable>(scope.LexerState))
        .Do(() => Probe.Leave(watch))
        .Select(x => x.WithScope(scope))
        ;
    }

    public Maybe<Complex> Complex()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
        return Atom()
            .Map(functor => TupleParser.ParseArgList(this) // Regular tuples can't be 1 item long, but arg lists can.
                .Select(args => new Complex(functor, args.Contents.ToArray())))
            .Or(() => MemoizeFailureAndFail<Complex>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            .Select(x => x.WithScope(scope))
            ;
    }

    public Maybe<ITerm> ExpressionOrTerm()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
        return Expression().Select<ITerm>(e => e.Term)
            .Or(() => Term())
            .Or(() => MemoizeFailureAndFail<ITerm>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            .Select(x => x.WithScope(scope))
            ;
    }

    public Maybe<AbstractTerm> Abstract()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (SortedAbstractTermParsers.Count > 0)
        {
            var abstractFold = SortedAbstractTermParsers.Skip(1)
                .Aggregate(SortedAbstractTermParsers.First().Parse(this).Or(() => Fail<AbstractTerm>(scope.LexerState)),
                    (a, b) => a.Or(() => b.Parse(this)).Or(() => Fail<AbstractTerm>(scope.LexerState)))
                ;
            return abstractFold
                .Do(() => Probe.Leave(watch));
        }
        return default;
    }

    public Maybe<ITerm> Term()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
        return Parenthesized("(", ")", () => Expression())
                .Select(x => x.Term.AsParenthesized(true))
            .Or(() => Parenthesized("(", ")", () => Inner()
                .Select(x => x.AsParenthesized(true))))
            .Or(() => Inner())
            .Or(() => MemoizeFailureAndFail<ITerm>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            .Select(x => x.WithScope(scope))
            ;

        Maybe<ITerm> Inner()
        {
            var scope = GetScope();
            var primary = () => Variable().Select(x => (ITerm)x)
                .Or(() => Complex().Select(x => (ITerm)x))
                .Or(() => Atom().Select(x => (ITerm)x))
                .Or(() => Fail<ITerm>(scope.LexerState));
            return Abstract().Select(x => (ITerm)x).Or(() => primary());
        }
    }

    public Maybe<Operator> ExpectOperator(Func<Operator, bool> match)
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        return Expect<string>(new[] { ErgoLexer.TokenType.Operator, ErgoLexer.TokenType.Term })
            .Map(str => GetOperatorsFromFunctor(new Atom(str)))
            .Where(ops => ops.Any(match))
            .Select(ops => ops.Single(match))
            .Do(() => Probe.Leave(watch))
            ;
    }
    public Expression BuildExpression(Operator op, ITerm lhs, Maybe<ITerm> maybeRhs = default, bool exprParenthesized = false)
    {
        var watch = Probe.Enter();
        if (!lhs.IsParenthesized
            && TryConvertExpression(lhs, out var lhsExpr, exprParenthesized)
            && lhsExpr.Operator.Fixity == Fixity.Prefix
            && lhsExpr.Operator.Associativity == OperatorAssociativity.Right
            && lhsExpr.Operator.Precedence < op.Precedence)
        {
            // $K.a.b.c -> $(.(.(.(K, a), b), c))
            if (op.Fixity == Fixity.Infix)
            {
                var inner = BuildExpression(op, lhsExpr.Left, maybeRhs);
                var ret = BuildExpression(lhsExpr.Operator, inner.Term, default, exprParenthesized);
                Probe.Leave(watch);
                return ret;
            }
        }
        return maybeRhs
            .Select(rhs => Associate(lhs, rhs))
            .Select(TryConvertCommaExpression)
            .Do(() => Probe.Leave(watch))
            .GetOr(new Expression(op, lhs, Maybe<ITerm>.None, lhs.IsParenthesized || exprParenthesized))
            ;

        // Special case for tuples (comma-expressions). Since they are so ingrained in the language,
        // parsing can be very ambiguous in the non-parenthesized case. Therefore it's best to parse
        // them as expressions and unfold them right at the end by overwriting the expression.
        Expression TryConvertCommaExpression(Expression expr)
        {
            if (NTuple.FromPseudoCanonical(expr.Term, expr.Term.Scope).TryGetValue(out var tup))
            {
                return expr.WithTerm(tup);
            }
            return expr;
        }

        Expression Associate(ITerm lhs, ITerm rhs)
        {
            // When the lhs represents an expression with the same precedence as this (and thus associativity, by design)
            // and right associativity, we have to swap the arguments around until they look right.
            if (!lhs.IsParenthesized
            && TryConvertExpression(lhs, out var lhsExpr, exprParenthesized))
            {
                if (lhsExpr.Operator.Fixity == Fixity.Infix
                    && op.Fixity == Fixity.Infix
                    && lhsExpr.Operator.Associativity == OperatorAssociativity.Right
                    && lhsExpr.Operator.Precedence == op.Precedence)
                {
                    // a, b, c -> ','(','(','(a, b), c)) -> ','(a, ','(b, ','(c))
                    var lhsRhs = lhsExpr.Right.GetOrThrow(new InvalidOperationException());
                    var newRhs = BuildExpression(lhsExpr.Operator, lhsRhs, Maybe.Some(rhs), exprParenthesized);
                    return BuildExpression(op, lhsExpr.Left, Maybe.Some<ITerm>(newRhs.Term), exprParenthesized);
                }
                if (TryConvertExpression(rhs, out var rhsExpr, exprParenthesized)
                    && lhsExpr.Operator.Fixity == Fixity.Infix
                    && rhsExpr.Operator.Fixity == Fixity.Postfix
                    && rhsExpr.Operator.Associativity == OperatorAssociativity.Left
                    && lhsExpr.Operator.Precedence > rhsExpr.Operator.Precedence)
                {
                    // lhs: K.a.b rhs: c$ op: .
                    var inner = BuildExpression(op, lhsExpr.Left, Maybe.Some(rhsExpr.Left));
                    var ret = BuildExpression(rhsExpr.Operator, inner.Term, default, exprParenthesized);
                    return ret;
                }
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
            var ops_ = ops.Where(op => cplx.Arity switch
            {
                1 => op.Fixity != Fixity.Infix,
                _ => op.Fixity == Fixity.Infix
            })
            .OrderBy(x => x.Precedence)
            .ToArray();
            if (ops_.Length == 0)
                return false;
            var op = ops_[0];
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
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
        return ExpectOperator(op => op.Fixity == Fixity.Prefix)
            .Map(op => Term()
                .Select(arg => BuildExpression(op, arg, exprParenthesized: arg.IsParenthesized)))
            .Or(() => MemoizeFailureAndFail<Expression>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            ;
    }

    public Maybe<Expression> Postfix()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
        return Term()
            .Map(arg => ExpectOperator(op => op.Fixity == Fixity.Postfix)
                .Select(op => BuildExpression(op, arg, exprParenthesized: arg.IsParenthesized)))
            .Or(() => MemoizeFailureAndFail<Expression>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            ;
    }
    public Maybe<Expression> Expression()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }

        if (Primary().TryGetValue(out var lhs))
        {
            if (WithMinPrecedence(lhs, 0).TryGetValue(out var expr))
            {
                return Maybe.Some(expr)
                    .Do(() => Probe.Leave(watch));
            }

            // Special case for unary expressions
            if (lhs is not Complex cplx
                || cplx.Arguments.Length > 1
                || !GetOperatorsFromFunctor(cplx.Functor).TryGetValue(out var ops))
            {
                return MemoizeFailureAndFail<Expression>(scope.LexerState)
                    .Do(() => Probe.Leave(watch));
            }

            var op = ops.Single(op => op.Fixity != Fixity.Infix);
            if (!PeekNextOperator().TryGetValue(out var nextOperator)
                || nextOperator.Precedence < op.Precedence)
            {
                expr = BuildExpression(op, cplx.Arguments[0], Maybe<ITerm>.None);
                return Maybe.Some(expr)
                    .Do(() => Probe.Leave(watch));
            }
            return MemoizeFailureAndFail<Expression>(scope.LexerState)
                    .Do(() => Probe.Leave(watch));
        }

        return MemoizeFailureAndFail<Expression>(scope.LexerState)
            .Do(() => Probe.Leave(watch));

    }
    Maybe<Expression> WithMinPrecedence(ITerm lhs, int minPrecedence)
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (!PeekNextOperator().TryGetValue(out var lookahead))
        {
            return Fail<Expression>(scope.LexerState)
                .Do(() => Probe.Leave(watch))
                ;
        }

        if (lookahead.Fixity != Fixity.Infix || lookahead.Precedence < minPrecedence)
        {
            return Fail<Expression>(scope.LexerState)
                .Do(() => Probe.Leave(watch))
                ;
        }
        var expr = default(Expression);
        while (lookahead.Fixity == Fixity.Infix && lookahead.Precedence >= minPrecedence)
        {
            Lexer.ReadNext();
            var op = lookahead;

            if (!Primary().TryGetValue(out var rhs))
            {
                return Fail<Expression>(scope.LexerState)
                .Do(() => Probe.Leave(watch))
                ;
            }

            if (!PeekNextOperator().TryGetValue(out lookahead))
            {
                expr = BuildExpression(op, lhs, Maybe.Some(rhs));
                break;
            }

            while (lookahead.Fixity == Fixity.Infix && lookahead.Precedence > op.Precedence
                || lookahead.Associativity == OperatorAssociativity.Right && lookahead.Precedence == op.Precedence)
            {
                if (!WithMinPrecedence(rhs, op.Precedence + 1).TryGetValue(out var newRhs))
                {
                    break;
                }

                rhs = newRhs.Term;
                if (!PeekNextOperator().TryGetValue(out lookahead))
                {
                    break;
                }
            }

            lhs = (expr = BuildExpression(op, lhs, Maybe.Some(rhs))).Term;
        }

        return Maybe.Some(expr)
                .Do(() => Probe.Leave(watch))
                ;
    }

    Maybe<ITerm> Primary()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
        return Prefix().Select<ITerm>(p => p.Term)
            .Or(() => Postfix().Select<ITerm>(p => p.Term))
            .Or(() => Term())
            .Or(() => MemoizeFailureAndFail<ITerm>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            .Select(x => x.WithScope(scope))
            ;
    }

    Maybe<Operator> PeekNextOperator()
    {
        var watch = Probe.Enter();
        try
        {
            return Lexer.PeekNext()
                .Where(x => x.Type == ErgoLexer.TokenType.Operator || x.Type == ErgoLexer.TokenType.Term)
                .Map(lookahead => GetOperatorsFromFunctor(new Atom(lookahead.Value)))
                .Select(ops => ops.Where(op => op.Fixity == Fixity.Infix).MinBy(x => x.Precedence))
                .Do(() => Probe.Leave(watch))
                ;
        }
        catch (InvalidOperationException)
        {
            Probe.Leave(watch);
            return default;
        }
    }

    public Maybe<Directive> Directive()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
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
            return MemoizeFailureAndFail<Directive>(scope.LexerState).Do(() => Probe.Leave(watch));

        desc ??= " ";
        return Expression()
            .Where(op => WellKnown.Operators.UnaryHorn.Equals(op.Operator))
            .Map(op => ExpectDelimiter(p => p.Equals("."))
                .Do(none: () => Throw(scope.LexerState, ErrorType.UnterminatedClauseList))
                .Select(_ => op))
            .Select(op => new Directive(op.Left, desc))
            .Or(() => MemoizeFailureAndFail<Directive>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
            ;
    }

    public Maybe<Predicate> Predicate()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
        if (IsFailureMemoized(scope.LexerState))
        {
            Probe.Leave(watch);
            return default;
        }
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
            return MemoizeFailureAndFail<Predicate>(scope.LexerState).Do(() => Probe.Leave(watch));

        desc ??= " ";
        return Expression()
            .Map(op => Maybe.Some(op)
                .Where(op => WellKnown.Operators.BinaryHorn.Equals(op.Operator))
                .Or(() => new Expression(WellKnown.Operators.BinaryHorn, op.Term, Maybe.Some<ITerm>(WellKnown.Literals.True), false)))
            .Map(op => Maybe.Some(op.Right.GetOrThrow(new InvalidOperationException()))
                .Map(rhs => NTuple.FromPseudoCanonical(rhs, default, rhs.IsParenthesized, hasEmptyElement: false)
                    .Or(() => new NTuple(new[] { rhs }, scope, rhs.IsParenthesized)))
                .Select(body => (head: op.Left, body)))
            .Or(() => Term()
                .Select(head => (head, body: new NTuple(new ITerm[] { WellKnown.Literals.True }, scope, false))))
            .Do(none: () => Throw(scope.LexerState, ErrorType.ExpectedClauseList))
            .Map(x => MakePredicate(scope.LexerState, desc, x.head, x.body))
            .Map(p => ExpectDelimiter(p => p.Equals("."))
                .Do(none: () => Throw(scope.LexerState, ErrorType.UnterminatedClauseList))
                .Select(_ => p))
            .Or(() => MemoizeFailureAndFail<Predicate>(scope.LexerState))
            .Do(() => Probe.Leave(watch))
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
                Throw(scope.LexerState, ErrorType.PredicateHasSingletonVariables, head.GetSignature().Explain(), singletons.Join());
            }

            return new Predicate(desc, WellKnown.Modules.User, head, body, false, false);
        }
    }

    public Maybe<ErgoProgram> Program()
    {
        var watch = Probe.Enter();
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

        if (moduleArgs.Length < 2 || moduleArgs[1] is not List exported)
        {
            exported = List.Empty;
        }

        var exportedPredicates = predicates.Select(p =>
        {
            var sign = p.Head.GetSignature();
            var form = new Complex(WellKnown.Operators.ArityIndicator.CanonicalFunctor, sign.Functor, new Atom((decimal)sign.Arity.GetOrThrow(new NotSupportedException())))
                .AsOperator(WellKnown.Operators.ArityIndicator);
            if (exported.Contents.Any(x => x.Equals(form)))
                return p.Exported();
            return p;
        });
        return Maybe.Some(new ErgoProgram(directives.ToArray(), exportedPredicates.ToArray())
            .AsPartial(false))
            .Do(() => Probe.Leave(watch))
            ;
    }

    public IEnumerable<Operator> OperatorDeclarations()
    {
        var watch = Probe.Enter();
        var scope = GetScope();
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
                    if (cplx.Arguments[2] is not List syns)
                        continue;
                    ret.Add(new(moduleName, type, precedence, syns.Contents.Cast<Atom>().ToHashSet()));
                }
            }
        }
        catch
        {
            // The parser reached a point where a newly-declared operator was used. Probably.
        }

        Lexer.Seek(scope.LexerState);
        Probe.Leave(watch);
        return ret;
    }

    public Maybe<ErgoProgram> ProgramDirectives()
    {
        var watch = Probe.Enter();
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
        return Maybe.Some(new ErgoProgram(directives.ToArray(), Array.Empty<Predicate>())
            .AsPartial(true))
            .Do(() => Probe.Leave(watch))
            ;
    }

    public void Dispose()
    {
#if ERGO_PARSER_DIAGNOSTICS
        Console.WriteLine(Lexer.Stream.FileName);
        Console.WriteLine(Probe.GetDiagnostics());
#endif
        Probe.Dispose();
        Lexer.Dispose();
        GC.SuppressFinalize(this);
    }
}
