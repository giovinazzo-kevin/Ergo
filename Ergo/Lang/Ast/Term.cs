﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Ergo.Lang
{
    public readonly partial struct Term
    {
        public class InstantiationContext
        {
            private volatile int _GlobalVarCounter;
            public int GetFreeVariableId() => Interlocked.Increment(ref _GlobalVarCounter);
        }

        public readonly TermType Type { get; }
        private readonly Atom AtomValue;
        private readonly Variable VariableValue;
        private readonly Complex ComplexValue;

        public readonly bool Ground;

        public static string Explain(Term t)
        {
            return t.Type switch
            {
                TermType.Atom => Atom.Explain(t.AtomValue)
                , TermType.Variable => Variable.Explain(t.VariableValue)
                , TermType.Complex => Complex.Explain(t.ComplexValue)
                , _ => throw new InvalidOperationException(t.Type.ToString())
            };
        }
        
        public Term Map(Func<Atom, Term> atom, Func<Variable, Term> variable, Func<Complex, Term> complex)
        {
            return Type switch
            {
                TermType.Atom => atom(AtomValue)
                , TermType.Variable => variable(VariableValue)
                , TermType.Complex => complex(ComplexValue)
                , _ => throw new InvalidOperationException(Type.ToString())
            };
        }
        public T Reduce<T>(Func<Atom, T> atom, Func<Variable, T> variable, Func<Complex, T> complex)
        {
            return Type switch
            {
                TermType.Atom => atom(AtomValue)
                , TermType.Variable => variable(VariableValue)
                , TermType.Complex => complex(ComplexValue)
                , _ => throw new InvalidOperationException(Type.ToString())
            };
        }

        public static Term Substitute(Term @base, Substitution s)
        {
            switch (@base.Type) {
                case TermType.Atom:
                case TermType.Variable:
                    if (@base.Equals(s.Lhs)) {
                        return s.Rhs;
                    }
                    return @base;
                case TermType.Complex:
                    if (@base.Equals(s.Lhs)) {
                        return s.Rhs;
                    }
                    var c = (Complex)@base;
                    var newArgs = new Term[c.Arguments.Length];
                    for (int i = 0; i < newArgs.Length; i++) {
                        newArgs[i] = Substitute(c.Arguments[i], s);
                    }
                    return c.WithArguments(newArgs);
                default: throw new InvalidOperationException();
            }
        }

        public static Term Substitute(Term @base, IEnumerable<Substitution> subs)
        {
            foreach (var s in subs) {
                @base = Substitute(@base, s);
            }
            return @base;
        }

        private Term(Atom atom, Variable variable, Complex complex, TermType kind)
        {
            AtomValue = atom;
            VariableValue = variable;
            ComplexValue = complex;
            Type = kind;
            Ground = kind == TermType.Atom || kind == TermType.Complex && ComplexValue.Arguments.All(a => a.Ground);
        }

        public static Term FromAtom(Atom atom)
        {
            return new Term(atom, default, default, TermType.Atom);
        }
        public static Term FromVariable(Variable variable)
        {
            return new Term(default, variable, default, TermType.Variable);
        }
        public static Term FromComplex(Complex complex)
        {
            return new Term(default, default, complex, TermType.Complex);
        }

        public static Term Instantiate(InstantiationContext ctx, Term t, bool discardsOnly = false, Dictionary<string, Variable> vars = null)
        {
            vars ??= new Dictionary<string, Variable>();
            var ret = t.Map(
                atom => atom
                , InstantiateVariable
                , complex => new Complex(complex.Functor, complex.Arguments.Select(a => Instantiate(ctx, a, discardsOnly, vars)).ToArray())
            );
            return ret;

            Term InstantiateVariable(Variable v)
            {
                if(vars.TryGetValue(v.Name, out var inst)) {
                    return inst;
                }
                if(v.Equals(Literals.Discard) || !discardsOnly) {
                    return vars[v.Name] = new Variable($"__G{ctx.GetFreeVariableId()}");
                }
                return v;
            }
        }

        public static Variable[] Variables(Term t)
        {
            return t.Type switch
            {
                TermType.Variable => new[] { t.VariableValue }
                , TermType.Complex => t.ComplexValue.Arguments.SelectMany(arg => Variables(arg)).ToArray()
                , _ => Array.Empty<Variable>()
            };
        }

        public override bool Equals(object other)
        {
            if (!(other is Term term)) {
                if (other is Atom a) term = a;
                else if (other is Variable v) term = v;
                else if (other is Complex c) term = c;
                else return false;
            }
            return (Type, term.Type) switch
            {
                (TermType.Atom, TermType.Atom) => Equals(AtomValue, term.AtomValue)
                , (TermType.Variable, TermType.Variable) => Equals(VariableValue, term.VariableValue)
                , (TermType.Complex, TermType.Complex) => Equals(ComplexValue, term.ComplexValue)
                , _ => false
            };
        }

        public override int GetHashCode()
        {
            return Type switch
            {
                TermType.Atom => AtomValue.GetHashCode()
                , TermType.Variable => VariableValue.GetHashCode()
                , TermType.Complex => ComplexValue.GetHashCode()
                , _ => throw new InvalidOperationException(Type.ToString())
            };
        }

        public static explicit operator Atom(Term rhs)
        {
            if (rhs.Type == TermType.Atom) {
                return rhs.AtomValue;
            }
            throw new InvalidCastException(rhs.Type.ToString());
        }

        public static explicit operator Variable(Term rhs)
        {
            if (rhs.Type == TermType.Variable) {
                return rhs.VariableValue;
            }
            throw new InvalidCastException(rhs.Type.ToString());
        }

        public static explicit operator Complex(Term rhs)
        {
            if (rhs.Type == TermType.Complex) {
                return rhs.ComplexValue;
            }
            throw new InvalidCastException(rhs.Type.ToString());
        }

        public override string ToString()
        {
            return Explain(this);
        }
    }
}