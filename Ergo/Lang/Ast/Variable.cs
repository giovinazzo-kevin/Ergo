using System;
using System.Diagnostics;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain(this) }")]
    public readonly struct Variable
    {
        public readonly string Name { get; }
        public readonly bool Ignored { get; }

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
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Variable other)) {
                return false;
            }
            return Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name);
        }

        public static implicit operator Term(Variable rhs)
        {
            return Term.FromVariable(rhs);
        }
    }

}
