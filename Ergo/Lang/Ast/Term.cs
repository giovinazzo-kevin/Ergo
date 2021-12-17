using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Ergo.Lang
{

    [DebuggerDisplay("{ Explain(this) }")]
    public readonly partial struct Term : IComparable<Term>
    {
        public class InstantiationContext
        {
            public readonly string VarPrefix;
            private volatile int _GlobalVarCounter;
            public InstantiationContext(string prefix) => VarPrefix = prefix;
            public int GetFreeVariableId() => Interlocked.Increment(ref _GlobalVarCounter);
        }

        public readonly TermType Type;
        private readonly Atom AtomValue;
        private readonly Variable VariableValue;
        private readonly Complex ComplexValue;

        public readonly bool IsGround;
        private readonly int HashCode;

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

        public static Term Substitute(string parse, IEnumerable<Substitution> s, out Term parsed)
        {
            parsed = new Parsed<Term>(parse, new(), str => throw new InterpreterException(Interpreter.ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Functor, parse))
                .Value.Reduce(some => some, () => default);
            return Substitute(parsed, s);
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
            var steps = subs.ToDictionary(s => s.Lhs);
            var variables = Variables(@base).Where(var => steps.ContainsKey(var));
            while (variables.Any()) {
                foreach (var var in variables) {
                    @base = Substitute(@base, steps[var]);
                }
                variables = Variables(@base).Where(var => steps.ContainsKey(var));
            }
            return @base;
        }

        public static bool TryUnify(Term a, Term b, out IEnumerable<Substitution> subs)
        {
            return Substitution.TryUnify(new(a, b), out subs);
        }

        public static bool TryUnify(Term a, string parse, out Term parsed, out IEnumerable<Substitution> subs)
        {
            parsed = new Parsed<Term>(parse, new(), str => throw new InterpreterException(Interpreter.ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.Functor, parse))
                .Value.Reduce(some => some, () => default);
            return Substitution.TryUnify(new(a, parsed), out subs);
        }

        private Term(Atom atom, Variable variable, Complex complex, TermType kind)
        {
            AtomValue = atom;
            VariableValue = variable;
            ComplexValue = complex;
            Type = kind;
            IsGround = kind == TermType.Atom || kind == TermType.Complex && ComplexValue.Arguments.All(a => a.IsGround);
            HashCode = Type switch
            {
                TermType.Atom => AtomValue.GetHashCode()
                , TermType.Variable => VariableValue.GetHashCode()
                , TermType.Complex => ComplexValue.GetHashCode()
                , _ => throw new InvalidOperationException(Type.ToString())
            };
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

        public static Term Instantiate(InstantiationContext ctx, Term t, Dictionary<string, Variable> vars = null)
        {
            vars ??= new Dictionary<string, Variable>();
            var ret = t.Map(
                atom => atom
                , InstantiateVariable
                , complex => new Complex(complex.Functor, complex.Arguments.Select(a => Instantiate(ctx, a, vars)).ToArray())
            );
            return ret;

            Term InstantiateVariable(Variable v)
            {
                if(vars.TryGetValue(v.Name, out var inst)) {
                    return inst;
                }
                return vars[v.Name] = new Variable($"__{ctx.VarPrefix}{ctx.GetFreeVariableId()}");
            }
        }

        public static IEnumerable<Variable> Variables(Term t)
        {
            return t.Type switch
            {
                TermType.Variable => new[] { t.VariableValue }
                , TermType.Complex => t.ComplexValue.Arguments.SelectMany(arg => Variables(arg))
                , _ => Array.Empty<Variable>()
            };
        }

        public override bool Equals(object other)
        {
            if (other is not Term term) {
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
            return HashCode;
        }

        public int CompareTo(Term other) 
        {
            return Type switch
            {
                TermType.Atom => AtomValue.CompareTo(other)
                , TermType.Variable => VariableValue.CompareTo(other)
                , TermType.Complex => ComplexValue.CompareTo(other)
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

        public static bool operator ==(Term left, Term right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Term left, Term right)
        {
            return !(left == right);
        }
    }
}