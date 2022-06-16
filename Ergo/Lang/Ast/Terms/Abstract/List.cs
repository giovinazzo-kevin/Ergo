namespace Ergo.Lang.Ast;

public sealed class List : AbstractList
{
    public static List Empty => new(ImmutableArray<ITerm>.Empty);

    public List(ImmutableArray<ITerm> head, Maybe<ITerm> tail = default)
        : base(head, tail) { }
    public List(IEnumerable<ITerm> contents)
        : this(ImmutableArray.CreateRange(contents), default) { }

    public override Atom Functor => WellKnown.Functors.List.First();
    public override Atom EmptyElement => WellKnown.Literals.EmptyList;
    public override (string Open, string Close) Braces => ("[", "]");
}
