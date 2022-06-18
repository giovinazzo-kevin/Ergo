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
            return EmptyElement.WithAbstractForm(default).Explain();

        var joined = string.Join(',', Contents.Select(t => t.Explain()));
        return $"{Braces.Open}{joined}{Braces.Close}";
    }
    public virtual Maybe<IEnumerable<Substitution>> Unify(IAbstractTerm other)
    {
        if (other is not AbstractList list)
            return CanonicalForm.Unify(other.CanonicalForm);
        if (list.Braces != Braces)
            return default;
        var u = CanonicalForm.WithAbstractForm(default).Unify(list.CanonicalForm.WithAbstractForm(default));
        return u;
        //IEnumerable<Substitution> Inner()
        //{
        //    foreach (var sub in u.GetOrThrow())
        //    {
        //        var ret = sub;
        //        if (Unfold(ret.Rhs, Functor) is { HasValue: true } unfold)
        //            ret = ret.WithRhs(Create(ImmutableArray.CreateRange(unfold.GetOrThrow())).CanonicalForm);
        //        yield return ret;
        //    }
        //}
    }

    public virtual IAbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        return Create(ImmutableArray.CreateRange(Contents.Select(c => c.Instantiate(ctx, vars))));
    }
    public virtual IAbstractTerm Substitute(Substitution s)
        => Create(ImmutableArray.CreateRange(Contents.Select(c => c.Substitute(s))));

    protected static ITerm Fold(Atom functor, ITerm emptyElement, ImmutableArray<ITerm> args)
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

    protected static Maybe<IEnumerable<ITerm>> Unfold(ITerm term, Func<ITerm, bool> matchTail, params Atom[] functors)
    {
        if (term is Complex { Arity: 2, Functor: var f } c && functors.Contains(f))
            return Maybe.Some(Inner());
        return default;

        IEnumerable<ITerm> Inner()
        {
            var list = new List<ITerm>();
            while (term is Complex { Arity: 2, Functor: var f } c && functors.Contains(f))
            {
                list.Add(c.Arguments[0]);
                term = c.Arguments[1];
            }

            if (!matchTail(term))
            {
                return Enumerable.Empty<ITerm>();
            }

            list.Add(term);
            return list;
        }
    }

}
