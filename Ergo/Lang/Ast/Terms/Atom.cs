using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ergo.Lang.Ast
{

    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Atom : ITerm
    {
        public bool IsGround => true;

        public readonly object Value;
        private readonly int HashCode;

        public Atom(object value)
        {
            Value = value;
            HashCode = value?.GetHashCode() ?? 0;
        }

        public string Explain()
        {
            if (Value is bool b)
            {
                return b ? "⊤" : "⊥";
            }
            else if (Value is string s)
            {
                s = Escape(s);
                // In certain cases, the quotes can be omitted
                if (
                       // If this == Literals.EmptyList
                       s == "[]"
                    // Or if this == Literals.EmptyCommaExpression
                    || s == "()"
                    // Or if this is not a string that can be confused with a variable name
                    || !Char.IsUpper(s.FirstOrDefault())
                        // And if this is a string with no weird punctuation and no spaces
                        && !s.Any(c => IsQuotablePunctuation(c) || Char.IsWhiteSpace(c))
                )
                {
                    return s;
                }
                return $"'{s}'";
            }
            else
            {
                return Value.ToString();
            }
            bool IsQuotablePunctuation(char c) => !Lexemes.IdentifierPunctuation.Contains(c) && Lexemes.QuotablePunctuation.Contains(c);
            string Escape(string s) => s
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        public ITerm Substitute(Substitution s)
        {
            if (Equals(s.Lhs)) return s.Rhs;
            return this;
        }

        public IEnumerable<Variable> Variables => Enumerable.Empty<Variable>();

        public static Atom WithValue(object newValue) => new(newValue);

        public override bool Equals(object obj)
        {
            if(obj is not Atom other) {
                return false;
            }
            if(other.Value is double n && Value is double m) {
                return m - n == 0d;
            }
            return Equals(Value, other.Value);
        }
        public bool Equals(ITerm obj) => Equals((object)obj);


        public override int GetHashCode()
        {
            return HashCode;
        }

        public int CompareTo(ITerm o)
        {
            if (o is Variable) return 1;
            if (o is Complex) return -1;
            if (o is not Atom other) throw new InvalidCastException();

            if (Value is double d && other.Value is double e)
            {
                return d.CompareTo(e);
            }
            if (Value is string s && other.Value is string t)
            {
                return s.CompareTo(t);
            }
            return Explain().CompareTo(other.Explain());
        }

        public ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
        {
            return this;
        }

        public ITerm Qualify(Atom m)
        {
            return new Atom($"{m.Explain()}:{Explain()}");
        }

        public static bool operator ==(Atom left, Atom right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Atom left, Atom right)
        {
            return !(left == right);
        }
    }

}
