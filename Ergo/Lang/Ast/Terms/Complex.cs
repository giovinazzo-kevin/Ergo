using System.Collections.Immutable;
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

    private readonly int HashCode;

    public Complex(Atom functor, params ITerm[] args)
    {
        Functor = functor;
        Arguments = args;
        HashCode = Arguments.Aggregate(Functor.GetHashCode(), (hash, a) => System.HashCode.Combine(hash, a));
        IsQualified = args.Length == 2 && WellKnown.Functors.Module.Contains(functor);
        Affix = Maybe<OperatorAffix>.None;
        IsParenthesized = false;
    }

    private Complex(Maybe<OperatorAffix> affix, bool parenthesized, Atom functor, params ITerm[] args)
        : this(functor, args)
    {
        Affix = affix;
        IsParenthesized = parenthesized;
    }
    public Complex AsOperator(Maybe<OperatorAffix> affix) => new(affix, IsParenthesized, Functor, Arguments);
    public Complex AsOperator(OperatorAffix affix) => new(Maybe.Some(affix), IsParenthesized, Functor, Arguments);
    public Complex AsParenthesized(bool parens) => new(Affix, parens, Functor, Arguments);

    public string Explain(bool canonical = false)
    {
        if (!canonical && IsParenthesized)
        {
            return $"({Inner(this)})";
        }
        return Inner(this);

        string Inner(Complex c)
        {
            if (List.TryUnfold(c, out var list))
                return list.Explain(canonical);
            if (CommaSequence.TryUnfold(c, out var comma))
                return comma.Explain(canonical);
            return c.Affix.Reduce(some => canonical ? OperatorAffix.Prefix : some, () => OperatorAffix.Prefix) switch
            {
                OperatorAffix.Infix => $"{c.Arguments[0].Explain(canonical)}{c.Functor.Explain(canonical)}{c.Arguments[1].Explain(canonical)}",
                OperatorAffix.Postfix => $"{c.Arguments.Single().Explain(canonical)}{c.Functor.Explain(canonical)}",
                _ when !canonical && c.Affix.HasValue => $"{c.Functor.Explain(canonical)}{c.Arguments.Single().Explain(canonical)}",
                _ => $"{c.Functor.Explain(canonical)}({String.Join(',', c.Arguments.Select(arg => arg.Explain(canonical)))})",
            };
        }
    }

    public ITerm Substitute(Substitution s)
    {
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

    public Complex WithFunctor(Atom functor)
    {
        return new Complex(Affix, IsParenthesized, functor, Arguments);
    }

    public Complex WithArguments(params ITerm[] args)
    {
        return new Complex(Affix, IsParenthesized, Functor, args);
    }

    public bool Matches(Complex other)
    {
        return Equals(Functor, other.Functor) && Arity == other.Arity;
    }

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

    public override int GetHashCode()
    {
        return HashCode;
    }

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
        return new Complex(Affix, IsParenthesized, Functor, Arguments.Select(arg => arg.Instantiate(ctx, vars)).ToArray());
    }

    public static bool operator ==(Complex left, Complex right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Complex left, Complex right)
    {
        return !(left == right);
    }
}
