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
                "Print something to the console."
                , new Atom("@print"), 0, BuiltIn_Print));
            AddBuiltIn(new BuiltIn(
                "Assigns the right hand side to the left hand side."
                , new Atom("@set"), 2, BuiltIn_Assign));
            AddBuiltIn(new BuiltIn(
                "Is true if its argument cannot be proven true."
                , new Atom("@not"), 1, BuiltIn_Negate));
            AddBuiltIn(new BuiltIn(
                "Is true if its argument is a ground term."
                , new Atom("@ground"), 1, BuiltIn_Ground));
            AddBuiltIn(new BuiltIn(
                "Unifies the left hand side with the right hand side."
                , new Atom("@unify"), 2, BuiltIn_Unify));
            AddBuiltIn(new BuiltIn(
                "Evaluates the right hand side and assigns the result to the left hand side."
                , new Atom("@eval"), 2, BuiltIn_Evaluate));
            AddBuiltIn(new BuiltIn(
                "Builds a complex term with the desired arity where all terms are discarded variables."
                , new Atom("@anon"), 2, BuiltIn_AnonymousComplex));
            AddBuiltIn(new BuiltIn(
                "Grabs the first solution for the previous clause, instead of every solution."
                , new Atom("@cut"), 0, BuiltIn_Cut));
            AddBuiltIn(new BuiltIn(
                "Compares two terms for equality."
                , new Atom("@eq"), 2, BuiltIn_Equals));
        }

        protected Complex ComplexGuard(Term t, Func<Complex, Exception> @throw)
        {
            if (t.Type != TermType.Complex) {
                @throw(default);
            }
            var c = (Complex)t;
            if (@throw(c) is Exception ex) {
                throw ex;
            }
            return c;
        }
        protected Atom AtomGuard(Term t, Func<Atom, Exception> @throw)
        {
            if (t.Type != TermType.Atom) {
                @throw(default);
            }
            var c = (Atom)t;
            if (@throw(c) is Exception ex) {
                throw ex;
            }
            return c;
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Negate(Term t)
        {
            var c = ComplexGuard(t, c => {
                if (c.Arguments.Length != 1) {
                    return new InterpreterException(ErrorType.ExpectedTermWithArity, Term.Explain(c.Functor), 1);
                }
                return null;
            });

            var arg = c.Arguments.Single();
            if (Solve(CommaExpression.Build(arg)).Any()) {
                return new BuiltIn.Evaluation(Literals.False);
            }
            return new BuiltIn.Evaluation(Literals.True);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Equals(Term t)
        {
            var c = ComplexGuard(t, c => {
                if (c.Arguments.Length != 2) {
                    return new InterpreterException(ErrorType.ExpectedTermWithArity, Term.Explain(c.Functor), 2);
                }
                return null;
            });
            if (!c.Arguments[0].Equals(c.Arguments[1])) {
                return new BuiltIn.Evaluation(Literals.False);
            }
            return new BuiltIn.Evaluation(Literals.True);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Cut(Term t)
        {
            return new BuiltIn.Evaluation(Literals.True);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Assign(Term t)
        {
            var c = ComplexGuard(t, c => {
                if (c.Arguments.Length != 2) {
                    return new InterpreterException(ErrorType.ExpectedTermWithArity, Term.Explain(c.Functor), 2);
                }
                if (c.Arguments[0].Type != TermType.Variable) {
                    return new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.FreeVariable, Term.Explain(c.Arguments[0]));
                }
                return null;
            });
            return new BuiltIn.Evaluation(Literals.True, new Substitution(c.Arguments[0], c.Arguments[1]));
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Unify(Term t)
        {
            var c = ComplexGuard(t, c => {
                if (c.Arguments.Length != 2) {
                    return new InterpreterException(ErrorType.ExpectedTermWithArity, Term.Explain(c.Functor), 2);
                }
                return null;
            });
            if (Substitution.TryUnify(new Substitution(c.Arguments[0], c.Arguments[1]), out var subs)) {
                return new BuiltIn.Evaluation(Literals.True, subs.ToArray());
            }
            return new BuiltIn.Evaluation(Literals.False);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Evaluate(Term t)
        {
            var c = ComplexGuard(t, c => {
                if (c.Arguments.Length != 2) {
                    return new InterpreterException(ErrorType.ExpectedTermWithArity, Term.Explain(c.Functor), 2);
                }
                return null;
            });

            var result = new Atom(Eval(c.Arguments[1]));
            if (Substitution.TryUnify(new Substitution(c.Arguments[0], result), out var subs)) {
                return new BuiltIn.Evaluation(Literals.True, subs.ToArray());
            }
            return new BuiltIn.Evaluation(Literals.False);

            static decimal Eval(Term t)
            {
                return t.Reduce(
                    a => a.Value is decimal d ? d : Throw(a),
                    v => Throw(v),
                    c => c.Functor switch {
                          var f when Operators.BinaryPlus.Synonyms.Contains(f) => Eval(c.Arguments[0]) + Eval(c.Arguments[1])
                        , var f when Operators.BinaryMinus.Synonyms.Contains(f) => Eval(c.Arguments[0]) - Eval(c.Arguments[1])
                        , var f when Operators.BinaryAsterisk.Synonyms.Contains(f) => Eval(c.Arguments[0]) * Eval(c.Arguments[1])
                        , var f when Operators.BinaryForwardSlash.Synonyms.Contains(f) => Eval(c.Arguments[0]) / Eval(c.Arguments[1])
                        , _ => Throw(c)
                    }
                );
                static decimal Throw(Term t)
                {
                    throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Number, Term.Explain(t));
                }
            }
        }

        protected virtual BuiltIn.Evaluation BuiltIn_Print(Term t)
        {
            var c = ComplexGuard(t, c => null);
            var args = c.Arguments
                .Select(a => a.Reduce(
                    term => {
                        if (term.Value is string s) return s;
                        return Term.Explain(term);
                    },
                    var => var.Name,
                    complex => Term.Explain(complex)
                ))
                .ToArray();
            foreach (var arg in args) {
                Console.Write(arg);
            }
            return new BuiltIn.Evaluation(Literals.True);
        }

        protected virtual BuiltIn.Evaluation BuiltIn_AnonymousComplex(Term t)
        {
            var c = ComplexGuard(t, c => {
                if (c.Arguments.Length != 2) {
                    return new InterpreterException(ErrorType.ExpectedTermWithArity, Term.Explain(c.Functor), 2);
                }
                if (c.Arguments[0].Type != TermType.Atom) {
                    return new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Functor, Term.Explain(c.Arguments[0]));
                }
                if (c.Arguments[1].Type != TermType.Atom) {
                    return new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Number, Term.Explain(c.Arguments[1]));
                }
                return null;
            });

            var args = (Functor: (Atom)c.Arguments[0], Arity: (Atom)c.Arguments[1]);
            if (!(args.Functor.Value is string functor)) {
                throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Functor, Term.Explain(args.Functor));
            }
            if (!(args.Arity.Value is decimal arity)) {
                throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Number, Term.Explain(args.Arity));
            }
            if (arity - (int)arity != 0) {
                throw new InterpreterException(ErrorType.ExpectedAtomWithDomain, BuiltIn.Domains.Integers);
            }
            var predArgs = Enumerable.Range(0, (int)arity)
                .Select(i => Literals.Discard)
                .ToArray();
            return new BuiltIn.Evaluation(new Complex(args.Functor, predArgs));
        }
        protected virtual BuiltIn.Evaluation BuiltIn_Ground(Term t)
        {
            if(t.Ground) {
                return new BuiltIn.Evaluation(Literals.True);
            }
            return new BuiltIn.Evaluation(Literals.False);
        }
    }
}
