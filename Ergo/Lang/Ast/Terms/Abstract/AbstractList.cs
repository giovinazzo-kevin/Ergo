using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public abstract class AbstractList : IAbstractTerm
{
    public readonly ImmutableArray<ITerm> Contents;
    public readonly bool IsEmpty;

    public abstract Atom Functor { get; }
    public abstract Atom EmptyElement { get; }
    public abstract (string Open, string Close) Braces { get; }
    public Signature Signature { get; }

    public abstract ITerm CanonicalForm { get; }

    public AbstractList(ImmutableArray<ITerm> head)
    {
        Contents = head;
        IsEmpty = head.Length == 0;
    }
    public AbstractList(params ITerm[] args) : this(ImmutableArray.CreateRange(args)) { }
    public AbstractList(IEnumerable<ITerm> args) : this(ImmutableArray.CreateRange(args)) { }
    protected abstract AbstractList Create(ImmutableArray<ITerm> head);

    public virtual string Explain()
    {
        if (IsEmpty)
        {
            return EmptyElement.WithAbstractForm(default).Explain();
        }

        var joined = string.Join(',', Contents.Select(t => t.Explain()));
        return $"{Braces.Open}{joined}{Braces.Close}";
    }
    public virtual Maybe<IEnumerable<Substitution>> Unify(IAbstractTerm other)
    {
        if (other is not AbstractList list || list.Braces != Braces)
            return default;
        var u = CanonicalForm.WithAbstractForm(default).Unify(list.CanonicalForm.WithAbstractForm(default));
        if (!u.HasValue)
            return default;
        return Maybe.Some(Inner());
        IEnumerable<Substitution> Inner()
        {
            foreach (var sub in u.GetOrThrow())
            {
                if (Unfold(sub.Rhs, Functor) is { HasValue: true } unfold)
                    yield return sub.WithRhs(Create(ImmutableArray.CreateRange(unfold.GetOrThrow().SkipLast(1))).CanonicalForm);
                else
                    yield return sub;
            }
        }
    }

    public static ITerm Fold(Atom functor, ITerm emptyElement, ImmutableArray<ITerm> args)
    {
        if (args.Length == 0)
            return emptyElement;
        if (args.Length == 1)
            return new Complex(functor, args[0], emptyElement);
        return args
            .Append(emptyElement)
            .Reverse()
            .Aggregate((a, b) => new Complex(functor, b, a)
                .AsOperator(OperatorAffix.Infix));
    }

    public static Maybe<IEnumerable<ITerm>> Unfold(ITerm term, params Atom[] functors)
    {
        if (term.IsAbstractTerm<CommaList>(out var comma))
            return Maybe.Some(comma.Contents.AsEnumerable());

        if (term is Complex { Arity: 2, Functor: var f } c && functors.Contains(f))
            return Maybe.Some(Inner());
        return default;

        IEnumerable<ITerm> Inner()
        {
            while (term is Complex { Arity: 2, Functor: var f } c && functors.Contains(f))
            {
                yield return c.Arguments[0];
                term = c.Arguments[1];
            }

            yield return term;
        }
    }
}
