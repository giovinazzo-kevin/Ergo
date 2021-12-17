using System;
using System.Diagnostics;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct Variable : IComparable<Term>
    {
        public readonly string Name;
        public readonly bool Ignored;

        private readonly int HashCode;

        public static string Explain(Variable v)
        {
            return v.Name;
        }


        public Variable(string name)
        {
            if(String.IsNullOrWhiteSpace(name) || name[0] != Char.ToUpper(name[0])) {
                throw new InvalidOperationException("Variables must have a name that starts with an uppercase letter.");
            }
            Name = name;
            Ignored = name.StartsWith('_');
            HashCode = Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj is not Variable other) {
                return false;
            }
            return Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public int CompareTo(Term other)
        {
            return other.Type switch {
                TermType.Atom => this.CompareTo((Atom)other)
                , TermType.Variable => this.CompareTo((Variable)other)
                , TermType.Complex => this.CompareTo((Complex)other)
                , _ => throw new InvalidOperationException(other.Type.ToString())
            };
        }
        public int CompareTo(Atom _) => -1;
        public int CompareTo(Variable other) => Name.CompareTo(other.Name);
        public int CompareTo(Complex _) => -1;

        public static implicit operator Term(Variable rhs)
        {
            return Term.FromVariable(rhs);
        }

        public static bool operator ==(Variable left, Variable right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Variable left, Variable right)
        {
            return !(left == right);
        }
    }

}
