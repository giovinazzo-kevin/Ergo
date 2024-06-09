using Ergo.Lang.Compiler;

namespace Ergo.Runtime;

public partial class ErgoVM
{
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
            var backupEnvironment = vm.Memory.SaveState();
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
                vm.Memory.LoadState(backupEnvironment);
                alternative(vm);
                vm.SuccessToSolution();
            }
        };
        public static Op IfThen(Op condition, Op consequence) => IfThenElse(condition, consequence, NoOp);
        /// <summary>
        /// Converts a query into the corresponding Op.
        /// </summary>
        public static Op Goals(ITermAddress[] goals)
        {
            if (goals.Length == 0)
                return NoOp;
            if (goals.Length == 1)
                return Goal(goals[0]);
            return And(goals.Select(x => Goal(x, dynamic: false)).ToArray());
        }

        /// <summary>
        /// Calls an individual goal.
        /// </summary>
        public static Op Goal(ITermAddress goal, bool dynamic = false) => vm =>
        {
            const string cutValue = "!";
            var op = goal switch
            {
                AbstractAddress a when vm.Memory[a].Type == typeof(NTuple)
                    && vm.Memory[(StructureAddress)vm.Memory[a].Address] is var goals => Goals(goals),
                ConstAddress a when vm.Memory[a] is Atom { Value: true } => NoOp,
                ConstAddress a when vm.Memory[a] is Atom { Value: false } => Fail,
                ConstAddress a when vm.Memory[a] is Atom { Value: cutValue, IsQuoted: false } => Cut,
                _ => Resolve
            };
            op(vm);

            void Resolve(ErgoVM vm)
            {
                var matchEnum = GetEnumerator();
                NextMatch(vm);
                void NextMatch(ErgoVM vm)
                {
                    var anyMatch = false;
                    if (matchEnum.MoveNext())
                    {
                        var predicate = matchEnum.Current;
                        if (dynamic && !predicate.IsDynamic)
                            return;
                        anyMatch = true;
                        // Push a choice point for this match. If it fails, it will be retried until there are no more matches.
                        vm.PushChoice(NextMatch);
                        // Decide how to execute this goal depending on whether:
                        Op runGoal = NoOp;
                        var pred = matchEnum.Current.Body;
                        // Track the number of choice points before executing the goal (up to the one we just pushed)
                        var numCp = vm.NumChoicePoints;
                        // Actually execute the goal. This may produce success, a solution, or set the VM in a failure state.
                        runGoal(vm);
                        // If the VM is in success state, promote that success to a solution by pushing the current environment.
                        vm.SuccessToSolution();
                    }
                    else if (!anyMatch)
                    {
                        // Essentially, when we exhaust the list of matches for 'goal', we set the VM in a failure state to signal backtracking.
                        vm.Fail();
                    }
                }
            }
            IEnumerator<TermMemory.PredicateCell> GetEnumerator()
            {
                if (!vm.CKB.GetMatches(goal).TryGetValue(out var matches))
                    return Enumerable.Empty<TermMemory.PredicateCell>().GetEnumerator();
                return matches.GetEnumerator();
            }
        };
    }
}
