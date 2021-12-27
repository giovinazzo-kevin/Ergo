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
            : base(documentation, functor, arity)
        {
        }
        protected double Eval(ErgoSolver solver, SolverScope scope, ITerm t)
        {
            if (t is Atom a) { return a.Value is double d ? d : Throw(a); }
            if(t is not Complex c) { Throw(t); }
            return c.Functor switch
            {
                var f when c.Arguments.Length == 1 && f.Equals(Signature.Functor)
                => Eval(solver, scope, c.Arguments[0]),
                var f when c.Arguments.Length == 2 && Operators.BinaryMod.Synonyms.Contains(f) 
                => Eval(solver, scope, c.Arguments[0]) % Eval(solver, scope, c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinarySum.Synonyms.Contains(f) 
                => Eval(solver, scope, c.Arguments[0]) + Eval(solver, scope, c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinarySubtraction.Synonyms.Contains(f) 
                => Eval(solver, scope, c.Arguments[0]) - Eval(solver, scope, c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryMultiplication.Synonyms.Contains(f) 
                => Eval(solver, scope, c.Arguments[0]) * Eval(solver, scope, c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryDivision.Synonyms.Contains(f) 
                => Eval(solver, scope, c.Arguments[0]) / Eval(solver, scope, c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryIntegerDivision.Synonyms.Contains(f) 
                => (int)(Eval(solver, scope, c.Arguments[0]) / Eval(solver, scope, c.Arguments[1]))
                , var f when c.Arguments.Length == 2 && Operators.BinaryPower.Synonyms.Contains(f) 
                => Math.Pow(Eval(solver, scope, c.Arguments[0]), Eval(solver, scope, c.Arguments[1]))
                , var f when c.Arguments.Length == 1 && Operators.UnaryNegative.Synonyms.Contains(f) 
                => -Eval(solver, scope, c.Arguments[0])
                , var f when c.Arguments.Length == 1 && Operators.UnaryPositive.Synonyms.Contains(f) 
                => +Eval(solver, scope, c.Arguments[0])
                , _ => Throw(c)
            };
            static double Throw(ITerm t)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Number, t.Explain());
            }
        }
    }
}
