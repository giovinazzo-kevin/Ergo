using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Linq;

namespace Ergo.Lang.BuiltIns
{
    public sealed class Compare : MathBuiltIn
    {
        public Compare()
            : base("", new("@evalcmp"), Maybe<int>.Some(1))
        {
        }

        public override Evaluation Apply(Solver solver, Solver.Scope scope, ITerm[] arguments)
        {
            var c = arguments[0].Reduce(a => Throw<Complex>(a), v => Throw<Complex>(v), c => c);
            return new(new Atom(c.Functor switch {
                    var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGt.Synonyms.Contains(f) => Eval(c.Arguments[0]) > Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGte.Synonyms.Contains(f) => Eval(c.Arguments[0]) >= Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonLt.Synonyms.Contains(f) => Eval(c.Arguments[0]) < Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators. BinaryComparisonLte.Synonyms.Contains(f) => Eval(c.Arguments[0]) <= Eval(c.Arguments[1])
                , _ => Throw<bool>(c)
            }));
            static T Throw<T>(ITerm t)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Number, t.Explain());
            }
        }
    }
}
