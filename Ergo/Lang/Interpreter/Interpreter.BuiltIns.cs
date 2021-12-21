using System;
using System.Linq;

namespace Ergo.Lang
{
    public partial class Interpreter
    {
        protected void AddBuiltIn(BuiltIn b)
        {
            BuiltInsDict[b.Signature] = b;
        }

        protected void AddVariadicBuiltIn(BuiltIn b)
        {
            const int MAX_ARGS = 8;
            for (int i = 0; i < MAX_ARGS; i++) {
                AddBuiltIn(b.WithArity(i));
            }
        }

        protected virtual void AddBuiltins()
        {
            AddVariadicBuiltIn(new BuiltIn(
                "Prints something to the console."
                , new Atom("@print"), 0, BuiltIn_Print));
            AddBuiltIn(new BuiltIn(
                "Grabs the first solution for the previous clause, instead of every solution."
                , new Atom("@cut"), 0, BuiltIn_Cut));
            AddBuiltIn(new BuiltIn(
                "Is true if its argument cannot be proven true."
                , new Atom("@unprovable"), 1, BuiltIn_Unprovable));
            AddBuiltIn(new BuiltIn(
                "Boolean negation."
                , new Atom("@not"), 1, BuiltIn_Not));
            AddBuiltIn(new BuiltIn(
                "Is true if its argument is a ground ITerm."
                , new Atom("@ground"), 1, BuiltIn_Ground));
            AddBuiltIn(new BuiltIn(
                "Evaluates to the result of its argument, a comparison."
                , new Atom("@evalcmp"), 1, BuiltIn_Cmp1));
            AddBuiltIn(new BuiltIn(
                "Evaluates to the result of its argument, a mathematical expression."
                , new Atom("@eval"), 1, BuiltIn_Eval1));
            AddBuiltIn(new BuiltIn(
                "Builds a complex ITerm with the desired arity where all ITerms are discarded variables."
                , new Atom("@anon"), 2, BuiltIn_AnonymousComplex));
            AddBuiltIn(new BuiltIn(
                "Assigns the right hand side to the left hand side."
                , new Atom("@set"), 2, BuiltIn_Assign));
            AddBuiltIn(new BuiltIn(
                "Evaluates the rhs, a mathematical expression, and substitutes the lhs with the result."
                , new Atom("@eval"), 2, BuiltIn_Eval2));
            AddBuiltIn(new BuiltIn(
                "Unifies the left hand side with the right hand side."
                , new Atom("@unify"), 2, BuiltIn_Unify));
            AddBuiltIn(new BuiltIn(
                "Produces the list of equations necessary to unify the left hand side with the right hand side."
                , new Atom("@unifiable"), 3, BuiltIn_Unifiable));
            AddBuiltIn(new BuiltIn(
                "Compares two ITerms according to the standard order of ITerms."
                , new Atom("@compare"), 3, BuiltIn_Compare));
        }

        protected static Complex ComplexGuard(ITerm t, Func<Complex, Exception> @throw)
        {
            if (t is not Complex c) {
                @throw(default);
                return default;
            }
            if (@throw(c) is Exception ex) {
                throw ex;
            }
            return c;
        }
        protected static Atom AtomGuard(ITerm t, Func<Atom, Exception> @throw)
        {
            if (t is not Atom c) {
                @throw(default);
                return default;
            }
            if (@throw(c) is Exception ex) {
                throw ex;
            }
            return c;
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Unprovable(ITerm t, Atom module)
        {
            var c = (Complex)t;
            var arg = c.Arguments.Single();
            if (Solve(new Query(new(arg)), Maybe.Some(module)).Any()) {
                return new(Literals.False);
            }
            return new(Literals.True);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Not(ITerm t, Atom module)
        {
            var c = ComplexGuard(t, c => {
                if (c.Arguments[0] is not Atom) {
                    return new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Boolean, c.Arguments[0].Explain());
                }
                return null;
            });

            var arg = c.Arguments.Single();
            if (((Atom)arg).Value is not bool eval) {
                throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Boolean, arg.Explain());
            }
            return new(new Atom(!eval));
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Cut(ITerm t, Atom module)
        {
            return new(Literals.True);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Assign(ITerm t, Atom module)
        {
            var c = (Complex)t;
            return new(Literals.True, new Substitution(c.Arguments[0], c.Arguments[1]));
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Unify(ITerm t, Atom module)
        {
            var c = (Complex)t;
            if (new Substitution(c.Arguments[0], c.Arguments[1]).TryUnify(out var subs))
            {
                return new(Literals.True, subs.ToArray());
            }
            return new(Literals.False);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Unifiable(ITerm t, Atom module)
        {
            var c = (Complex)t;
            if (new Substitution(c.Arguments[0], c.Arguments[1]).TryUnify(out var subs)) {
                var equations = subs.Select(s => (ITerm)new Complex(Operators.BinaryUnification.CanonicalFunctor, s.Lhs, s.Rhs));
                var list = new List(equations.ToArray());
                if (new Substitution(c.Arguments[2], list.Root).TryUnify(out subs)) {
                    return new(Literals.True, subs.ToArray());
                }
            }
            return new(Literals.False);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Compare(ITerm t, Atom module)
        {
            var c = (Complex)t;
            var cmp = (double)c.Arguments[1].CompareTo(c.Arguments[2]);
            if(c.Arguments[0].IsGround) {
                var a = AtomGuard(c.Arguments[0], a => {
                    if(a.Value is not double d) {
                        return new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Number, a.Explain());
                    }
                    if(d - (int)d != 0) {
                        return new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Integer, a.Explain());
                    }
                    return null;
                });
                if(a.Value.Equals(cmp)) {
                    return new(Literals.True);
                }
                return new(Literals.False);
            }
            return new(Literals.True, new Substitution(c.Arguments[0], new Atom(cmp)));
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Eval1(ITerm t, Atom module)
        {
            var result = new Atom(Eval(((Complex)t).Arguments[0]));
            return new(result);
        }
        protected virtual BuiltIn.Evaluation BuiltIn_Cmp1(ITerm t, Atom module)
        {
            var result = new Atom(Cmp(((Complex)t).Arguments[0]));
            return new(result);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Eval2(ITerm t, Atom module)
        {
            var result = new Atom(Eval(((Complex)t).Arguments[1]));
            if (new Substitution(((Complex)t).Arguments[0], result).TryUnify(out var subs)) {
                return new(Literals.True, subs.ToArray());
            }
            return new(Literals.False);
        }

        static double Eval(ITerm t)
        {
            if(t is Atom a) { return a.Value is double d ? d : Throw(a); }
            if(t is not Complex c) { Throw(t); }
            return c.Functor switch {
                    var f when c.Arguments.Length == 2 && Operators.BinaryMod.Synonyms.Contains(f) => Eval(c.Arguments[0]) % Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinarySum.Synonyms.Contains(f) => Eval(c.Arguments[0]) + Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinarySubtraction.Synonyms.Contains(f) => Eval(c.Arguments[0]) - Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryMultiplication.Synonyms.Contains(f) => Eval(c.Arguments[0]) * Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryDivision.Synonyms.Contains(f) => Eval(c.Arguments[0]) / Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryPower.Synonyms.Contains(f) => Math.Pow(Eval(c.Arguments[0]), Eval(c.Arguments[1]))
                , var f when c.Arguments.Length == 1 && Operators.UnaryNegative.Synonyms.Contains(f) => -Eval(c.Arguments[0])
                , var f when c.Arguments.Length == 1 && Operators.UnaryPositive.Synonyms.Contains(f) => +Eval(c.Arguments[0])
                , _ => Throw(c)
            };
            static double Throw(ITerm t)
            {
                throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Number, t.Explain());
            }
        }

        static bool Cmp(ITerm t)
        {
            if (t is not Complex c) { Throw(t); }
            return c.Functor switch {
                    var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGt.Synonyms.Contains(f) => Eval(c.Arguments[0]) > Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonGte.Synonyms.Contains(f) => Eval(c.Arguments[0]) >= Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators.BinaryComparisonLt.Synonyms.Contains(f) => Eval(c.Arguments[0]) < Eval(c.Arguments[1])
                , var f when c.Arguments.Length == 2 && Operators. BinaryComparisonLte.Synonyms.Contains(f) => Eval(c.Arguments[0]) <= Eval(c.Arguments[1])
                , _ => Throw(c)
            };
            static bool Throw(ITerm t)
            {
                throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Number, t.Explain());
            }
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Print(ITerm t, Atom module)
        {
            var args = ((Complex)t).Arguments
                .Select(a => a.Explain())
                .ToArray();
            foreach (var arg in args) {
                Console.Write(arg);
            }
            return new(Literals.True);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_AnonymousComplex(ITerm t, Atom module)
        {
            if(t.Matches(out var match, new { Functor = default(string), Arity = default(int) }))
            {
                if (match.Arity == 0) { return new(new Atom(match.Functor)); }
                var predArgs = Enumerable.Range(0, match.Arity)
                    .Select(i => (ITerm)new Variable($"{i}"))
                    .ToArray();
                return new(new Complex(new(match.Functor), predArgs));
            }
            return new(Literals.False);
        }
        protected virtual BuiltIn.Evaluation BuiltIn_Ground(ITerm t, Atom module)
        {
            if (t.IsGround) {
                return new(Literals.True);
            }
            return new(Literals.False);
        }
    }
}
