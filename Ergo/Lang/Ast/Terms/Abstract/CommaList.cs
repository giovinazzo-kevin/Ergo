namespace Ergo.Lang.Ast;

public sealed class CommaList : AbstractList
{
    public static CommaList Empty => new(ImmutableArray<ITerm>.Empty);

    public CommaList(ImmutableArray<ITerm> head, Maybe<ITerm> tail = default)
        : base(head, tail) { }
    public CommaList(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents), default) { }
    public override Atom Functor => WellKnown.Functors.CommaList.First();
    public override ITerm EmptyElement => WellKnown.Literals.EmptyCommaList;
    public override (string Open, string Close) Braces => ("(", ")");
}
