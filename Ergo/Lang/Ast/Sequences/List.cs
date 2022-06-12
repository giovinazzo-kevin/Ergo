using System.Collections.Immutable;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct List : ISequence
{
    public static readonly Atom CanonicalFunctor = new("[|]");
    public static readonly Atom EmptyLiteral = new("[]");

    public static readonly List Empty = new(ImmutableArray<ITerm>.Empty);

    public ITerm Root { get; }
    public Atom Functor { get; }
    public ImmutableArray<ITerm> Contents { get; }
    public ITerm EmptyElement { get; }
    public bool IsEmpty { get; }
    public bool IsParenthesized { get; }

    public readonly ITerm Tail;

    public List(ImmutableArray<ITerm> head, Maybe<ITerm> tail = default, bool parens = false)
    {
        Functor = CanonicalFunctor;
        EmptyElement = EmptyLiteral;
        Contents = head;
        IsEmpty = head.Length == 0;
        Tail = tail.Reduce(some => some, () => EmptyLiteral);
        IsParenthesized = parens;
        Root = ISequence.Fold(Functor, Tail, head)
            .Reduce<ITerm>(a => a, v => v, c => c.AsParenthesized(parens), d => d);
    }
    public List(params ITerm[] args) : this(ImmutableArray.CreateRange(args), default, false) { }
    public List(IEnumerable<ITerm> args) : this(ImmutableArray.CreateRange(args), default, false) { }

    public static bool TryUnfold(ITerm t, out List expr)
    {
        expr = default;
        if (t.Equals(EmptyLiteral))
        {
            expr = Empty;
            return true;
        }

        if (t is Complex c && CanonicalFunctor.Equals(c.Functor))
        {
            var args = new List<ITerm>() { c.Arguments[0] };
            if (c.Arguments.Length == 1 || c.Arguments[1].Equals(EmptyLiteral))
            {
                expr = new List(ImmutableArray.CreateRange(args));
                return true;
            }

            if (c.Arguments.Length != 2)
                return false;
            if (TryUnfold(c.Arguments[1], out var subExpr))
            {
                args.AddRange(subExpr.Contents);
                expr = new List(ImmutableArray.CreateRange(args), Maybe.Some(subExpr.Tail));
                return true;
            }
            else
            {
                expr = new List(ImmutableArray.CreateRange(args), Maybe.Some(c.Arguments[1]));
                return true;
            }
        }

        return false;
    }

    public string Explain(bool canonical = false)
    {
        if (IsParenthesized)
        {
            return $"({Inner(this)})";
        }

        return Inner(this);
        string Inner(List seq)
        {
            if (seq.IsEmpty)
            {
                return seq.Tail.Explain(canonical);
            }

            var joined = string.Join(',', seq.Contents.Select(t => t.Explain(canonical)));
            if (!seq.Tail.Equals(seq.EmptyElement))
            {
                return $"[{joined}|{seq.Tail.Explain(canonical)}]";
            }

            return $"[{joined}]";
        }
    }

    public ISequence Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) =>
        new List(ImmutableArray.CreateRange(Contents.Select(arg => arg.Instantiate(ctx, vars))), Maybe.Some(Tail.Instantiate(ctx, vars)));

    public ISequence Substitute(IEnumerable<Substitution> subs) =>
        new List(ImmutableArray.CreateRange(Contents.Select(arg => arg.Substitute(subs))), Maybe.Some(Tail.Substitute(subs)));
}

