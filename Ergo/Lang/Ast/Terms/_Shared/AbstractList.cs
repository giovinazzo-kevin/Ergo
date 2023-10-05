using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public abstract class AbstractList : IAbstractTerm
{
    public readonly ImmutableArray<ITerm> Contents;
    public readonly bool IsEmpty;

    public abstract Operator Operator { get; }
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
            return EmptyElement.Explain(true);
        var joined = Contents.Join(t => t.Explain(true));
        return $"{Braces.Open}{joined}{Braces.Close}";
    }
    public virtual Maybe<SubstitutionMap> Unify(IAbstractTerm other)
    {
        if (other is not AbstractList list)
            return CanonicalForm.Unify(other.CanonicalForm);
        if (list.Braces != Braces)
            return default;
        var u = CanonicalForm.Unify(list.CanonicalForm);
        return u;
    }

    public virtual IAbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        var builder = Contents.ToArray();
        for (int i = 0; i < builder.Length; i++)
        {
            builder[i] = Contents[i].Instantiate(ctx, vars);
        }
        return Create(builder.ToImmutableArray());
    }
    public virtual IAbstractTerm Substitute(Substitution s)
    {
        var builder = Contents.ToArray();
        for (int i = 0; i < builder.Length; i++)
        {
            builder[i] = Contents[i].Substitute(s);
        }
        return Create(builder.ToImmutableArray());
    }
    public virtual IAbstractTerm Substitute(SubstitutionMap s)
    {
        var builder = Contents.ToArray();
        for (int i = 0; i < builder.Length; i++)
        {
            builder[i] = Contents[i].Substitute(s);
        }
        return Create(builder.ToImmutableArray());
    }
    /// <summary>
    /// Folds a list in the canonical way by composing f/2 recursively, appending the empty element at the end.
    /// </summary>
    public static ITerm Fold(Operator op, ITerm tail, ImmutableArray<ITerm> args)
    {
        if (op.Fixity != Fixity.Infix)
            throw new InvalidOperationException("Cannot fold a non-infix operator.");
        if (args.Length == 0)
            return tail;
        if (args.Length == 1)
            return new Complex(op.CanonicalFunctor, args[0], tail)
                .AsOperator(op);
        return args
            .Append(tail)
            .Reverse()
            .Aggregate((a, b) => new Complex(op.CanonicalFunctor, b, a)
                .AsOperator(op));
    }

    /// <summary>
    /// Folds a list in a non-canonical way that omits the trailing empty element.
    /// Note that the empty element is still returned for 0-length lists.
    /// </summary>
    public static ITerm FoldNoEmptyTail(Operator op, ITerm emptyElement, ImmutableArray<ITerm> args)
    {
        if (op.Fixity != Fixity.Infix)
            throw new InvalidOperationException("Cannot fold a non-infix operator.");
        // NOTE: It seems to make more sense to fold tuples and sets this way, since pattern matching is reserved to lists.
        if (args.Length == 0)
            return emptyElement;
        if (args.Length == 1)
            return new Complex(op.CanonicalFunctor, args[0]);
        return args
            .Reverse()
            .Aggregate((a, b) => new Complex(op.CanonicalFunctor, b, a)
                .AsOperator(op));
    }

    /// <summary>
    /// Folds a list in a non-canonical way that omits the trailing empty element and parenthesizes the single element instead of returning a malformed complex.
    /// Note that the empty element is still returned for 0-length lists.
    /// </summary>
    public static ITerm FoldNoEmptyTailParensSingle(Operator op, ITerm emptyElement, ImmutableArray<ITerm> args)
    {
        if (op.Fixity != Fixity.Infix)
            throw new InvalidOperationException("Cannot fold a non-infix operator.");
        // NOTE: It seems to make more sense to fold tuples and sets this way, since pattern matching is reserved to lists.
        if (args.Length == 0)
            return emptyElement;
        if (args.Length == 1)
            return args[0].AsParenthesized(true);
        return args
            .Reverse()
            .Aggregate((a, b) => new Complex(op.CanonicalFunctor, b, a)
                .AsOperator(op));
    }

    public static Maybe<IEnumerable<ITerm>> Unfold(ITerm term, ITerm emptyElement, Func<ITerm, bool> matchTail, HashSet<Atom> functors)
    {
        if (term is Complex { Arity: 2, Functor: var f } c && functors.Contains(f))
            return Maybe.Some(Inner());
        if (term is Atom && term.Equals(emptyElement))
            return Maybe.Some<IEnumerable<ITerm>>(new ITerm[] { term });
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

    public abstract Maybe<IAbstractTerm> FromCanonicalTerm(ITerm c);
}
