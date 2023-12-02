namespace Ergo.Runtime;

public partial class ErgoVM
{
    public static class Goals
    {
        /// <summary>
        /// Unifies arg0 with arg1, then performs Ops.Fail or Ops.UpdateEnvironment with the result of the unification.
        /// </summary>
        public static Op Unify2 => vm =>
        {
            // In this case unification is really just the act of updating the environment with the *result* of unification.
            // The Op is provided for convenience and as a wrapper. Note that unification is performed eagerly in this case. 
            if (vm.Arg(0).Unify(vm.Arg(1)).TryGetValue(out var subs))
                Ops.UpdateEnvironment(subs)(vm);
            else Ops.Fail(vm);
        };
    }
}
