using Ergo.VM.BuiltIns;

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
        /// Returns Ops.Fail or Ops.UpdateEnvironment with the result of the unification.
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
        /// Creates a built-in goal call.
        /// </summary>
        public static Goal BuiltIn(BuiltIn builtIn)
        {
            return builtIn.Compile();
        }
    }
}
