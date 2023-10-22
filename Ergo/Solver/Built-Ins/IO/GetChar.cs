namespace Ergo.Solver.BuiltIns;

public sealed class GetChar : SolverBuiltIn
{
    public GetChar()
        : base("", new("get_char"), 1, WellKnown.Modules.IO)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        int value;
        do
        {
            value = context.Solver.In.Read();
        } while (value != '\n' && value != -1);

        ITerm charTerm = value != -1 ? new Atom((char)value) : new Atom("end_of_file");

        if (LanguageExtensions.Unify(arguments[0], charTerm).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }
        yield return False();
    }
}