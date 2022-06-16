using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public abstract class AbstractList : IAbstractTerm
{
    public readonly ImmutableArray<ITerm> Contents;
    public readonly bool IsEmpty;
    public readonly ITerm Tail;

    public abstract Atom Functor { get; }
    public abstract ITerm EmptyElement { get; }
    public abstract (string Open, string Close) Braces { get; }

    private readonly Lazy<ITerm> _canonical;
    public ITerm CanonicalForm => _canonical.Value;
    public Signature Signature { get; }

    public AbstractList(ImmutableArray<ITerm> head, Maybe<ITerm> tail = default)
    {
        Contents = head;
        IsEmpty = head.Length == 0;
        Tail = tail.Reduce(some => some, () => EmptyElement);
        _canonical = new(() => Fold(Functor, Tail, head)
            .Reduce<ITerm>(a => a, v => v, c => c));
    }
    public AbstractList(params ITerm[] args) : this(ImmutableArray.CreateRange(args), default) { }
    public AbstractList(IEnumerable<ITerm> args) : this(ImmutableArray.CreateRange(args), default) { }

    public virtual string Explain()
    {
        if (IsEmpty)
        {
            return Tail.Explain();
        }

        var joined = string.Join(',', Contents.Select(t => t.Explain()));
        if (!Tail.Equals(EmptyElement))
        {
            return $"{Braces.Open}{joined}|{Tail.Explain()}{Braces.Close}";
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
