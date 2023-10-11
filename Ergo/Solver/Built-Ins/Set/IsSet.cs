﻿namespace Ergo.Solver.BuiltIns;

public sealed class IsSet : SolverBuiltIn
{
    public IsSet()
        : base("", new("is_set"), 1, WellKnown.Modules.Set)
    {

    }

    public override IEnumerable<Evaluation> Apply(SolverContext solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].IsAbstract<Set>().TryGetValue(out _))
            yield return True();
        else yield return False();
    }
}