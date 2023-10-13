namespace Ergo.Solver.BuiltIns;

public sealed class RunTask : SolverBuiltIn
{
    public RunTask()
        : base("", new("run_task"), Maybe<int>.Some(1), WellKnown.Modules.Async)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        var any = false;
        foreach (var sol in context.Solver.Solve(new Query(args[0]), scope))
        {
            yield return True(sol.Substitutions);
            any = true;
        }
        if (!any)
            yield return False();
    }
}
