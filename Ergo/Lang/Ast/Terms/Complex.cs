using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly partial struct Complex : ITerm
{
    public bool IsGround => Arguments.All(arg => arg.IsGround);
    public readonly bool IsQualified { get; }

    public readonly Maybe<Fixity> Fixity;
    public readonly bool IsParenthesized { get; }

    public readonly Atom Functor;
    public readonly ImmutableArray<ITerm> Arguments;
    public readonly int Arity => Arguments.Length;
    public readonly Maybe<IAbstractTerm> AbstractForm { get; }

    private readonly int HashCode;

    public Complex(Atom functor, ImmutableArray<ITerm> args)
    {
        Functor = functor;
        Arguments = args;
        HashCode = Arguments.Aggregate(Functor.GetHashCode(), (hash, a) => System.HashCode.Combine(hash, a));
        IsQualified = args.Length == 2 && WellKnown.Functors.Module.Contains(functor);
        Fixity = Maybe<Fixity>.None;
        IsParenthesized = false;
        AbstractForm = default;
    }
    public Complex(Atom functor, params ITerm[] args)
        : this(functor, args.ToImmutableArray()) { }

    private Complex(Maybe<Fixity> affix, bool parenthesized, Atom functor, Maybe<IAbstractTerm> isAbstract, ImmutableArray<ITerm> args)
        : this(functor, args)
    {
        Fixity = affix;
        IsParenthesized = parenthesized;
        AbstractForm = isAbstract;
    }
    public Complex AsOperator(Maybe<Fixity> affix) => new(affix, IsParenthesized, Functor, AbstractForm, Arguments);
    public Complex AsOperator(Fixity affix) => new(affix, IsParenthesized, Functor, AbstractForm, Arguments);
    public Complex AsParenthesized(bool parens) => new(Fixity, parens, Functor, AbstractForm, Arguments);
    public Complex WithAbstractForm(Maybe<IAbstractTerm> abs) => new(Fixity, IsParenthesized, Functor, abs, Arguments);
    public Complex WithFunctor(Atom functor) => new(Fixity, IsParenthesized, functor, AbstractForm, Arguments);
    public Complex WithArguments(ImmutableArray<ITerm> args) => new(Fixity, IsParenthesized, Functor, AbstractForm, args);

    public string Explain(bool canonical = false)
    {
        if (!canonical && IsParenthesized)
            return ParenthesizeUnlessRedundant(Inner(this, AbstractForm));
        return Inner(this, AbstractForm);

        string ParenthesizeUnlessRedundant(string s) => s.StartsWith('(') && s.EndsWith(')') ? s : $"({s})";

        string Inner(Complex c, Maybe<IAbstractTerm> absForm)
        {
            if (absForm.TryGetValue(out var abs))
                return abs.Explain(canonical);
            return c.Fixity.Select(some => canonical ? Ast.Fixity.Prefix : some).GetOr(Ast.Fixity.Prefix) switch
            {
                Ast.Fixity.Infix when !c.Functor.IsQuoted => $"{c.Arguments[0].Explain(canonical)} {c.Functor.AsQuoted(false).Explain(canonical)} {c.Arguments[1].Explain(canonical)}",
                Ast.Fixity.Postfix when !c.Functor.IsQuoted => $"{c.Arguments.Single().Explain(canonical)}{c.Functor.AsQuoted(false).Explain(canonical)}",
                _ when !c.Functor.IsQuoted && !canonical && c.Fixity.TryGetValue(out _) => $"{c.Functor.AsQuoted(false).Explain(canonical)}{c.Arguments.Single().Explain(canonical)}",
                _ => $"{c.Functor.Explain(canonical)}({c.Arguments.Join(arg => arg.Explain(canonical))})",
            };
        }
    }

    public ITerm Substitute(Substitution s)
    {
        if (AbstractForm.TryGetValue(out var abs))
            return abs.Substitute(s).CanonicalForm;

        if (Equals(s.Lhs))
        {
            return s.Rhs;
        }

        var newArgs = new ITerm[Arguments.Length];
        for (var i = 0; i < newArgs.Length; i++)
        {
            newArgs[i] = Arguments[i].Substitute(s);
        }

        return WithArguments(newArgs.ToImmutableArray());
    }

    public IEnumerable<Variable> Variables => Arguments.SelectMany(arg => arg.Variables);

    public bool Matches(Complex other) => Equals(Functor, other.Functor) && Arity == other.Arity;

    public override bool Equals(object obj)
    {
        if (obj is not Complex other)
            return false;
        if (!Matches(other))
            return false;
        var args = Arguments;
        return args.SequenceEqual(other.Arguments);
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
        if (AbstractForm.TryGetValue(out var abs))
            return abs.Instantiate(ctx, vars).CanonicalForm;
        return new Complex(Fixity, IsParenthesized, Functor, AbstractForm, Arguments.Select(arg => arg.Instantiate(ctx, vars)).ToImmutableArray());
    }
    public static bool operator ==(Complex left, Complex right) => left.Equals(right);

    public static bool operator !=(Complex left, Complex right) => !(left == right);
}
