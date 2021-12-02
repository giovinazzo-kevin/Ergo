using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct Atom : IComparable<Term>, IComparable<Complex>, IComparable<Variable>, IComparable<Atom>
    {
        public static readonly char[] IdentifierPunctuation = new char[] {
            '@', '_', '(', ')', '[', ']'
        };
        public static readonly char[] QuotablePunctuation = new char[] {
            ','
        };

        public readonly object Value { get; }

        public static string Explain(Atom a)
        {
            if (a.Value is bool) {
                // Boolean literals are lowercase
                return a.Value.ToString().ToLower();
            }
            else if (a.Value is string s) {
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
                ) {
                    return s;
                }
                return $"'{s}'";
            }
            else {
                return a.Value.ToString();
            }
            bool IsQuotablePunctuation(char c) => !IdentifierPunctuation.Contains(c) && QuotablePunctuation.Contains(c);
            string Escape(string s) => s
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        public Atom(object value)
        {
            Value = value;
        }

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

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public int CompareTo(Term other)
        {
            return other.Type switch
            {
                TermType.Atom => this.CompareTo((Atom)other)
                , TermType.Variable => this.CompareTo((Variable)other)
                , TermType.Complex => this.CompareTo((Complex)other)
                , _ => throw new InvalidOperationException(other.Type.ToString())
            };
        }
        public int CompareTo(Atom other)
        {
            if(Value is double d && other.Value is double e) {
                return d.CompareTo(e);
            }
            if(Value is string s && other.Value is string t) {
                return s.CompareTo(t);
            }
            return Explain(this).CompareTo(Explain(other));
        }
        public int CompareTo(Variable other) => 1;
        public int CompareTo(Complex other) => -1;

        public static implicit operator Term(Atom rhs)
        {
            return Term.FromAtom(rhs);
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
