using System.Collections.Immutable;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct UntypedSequence : ISequence
{
    public ITerm Root { get; }
    public Atom Functor { get; }
    public ImmutableArray<ITerm> Contents { get; }
    public ITerm EmptyElement { get; }
    public bool IsEmpty { get; }
    public bool IsParenthesized { get; }

    public UntypedSequence(Atom functor, ITerm empty, ImmutableArray<ITerm> args, bool parens)
    {
        Functor = functor;
        EmptyElement = empty;
        Contents = args;
        IsEmpty = args.Length == 0;
        Root = ISequence.Fold(Functor, EmptyElement, args)
            .Reduce<ITerm>(a => a, v => v, c => c.AsParenthesized(parens), d => d);
        IsParenthesized = parens;
    }

    public string Explain(bool canonical = false)
    {
        if (IsParenthesized)
        {
            return $"{{{Inner(this)}}}";
        }

        return Inner(this);
        string Inner(UntypedSequence seq)
        {
            if (seq.IsEmpty)
            {
                return seq.EmptyElement.Explain(canonical);
            }

            var joined = string.Join(',', seq.Contents.Select(t => t.Explain(canonical)));
            if (seq.Contents.Length != 1)
            {
                return $"({joined})";
            }

            return joined;
        }
    }

    public ISequence Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) =>
        new UntypedSequence(Functor, EmptyElement, ImmutableArray.CreateRange(Contents.Select(arg => arg.Instantiate(ctx, vars))), IsParenthesized);

    public ISequence Substitute(IEnumerable<Substitution> subs) =>
        new UntypedSequence(Functor, EmptyElement, ImmutableArray.CreateRange(Contents.Select(arg => arg.Substitute(subs)).ToArray()), IsParenthesized);
}
