﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct Atom
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

        public Atom WithValue(object newValue) => new Atom(newValue);

        public override bool Equals(object obj)
        {
            if(!(obj is Atom other)) {
                return false;
            }
            if(other.Value is decimal n && Value is decimal m) {
                return m - n == 0M;
            }
            return Equals(Value, other.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static implicit operator Term(Atom rhs)
        {
            return Term.FromAtom(rhs);
        }
    }

}
