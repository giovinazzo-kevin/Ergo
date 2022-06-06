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
                var f when c.Arguments.Length == 1 && f.Equals(Signature.Functor)
                => Evaluate(c.Arguments[0], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Gt.Contains(f) 
                => Evaluate(c.Arguments[0], s) > Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Gte.Contains(f) 
                => Evaluate(c.Arguments[0], s) >= Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Lt.Contains(f) 
                => Evaluate(c.Arguments[0], s) < Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Lte.Contains(f)
                => Evaluate(c.Arguments[0], s) <= Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Modulo.Contains(f) 
                => Evaluate(c.Arguments[0], s) % Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Addition.Contains(f) 
                => Evaluate(c.Arguments[0], s) + Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Subtraction.Contains(f) 
                => Evaluate(c.Arguments[0], s) - Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Multiplication.Contains(f) 
                => Evaluate(c.Arguments[0], s) * Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Division.Contains(f) 
                => Evaluate(c.Arguments[0], s) / Evaluate(c.Arguments[1], s)
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.IntDivision.Contains(f) 
                => (int)(Evaluate(c.Arguments[0], s) / Evaluate(c.Arguments[1], s))
                , var f when c.Arguments.Length == 2 && WellKnown.Functors.Power.Contains(f) 
                => Math.Pow(Evaluate(c.Arguments[0], s), Evaluate(c.Arguments[1], s))
                , var f when c.Arguments.Length == 1 && WellKnown.Functors.SquareRoot.Contains(f) 
                => Math.Sqrt(Evaluate(c.Arguments[0], s))
                , var f when c.Arguments.Length == 1 && WellKnown.Functors.Minus.Contains(f) 
                => -Evaluate(c.Arguments[0], s)
                , var f when c.Arguments.Length == 1 && WellKnown.Functors.Plus.Contains(f) 
                => +Evaluate(c.Arguments[0], s)
                , var f when c.Arguments.Length == 1 && f.Equals(new Atom("sin"))
                => Math.Sin(Evaluate(c.Arguments[0], s))
                , var f when c.Arguments.Length == 1 && f.Equals(new Atom("cos"))
                => Math.Cos(Evaluate(c.Arguments[0], s))
                , var f when c.Arguments.Length == 1 && f.Equals(new Atom("tan"))
                => Math.Tan(Evaluate(c.Arguments[0], s))
                , var f when c.Arguments.Length == 1 && f.Equals(new Atom("sinh"))
                => Math.Sinh(Evaluate(c.Arguments[0], s))
                , var f when c.Arguments.Length == 1 && f.Equals(new Atom("cosh"))
                => Math.Cosh(Evaluate(c.Arguments[0], s))
                , var f when c.Arguments.Length == 1 && f.Equals(new Atom("tanh"))
                => Math.Tanh(Evaluate(c.Arguments[0], s))
                , _ => Throw(c)
            };
            double Throw(ITerm t)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, s, Types.Number, t.Explain());
            }
        }
    }
}
