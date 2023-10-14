namespace Ergo.Solver.BuiltIns;

public abstract class SolutionAggregationBuiltIn : SolverBuiltIn
{
    protected SolutionAggregationBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
        : base(documentation, functor, arity, module)
    {
    }

    protected IEnumerable<(List ArgVars, List ListTemplate, List ListVars)> AggregateSolutions(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        var (template, goal, instances) = (args[0], args[1], args[2]);
        scope = scope.WithDepth(scope.Depth + 1)
            .WithCaller(scope.Callee)
            .WithCallee(GetStub(args))
            .WithChoicePoint();

        if (goal is Variable)
        {
            throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, goal.Explain());
        }

        var templateVars = Enumerable.Empty<Variable>();
        while (goal is Complex c && WellKnown.Functors.ExistentialQualifier.Contains(c.Functor))
        {
            templateVars = templateVars.Concat(c.Arguments[0].Variables);
            goal = c.Arguments[1];
        }

        templateVars = templateVars.Concat(template.Variables)
            .ToHashSet();

        var variable = new Variable("TMP_BAGOF__"); // TODO: something akin to thread.next_free_variable() from TauProlog
        var freeVars = goal.Variables.Where(v => !templateVars.Contains(v))
            .ToHashSet();
        var listVars = new List(freeVars.Cast<ITerm>(), default, default);

        var goalClauses = new NTuple(new ITerm[] {
            goal,
            new Complex(WellKnown.Operators.Unification.CanonicalFunctor,
                variable,
                new NTuple(new[]{ listVars, template }, default))
            .AsOperator(WellKnown.Operators.Unification)
        }, default);
        var solutions = solver.Solve(new(goalClauses), scope)
            .Select(s => s.Simplify());
        foreach (var sol in solutions
            .Select(sol =>
            {
                var arg = (Complex)(sol.Substitutions[variable]);

                var argVars = arg.Arguments[0];
                var argTmpl = arg.Arguments[1];
                var varTmpl = argTmpl.Variables
                    .ToHashSet();

                var subTmpl = sol.Substitutions
                    .Where(s => s.Lhs is Variable && s.Rhs is Variable)
                    .Where(s => varTmpl.Contains((Variable)s.Rhs) && !templateVars.Contains((Variable)s.Lhs) && !freeVars.Contains((Variable)s.Lhs))
                    .Select(s => new Substitution(s.Lhs, s.Rhs));

                argVars = argVars.Substitute(subTmpl);
                argTmpl = argTmpl.Substitute(subTmpl);
                var argList = (List)argVars;
                return (argList, argTmpl);
            })
            .ToLookup(sol => sol.argList, sol => sol.argTmpl)
            .Select(kv => (kv.Key, new List(kv, default, default), listVars)))
        {
            yield return sol;
        }
    }
}
