using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public sealed class List : AbstractList
{
    public static readonly List Empty = new(ImmutableArray<ITerm>.Empty, default, default);
    public readonly ITerm Tail;
    public List(ImmutableArray<ITerm> contents, Maybe<ITerm> tail, Maybe<ParserScope> scope)
        : base(contents, scope)
    {
        Tail = tail.GetOr(EmptyElement);
        CanonicalForm = Fold(Operator, Tail, contents);
    }
    public List(IEnumerable<ITerm> contents, Maybe<ITerm> tail, Maybe<ParserScope> scope)
        : this(ImmutableArray.CreateRange(contents), tail, scope) { }

    public override Operator Operator => WellKnown.Operators.List;
    public override Atom EmptyElement => WellKnown.Literals.EmptyList;
    public override (string Open, string Close) Braces => ("[", "]");
    protected override ITerm CanonicalForm { get; }
    protected override AbstractList Create(ImmutableArray<ITerm> contents, Maybe<ParserScope> scope) => new List(contents, default, scope);

    public override AbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        return new List(
            ImmutableArray.CreateRange(Contents.Select(c => c.Instantiate(ctx, vars))),
            Maybe.Some(Tail.Instantiate(ctx, vars)),
            Scope
        );
    }
    public override AbstractTerm Substitute(Substitution s)
        => new List(
            ImmutableArray.CreateRange(Contents.Select(c => c.Substitute(s))),
            Maybe.Some(Tail.Substitute(s)),
            Scope
        );

    public override string Explain(bool canonical)
    {
        if (canonical)
            return CanonicalForm.Explain(true);
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
