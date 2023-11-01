namespace Ergo.Solver.BuiltIns;

public sealed class PeekChar : SolverBuiltIn
{
    public PeekChar()
        : base("", new("peek_char"), 1, WellKnown.Modules.IO)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        int value = context.Solver.In.Peek();
        ITerm charTerm = value != -1 ? new Atom((char)value) : new Atom("end_of_file");

        if (LanguageExtensions.Unify(arguments[0], charTerm).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }
        yield return False();
    }
}
