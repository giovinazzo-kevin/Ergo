using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct Atom : ITerm
{
    public bool IsGround => true;
    public bool IsQualified => false;
    public bool IsParenthesized => false;

    public readonly object Value;
    private readonly int HashCode;
    public readonly bool IsQuoted;
    public readonly Maybe<IAbstractTerm> AbstractForm { get; }

    public Atom(object value, Maybe<bool> quoted = default, Maybe<IAbstractTerm> abs = default)
    {
        Value = value;
        if (Value?.IsNumericType() ?? false)
            Value = Convert.ToDecimal(value);
        HashCode = value?.GetHashCode() ?? 0;
        IsQuoted = quoted.Reduce(some => some, () => value is string s
            && s != (string)WellKnown.Literals.EmptyList.Value
            && s != (string)WellKnown.Literals.EmptyCommaList.Value
            && (
                // And if this is not a string that can be confused with a variable name
                char.IsUpper(s.FirstOrDefault())
                // Or if this is a string that contains spaces or weird punctuation
                || s.Any(c => char.IsWhiteSpace(c)
                    || !WellKnown.Lexemes.IdentifierPunctuation.Contains(c) && WellKnown.Lexemes.QuotablePunctuation.Contains(c))
            ));
        AbstractForm = abs;
    }

    public string Explain(bool canonical = false)
    {
        if (!canonical && AbstractForm.HasValue)
            return AbstractForm.GetOrThrow().Explain();
        if (Value is bool b)
        {
            return b ? "⊤" : "⊥";
        }
        else if (Value is string s)
        {
            // In certain cases, the quotes can be omitted
            if (!IsQuoted)
            {
                return s;
            }

            return $"'{Escape(s)}'";
        }
        else
        {
            return Value.ToString();
        }

        static string Escape(string s) => s
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
    public Atom AsQuoted(bool quoted) => new(Value, Maybe.Some(quoted));
    public Atom WithAbstractForm(Maybe<IAbstractTerm> abs) => new(Value, Maybe.Some(IsQuoted), abs);

    public override bool Equals(object obj)
    {
        if (obj is not Atom other)
        {
            return false;
        }

        if (other.Value is double n && Value is double m)
        {
            return m - n == 0d;
        }

        return Equals(Value, other.Value);
    }
    public bool Equals(ITerm obj) => Equals((object)obj);

    public override int GetHashCode() => HashCode;

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

    public ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) => this;

    public static bool operator ==(Atom left, Atom right) => left.Equals(right);

    public static bool operator !=(Atom left, Atom right) => !(left == right);
}

