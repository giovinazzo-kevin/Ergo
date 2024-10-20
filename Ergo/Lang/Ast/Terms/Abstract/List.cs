using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class List : AbstractList
{
    public static readonly List Empty = new(ImmutableArray<ITerm>.Empty, default, default, false);
    public readonly ITerm Tail;
    public List(ImmutableArray<ITerm> contents, Maybe<ITerm> tail = default, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : base(contents, scope, parenthesized)
    {
        Tail = tail.GetOr(EmptyElement);
        CanonicalForm = Fold(Operator, Tail, contents).AsParenthesized(parenthesized);
    }
    public List(IEnumerable<ITerm> contents, Maybe<ITerm> tail = default, Maybe<ParserScope> scope = default, bool parenthesized = false)
        : this(ImmutableArray.CreateRange(contents), tail, scope, parenthesized) { }

    public override Operator Operator => WellKnown.Operators.List;
    public override Atom EmptyElement => WellKnown.Literals.EmptyList;
    public override (string Open, string Close) Braces => ("[", "]");
    public override ITerm CanonicalForm { get; set; }
    protected override AbstractList Create(ImmutableArray<ITerm> contents, Maybe<ParserScope> scope, bool parenthesized) => new List(contents, default, scope, parenthesized);

    public override AbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= [];
        return new List(
            ImmutableArray.CreateRange(Contents.Select(c => c.Instantiate(ctx, vars))),
            Maybe.Some(Tail.Instantiate(ctx, vars)),
            Scope,
            IsParenthesized
        );
    }
    public override AbstractTerm Substitute(Substitution s)
        => new List(
            ImmutableArray.CreateRange(Contents.Select(c => c.Substitute(s))),
            Maybe.Some(Tail.Substitute(s)),
            Scope,
            IsParenthesized
        );

    public override bool Equals(ITerm other)
    {
        if (other is not List list)
            return base.Equals(other);
        var minLength = Math.Min(Contents.Length, list.Contents.Length);
        for (int i = 0; i < minLength; i++)
        {
            if (!Contents[i].Equals(list.Contents[i]))
                return false;
        }
        if (Contents.Length > minLength)
        {
            var tailList = new List(Contents.Skip(minLength).ToArray());
            if (!tailList.Equals(list.Tail))
                return false;
        }
        else if (list.Contents.Length > minLength)
        {
            var tailList = new List(list.Contents.Skip(minLength).ToArray());
            if (!tailList.Equals(Tail))
                return false;
        }
        else if (!Tail.Equals(list.Tail))
        {
            return false;
        }
        return true;
    }

    public override Maybe<SubstitutionMap> Unify(ITerm other)
    {
        if (other is not List list)
            return base.Unify(other);

        var subs = SubstitutionMap.Pool.Acquire();
        var minLength = Math.Min(Contents.Length, list.Contents.Length);

        // Unify the common length parts of both lists
        for (int i = 0; i < minLength; i++)
        {
            if (!Contents[i].Unify(list.Contents[i]).TryGetValue(out var elemSubs))
                return Fail(subs);
            subs = Combine(subs, elemSubs);
        }

        // If the lengths are unequal, unify the tail of the longer list with the tail of the shorter list
        if (Contents.Length > minLength)
        {
            var tailList = new List(Contents.Skip(minLength).ToArray());
            if (!tailList.Unify(list.Tail).TryGetValue(out var remainingSubs))
                return Fail(subs);
            subs = Combine(subs, remainingSubs);
        }
        else if (list.Contents.Length > minLength)
        {
            var tailList = new List(list.Contents.Skip(minLength).ToArray());
            if (!tailList.Unify(Tail).TryGetValue(out var remainingSubs))
                return Fail(subs);
            subs = Combine(subs, remainingSubs);
        }
        // If the lengths are equal, unify the tails
        else if (!Tail.Unify(list.Tail).TryGetValue(out var tailSubs))
        {
            return Fail(subs);
        }
        else
        {
            subs = Combine(subs, tailSubs);
        }

        return subs;

        static SubstitutionMap Combine(SubstitutionMap main, SubstitutionMap release)
        {
            main.AddRange(release);
            SubstitutionMap.Pool.Release(release);
            return main;
        }

        static Maybe<SubstitutionMap> Fail(SubstitutionMap release)
        {
            SubstitutionMap.Pool.Release(release);
            return default;
        }
    }

    public override string Explain(bool canonical)
    {
        if (canonical)
            return CanonicalForm.Explain(true);
        if (IsParenthesized)
            return $"({Inner()})";
        return Inner();
        string Inner()
        {
            if (IsEmpty)
            {
                return Tail.Explain(false);
            }
            var joined = Contents.Join(t => t.Explain(false));
            if (!Tail.Equals(EmptyElement))
            {
                if (Tail is List rest)
                {
                    joined = Contents.Select(t => t.Explain()).Append(rest.Explain(false)[1..^1]).Join();
                    return $"{Braces.Open}{joined}{Braces.Close}";
                }

                return $"{Braces.Open}{joined}|{Tail.Explain()}{Braces.Close}";
            }

            return $"{Braces.Open}{joined}{Braces.Close}";
        }
    }
}
