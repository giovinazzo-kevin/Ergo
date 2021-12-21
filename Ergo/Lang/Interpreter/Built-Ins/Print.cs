using System;
using System.Linq;

namespace Ergo.Lang.BuiltIns
{
    public sealed class Print : BuiltIn
    {
        public Print()
            : base("", new("@print"), Maybe<int>.None)
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] args)
        {
            foreach (var arg in args)
            {
                Console.Write(arg.Explain());
            }
            return new(Literals.True);
        }
    }
}
