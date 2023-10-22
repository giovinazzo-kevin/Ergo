namespace Ergo.Solver.BuiltIns;

public readonly struct Evaluation
{
    public readonly bool Result;
    public readonly SubstitutionMap Substitutions { get; }

    public Evaluation(bool result, SubstitutionMap subs = null)
    {
        Result = result;
        Substitutions = subs ?? new();
    }
    public Evaluation(bool result, Substitution sub)
    {
        Result = result;
        Substitutions = new(new[] { sub });
    }
}
