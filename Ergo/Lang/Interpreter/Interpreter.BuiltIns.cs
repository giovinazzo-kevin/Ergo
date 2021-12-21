using System;
using System.Linq;

namespace Ergo.Lang
{
    //public partial class Interpreter
    //{

    //    static bool Cmp(ITerm t)
    //    {
    //    }

    //    protected virtual BuiltIn.Evaluation BuiltIn_AnonymousComplex(ITerm t, Atom module)
    //    {
    //        if(t.Matches(out var match, shape: new { Functor = default(string), Arity = default(int) }))
    //        {
    //            if (match.Arity == 0) { return new(new Atom(match.Functor)); }
    //            var predArgs = Enumerable.Range(0, match.Arity)
    //                .Select(i => (ITerm)new Variable($"{i}"))
    //                .ToArray();
    //            return new(new Complex(new(match.Functor), predArgs));
    //        }
    //        return new(Literals.False);
    //    }
    //    protected virtual BuiltIn.Evaluation BuiltIn_Ground(ITerm t, Atom module)
    //    {
    //        if (t.IsGround) {
    //            return new(Literals.True);
    //        }
    //        return new(Literals.False);
    //    }
    //}
}
