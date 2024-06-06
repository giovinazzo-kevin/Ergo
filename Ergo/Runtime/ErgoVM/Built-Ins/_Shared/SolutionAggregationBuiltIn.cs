namespace Ergo.Runtime.BuiltIns;

public abstract class SolutionAggregationBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module) : BuiltIn(documentation, functor, arity, module)
{
    protected static IEnumerable<(List ArgVars, List ListTemplate, List ListVars)> AggregateSolutions(ErgoVM vm)
    {
        var (template, goal, instances) = (
            vm.Memory.Dereference(vm.Args2[1]),
            vm.Memory.Dereference(vm.Args2[2]),
            vm.Memory.Dereference(vm.Args2[3]));
        if (goal is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, goal.Explain());
            yield break;
        }

        IEnumerable<Variable> templateVars = [];
        while (goal is Complex c && WellKnown.Functors.ExistentialQualifier.Contains(c.Functor))
        {
            templateVars = templateVars.Concat(c.Arguments[0].Variables);
            goal = c.Arguments[1];
        }

        templateVars = templateVars.Concat(template.Variables)
            .ToHashSet();

        var variable = (Variable)"TMP_BAGOF__"; // TODO: something akin to thread.next_free_variable() from TauProlog
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
        var query = new Query(goalClauses);
        var newVm = vm.ScopedInstance();
        newVm.Query = newVm.CompileQuery(query);
        newVm.Run();
        if (newVm.State == ErgoVM.VMState.Fail)
            yield break;
        foreach (var sol in newVm.Solutions
            .Select(sol =>
            {
                var arg = (NTuple)(sol.Substitutions[variable]);

                var argVars = arg.Contents[0];
                var argTmpl = arg.Contents[1];
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
            .Select(kv => (kv.Key, new List(kv), listVars)))
        {
            yield return sol;
        }
    }
}
