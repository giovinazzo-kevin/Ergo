using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public abstract class AbstractList : IAbstractTerm
{
    private readonly Lazy<ITerm> _canonical, _tail;

    public readonly ImmutableArray<ITerm> Contents;
    public readonly bool IsEmpty;

    public abstract Atom Functor { get; }
    public abstract Atom EmptyElement { get; }
    public abstract (string Open, string Close) Braces { get; }
    public Signature Signature { get; }

    public ITerm CanonicalForm => _canonical.Value;
    public ITerm Tail => _tail.Value;

    public AbstractList(ImmutableArray<ITerm> head, Maybe<ITerm> tail = default)
    {
        Contents = head;
        IsEmpty = head.Length == 0;
        _tail = new(() => tail.Reduce(some => some, () => EmptyElement
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this))));
        _canonical = new(() => Fold(Functor, Tail, head)
            .Reduce<ITerm>(a => a, v => v, c => c)
            .WithAbstractForm(Maybe.Some<IAbstractTerm>(this)));
    }
    public AbstractList(params ITerm[] args) : this(ImmutableArray.CreateRange(args), default) { }
    public AbstractList(IEnumerable<ITerm> args) : this(ImmutableArray.CreateRange(args), default) { }

    public virtual string Explain()
    {
        if (IsEmpty)
        {
            return Tail.WithAbstractForm(default).Explain();
        }

        var joined = string.Join(',', Contents.Select(t => t.Explain()));
        if (!Tail.Equals(EmptyElement))
        {
            return $"{Braces.Open}{joined}|{Tail.WithAbstractForm(default).Explain()}{Braces.Close}";
        }

        return $"{Braces.Open}{joined}{Braces.Close}";
    }
    public virtual Maybe<IEnumerable<Substitution>> Unify(IAbstractTerm other)
    {
        if (other is not AbstractList list)
            return default;
        return CanonicalForm.Unify(list.CanonicalForm);
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
}
