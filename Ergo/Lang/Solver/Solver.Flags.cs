using System;

namespace Ergo.Lang
{

    public partial class Solver
    {
        [Flags]
        public enum SolverFlags
        {
            Default = ThrowOnPredicateNotFound
            , None = 0
            , ThrowOnPredicateNotFound = 1
        }
    }
}
