using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{

    public abstract class MathBuiltIn : BuiltIn
    {
        protected MathBuiltIn(string documentation, Atom functor, Maybe<int> arity) 
            : base(documentation, functor, arity, Modules.Math)
        {
        }
        public dynamic Evaluate(ITerm t, InterpreterScope s)
        {
            if (t is Atom a) { return a.Value is double d ? d : Throw(a); }
            if(t is not Complex c) { Throw(t); }
            return c.Functor switch
            {
                var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGt.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) > Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGte.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) >= Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonLt.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) < Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators. BinaryComparisonLte.Synonyms.Contains(f)
                => Evaluate(c.Arguments[0], s) <= Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 1 && f.Equals(Signature.Functor)
                => Evaluate(c.Arguments[0], s)
                , var f when c.Arguments.Length == 2 && Operators.BinaryMod.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) % Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators.BinarySum.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) + Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators.BinarySubtraction.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) - Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators.BinaryMultiplication.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) * Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators.BinaryDivision.Synonyms.Contains(f) 
                => Evaluate(c.Arguments[0], s) / Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && Operators.BinaryIntegerDivision.Synonyms.Contains(f) 
                => (int)(Evaluate(c.Arguments[0], s) / Evaluate(c.Arguments[1], s))
                , var f when c.Arguments.Length == 2 && Operators.BinaryPower.Synonyms.Contains(f) 
                => Math.Pow(Evaluate(c.Arguments[0], s), Evaluate(c.Arguments[1], s))
                , var f when c.Arguments.Length == 1 && Operators.UnaryNegative.Synonyms.Contains(f) 
                => -Evaluate(c.Arguments[0], s)
                , var f when c.Arguments.Length == 1 && Operators.UnaryPositive.Synonyms.Contains(f) 
                => +Evaluate(c.Arguments[0], s)
                , _ => Throw(c)
            };
            double Throw(ITerm t)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, s, Types.Number, t.Explain());
            }
        }
    }
}
