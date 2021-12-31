using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class Compare : MathBuiltIn
    {
        public Compare()
            : base("", new("@evalcmp"), Maybe<int>.Some(1))
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            var c = arguments[0].Reduce(a => Throw<Complex>(a), v => Throw<Complex>(v), c => c);
            yield return new(new Atom(c.Functor switch {
                    var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGt.Synonyms.Contains(f) => Eval(c.Arguments[0], solver.InterpreterScope) > Eval(c.Arguments[1], solver.InterpreterScope)
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGte.Synonyms.Contains(f) => Eval(c.Arguments[0], solver.InterpreterScope) >= Eval(c.Arguments[1], solver.InterpreterScope)
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonLt.Synonyms.Contains(f) => Eval(c.Arguments[0], solver.InterpreterScope) < Eval(c.Arguments[1], solver.InterpreterScope)
                , var f when c.Arguments.Length == 2 && Operators. BinaryComparisonLte.Synonyms.Contains(f) => Eval(c.Arguments[0], solver.InterpreterScope) <= Eval(c.Arguments[1], solver.InterpreterScope)
                , _ => Throw<bool>(c)
            }));
            T Throw<T>(ITerm t)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.Number, t.Explain());
            }
        }
    }
}
