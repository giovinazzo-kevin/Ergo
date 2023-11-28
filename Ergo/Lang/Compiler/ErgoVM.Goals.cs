using Ergo.Solver.BuiltIns;

namespace Ergo.Lang.Compiler;

public partial class ErgoVM
{
    public static class Goals
    {
        public static Goal True => _ => Ops.NoOp;
        public static Goal False => _ => Ops.Fail;
        public static Goal Throw(ErrorType ex, params object[] args) => _ => Ops.Throw(ex, args);
        /// <summary>
        /// Performs the unification at the time when Unify is called.
        /// Either returns Ops.Fail or Ops.UpdateEnvironment with the result of unification.
        /// </summary>
        public static Goal Unify => args =>
        {
            // In this case unification is really just the act of updating the environment with the *result* of unification.
            // The Op is provided for convenience and as a wrapper. Note that unification is performed eagerly in this case. 
            if (args[0].Unify(args[1]).TryGetValue(out var subs))
                return Ops.UpdateEnvironment(subs);
            return Ops.Fail;
        };
        /// <summary>
        /// Calls a built-in by passing it the matching goal's arguments.
        /// </summary>
        public static Goal BuiltIn(SolverBuiltIn builtIn)
        {
            var comp = builtIn.Compile();
            return args => vm =>
            {
                var op = comp(args);
                // Temporary: once Solver is dismantled, remove this check and allow a builtin to resolve to noop.
                if (Ops.NoOp != op)
                {
                    op(vm);
                    return;
                }
                #region temporary code
                var next = builtIn.Apply(vm.Context, vm.Scope, args).GetEnumerator();
                NextGoal(vm);
                void NextGoal(ErgoVM vm)
                {
                    if (next.MoveNext())
                    {
                        if (!next.Current.Result)
                        {
                            vm.Fail();
                            return;
                        }
                        vm.Solution(next.Current.Substitutions);
                        vm.PushChoice(NextGoal);
                    }
                    else
                    {
                        vm.Fail();
                    }
                }
                #endregion
            };
        }
    }
}
