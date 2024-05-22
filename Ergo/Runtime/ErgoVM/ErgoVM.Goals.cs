namespace Ergo.Runtime;

public partial class ErgoVM
{
    /// <summary>
    /// Utility goals.
    /// </summary>
    public static class Goals
    {
        /// <summary>
        /// Unifies arg0 with arg1, then performs Ops.Fail or Ops.UpdateEnvironment with the result of the unification.
        /// </summary>
        public static Op Unify2 => vm =>
        {
            if (!vm.Memory.Unify(vm.Arg2(1), vm.Arg2(2), transaction: true))
                Ops.Fail(vm);
        };
    }
}
