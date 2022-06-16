namespace Ergo.Lang.Ast;

public sealed class BracyList : AbstractList
{
    public static BracyList Empty => new(ImmutableArray<ITerm>.Empty);

    public BracyList(ImmutableArray<ITerm> head, Maybe<ITerm> tail = default)
        : base(head, tail) { }
    public BracyList(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents), default) { }
    public override Atom Functor => WellKnown.Functors.BracyList.First();
    public override ITerm EmptyElement => WellKnown.Literals.EmptyBracyList;
    public override (string Open, string Close) Braces => ("{", "}");
}
