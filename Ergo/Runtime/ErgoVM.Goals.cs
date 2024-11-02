namespace Ergo.Runtime;

public partial class ErgoVM
{
    /// <summary>
    /// Utility goals.
    /// </summary>
    public static class Goals
    {
        /// <summary>
        /// Unifies arg0 with arg1.
        /// </summary>
        public static Op Unify2 => vm =>
        {
            var lhs = vm.Arg(0);
            var rhs = vm.Arg(1);
            var unif = vm.Memory.Unify(lhs, rhs);
            if (!unif)
                Ops.Fail(vm);
        };
    }
}
