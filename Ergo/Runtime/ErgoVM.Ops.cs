using static Ergo.Runtime.ErgoVM;

namespace Ergo.Runtime;

public static class Ops
{
    public static Op NoOp => _ => { };
    public static Op Fail => vm => vm.Fail();
    public static Op Throw(ErrorType ex, params object[] args) => vm => vm.Throw(ex, args);
    public static Op Cut => vm => vm.Cut();
    public static Op Solution => vm => vm.Solution();
    public static Op And2(Op a, Op b) => vm =>
    {
        // Cache continuation so that goals calling PushChoice know where to continue from.
        var @continue = vm.@continue;
        vm.@continue += b;
        a(vm);
        // Restore previous continuation before potentially yielding control to another And.
        vm.@continue = @continue;
        switch (vm.State)
        {
            case VMState.Fail: return;
            case VMState.Solution: vm.MergeEnvironment(); break;
        }
        b(vm);
    };
    public static Op And(params Op[] goals)
    {
        if (goals.Length == 0)
            return NoOp;
        if (goals.Length == 1)
            return goals[0];
        return vm =>
        {
            ContinueFrom(vm, 0);
            void ContinueFrom(ErgoVM vm, int j)
            {
                if (j >= goals.Length) return;
                for (int i = j; i < goals.Length; i++)
                {
                    var k = i + 1;
                    // Cache continuation so that goals calling PushChoice know where to continue from.
                    var @continue = vm.@continue;
                    vm.@continue = k >= goals.Length
                    ? @continue
                    : @continue == NoOp
                        ? vm => ContinueFrom(vm, k)
                        : vm => { @continue(vm); ContinueFrom(vm, k); };
                    goals[i](vm);
                    // Restore previous continuation before potentially yielding control to another And.
                    vm.@continue = @continue;
                    switch (vm.State)
                    {
                        // Ready can be used as a non-failing way to halt with a solution.
                        case VMState.Ready:
                            {
                                vm.State = VMState.Solution;
                                return;
                            }
                        case VMState.Fail: return;
                        case VMState.Solution: vm.MergeEnvironment(); break;
                    }
                }
                vm.Solution();
            }
        };
    }
    public static Op Or(params Op[] branches)
    {
        if (branches.Length == 0)
            return Fail;
        if (branches.Length == 1)
            return branches[0];
        return vm =>
        {
            for (int i = branches.Length - 1; i >= 1; i--)
            {
                vm.PushChoice(Branch(branches[i]));
            }
            Branch(branches[0])(vm);

            Op Branch(Op branch) => vm =>
            {
                branch(vm);
                vm.SuccessToSolution();
            };
        };
    }
    public static Op IfThenElse(Op condition, Op consequence, Op alternative) => vm =>
    {
        var backupEnvironment = vm.CloneEnvironment();
        var numCp = vm.NumChoicePoints;
        condition(vm);
        if (vm.State != VMState.Fail)
        {
            if (vm.State == VMState.Solution)
                vm.MergeEnvironment();
            // Discard choice points created by the condition. Similar to a cut but more specific.
            vm.DiscardChoices(vm.NumChoicePoints - numCp);
            consequence(vm);
        }
        else
        {
            vm.State = VMState.Success;
            vm.Environment = backupEnvironment;
            alternative(vm);
            vm.SuccessToSolution();
        }
    };
    public static Op IfThen(Op condition, Op consequence) => IfThenElse(condition, consequence, NoOp);
    /// <summary>
    /// Adds the current set of substitutions to te VM's environment, and then releases it back into the substitution map pool.
    /// </summary>
    public static Op UpdateEnvironment(SubstitutionMap subsToAdd) => vm =>
    {
        vm.Environment.AddRange(subsToAdd);
        SubstitutionMap.Pool.Release(subsToAdd);
    };
    public static Op SetEnvironment(SubstitutionMap newEnv) => vm =>
    {
        SubstitutionMap.Pool.Release(vm.Environment);
        vm.Environment = newEnv;
    };
    /// <summary>
    /// Converts a query into the corresponding Op.
    /// </summary>
    public static Op Goals(NTuple goals)
    {
        if (goals.Contents.Length == 0)
            return NoOp;
        if (goals.Contents.Length == 1)
            return Goal(goals.Contents[0]);
        return And(goals.Contents.Select(x => Goal(x, dynamic: false)).ToArray());
    }
    /// <summary>
    /// Calls an individual goal.
    /// </summary>
    public static Op Goal(ITerm goal, bool dynamic = false)
    {
        return null;
    }
}