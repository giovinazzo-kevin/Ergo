namespace Ergo.Solver.BuiltIns;

public readonly struct Evaluation
{
    public readonly ITerm Result;
    public readonly SubstitutionMap Substitutions { get; }

    public Evaluation(ITerm result, SubstitutionMap subs = null)
    {
        Result = result;
        Substitutions = subs ?? new();
    }
    public Evaluation(ITerm result, Substitution sub)
    {
        Result = result;
        Substitutions = new(new[] { sub });
    }
}
