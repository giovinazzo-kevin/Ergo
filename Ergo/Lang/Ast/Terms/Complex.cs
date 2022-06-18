using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly partial struct Complex : ITerm
{
    public bool IsGround => Arguments.All(arg => arg.IsGround);
    public readonly bool IsQualified { get; }

    public readonly Maybe<OperatorAffix> Affix;
    public readonly bool IsParenthesized { get; }

    public readonly Atom Functor;
    public readonly ITerm[] Arguments;
    public readonly int Arity => Arguments.Length;
    public readonly Maybe<IAbstractTerm> AbstractForm { get; }

    private readonly int HashCode;

    public Complex(Atom functor, params ITerm[] args)
    {
        Functor = functor;
        Arguments = args;
        HashCode = Arguments.Aggregate(Functor.GetHashCode(), (hash, a) => System.HashCode.Combine(hash, a));
        IsQualified = args.Length == 2 && WellKnown.Functors.Module.Contains(functor);
        Affix = Maybe<OperatorAffix>.None;
        IsParenthesized = false;
        AbstractForm = default;
    }

    private Complex(Maybe<OperatorAffix> affix, bool parenthesized, Atom functor, Maybe<IAbstractTerm> isAbstract, params ITerm[] args)
        : this(functor, args)
    {
        Affix = affix;
        IsParenthesized = parenthesized;
        AbstractForm = isAbstract;
    }
    public Complex AsOperator(Maybe<OperatorAffix> affix) => new(affix, IsParenthesized, Functor, AbstractForm, Arguments);
    public Complex AsOperator(OperatorAffix affix) => new(Maybe.Some(affix), IsParenthesized, Functor, AbstractForm, Arguments);
    public Complex AsParenthesized(bool parens) => new(Affix, parens, Functor, AbstractForm, Arguments);
    public Complex WithAbstractForm(Maybe<IAbstractTerm> abs) => new(Affix, IsParenthesized, Functor, abs, Arguments);
    public Complex WithFunctor(Atom functor) => new(Affix, IsParenthesized, functor, AbstractForm, Arguments);
    public Complex WithArguments(params ITerm[] args) => new(Affix, IsParenthesized, Functor, AbstractForm, args);

    public string Explain(bool canonical = false)
    {
        if (!canonical && IsParenthesized)
            return $"({Inner(this, AbstractForm)})";
        return Inner(this, AbstractForm);

        string Inner(Complex c, Maybe<IAbstractTerm> absForm)
        {
            if (!canonical && absForm.HasValue)
                return absForm.GetOrThrow().Explain();
            if (c.IsAbstractTerm<List>(out var list))
                return list.Explain();
            return c.Affix.Reduce(some => canonical ? OperatorAffix.Prefix : some, () => OperatorAffix.Prefix) switch
            {
                OperatorAffix.Infix => $"{c.Arguments[0].Explain(canonical)}{c.Functor.Explain(canonical)}{c.Arguments[1].Explain(canonical)}",
                OperatorAffix.Postfix => $"{c.Arguments.Single().Explain(canonical)}{c.Functor.Explain(canonical)}",
                _ when !canonical && c.Affix.HasValue => $"{c.Functor.Explain(canonical)}{c.Arguments.Single().Explain(canonical)}",
                _ => $"{c.Functor.Explain(canonical)}({string.Join(',', c.Arguments.Select(arg => arg.Explain(canonical)))})",
            };
        }
    }

    public ITerm Substitute(Substitution s)
    {
        if (AbstractForm.HasValue)
            return AbstractForm.GetOrThrow().Substitute(s).CanonicalForm;

        if (Equals(s.Lhs))
        {
            return s.Rhs;
        }

        var newArgs = new ITerm[Arguments.Length];
        for (var i = 0; i < newArgs.Length; i++)
        {
            newArgs[i] = Arguments[i].Substitute(s);
        }

        return WithArguments(newArgs);
    }

    public IEnumerable<Variable> Variables => Arguments.SelectMany(arg => arg.Variables);

    public bool Matches(Complex other) => Equals(Functor, other.Functor) && Arity == other.Arity;

    public override bool Equals(object obj)
    {
        if (obj is not Complex other)
        {
            return false;
        }

        var args = Arguments;
        return Matches(other) && Enumerable.Range(0, Arity).All(i => Equals(args[i], other.Arguments[i]));
    }
    public bool Equals(ITerm obj) => Equals((object)obj);

    public override int GetHashCode() => HashCode;

    public int CompareTo(ITerm o)
    {
        if (o is Atom) return 1;
        if (o is Variable) return 1;
        if (o is not Complex other) throw new InvalidCastException();

        if (Arity.CompareTo(other.Arity) is var cmpArity && cmpArity != 0)
            return cmpArity;
        if (Functor.CompareTo(other.Functor) is var cmpFunctor && cmpFunctor != 0)
            return cmpFunctor;
        return Arguments.Select((a, i) => a.CompareTo(other.Arguments[i]))
            .DefaultIfEmpty(0)
            .FirstOrDefault(cmp => cmp != 0);
    }

    public ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        if (AbstractForm.HasValue)
            return AbstractForm.GetOrThrow().Instantiate(ctx, vars).CanonicalForm;
        return new Complex(Affix, IsParenthesized, Functor, AbstractForm, Arguments.Select(arg => arg.Instantiate(ctx, vars)).ToArray());
    }
    public static bool operator ==(Complex left, Complex right) => left.Equals(right);

    public static bool operator !=(Complex left, Complex right) => !(left == right);
}
