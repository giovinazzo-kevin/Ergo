using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class AnonymousComplex : BuiltIn
    {
        // TODO: Remove once Reflection module is up and running
        public AnonymousComplex()
            : base("", new("anon"), Maybe<int>.Some(2), Modules.Reflection)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if (!args[1].Matches<int>(out var arity))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Number, args[1].Explain());
            }
            if (args[0] is not Atom functor)
            {
                if (args[0].TryGetQualification(out var qm, out var qs) && qs is Atom functor_)
                {
                    var cplx = (ITerm)functor_.BuildAnonymousComplex(arity);
                    if (cplx.TryQualify(qm, out var qualified))
                    {
                        yield return new(qualified);
                        yield break;
                    }
                }
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Functor, args[0].Explain());
            }
            yield return new(functor.BuildAnonymousComplex(arity));
        }
    }
}
