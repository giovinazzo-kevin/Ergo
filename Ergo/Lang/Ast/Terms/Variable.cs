using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct Variable : ITerm
{
    public bool IsGround => false;
    public bool IsQualified => false;
    public bool IsParenthesized => false;

    public readonly string Name;
    public readonly bool Ignored;

    private readonly int HashCode;

    public readonly Maybe<IAbstractTerm> AbstractForm { get; }

    public Variable(string name, Maybe<IAbstractTerm> abs = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name[0] != char.ToUpper(name[0]))
        {
            throw new InvalidOperationException("Variables must have a name that starts with an uppercase letter.");
        }

        Name = name;
        Ignored = name.StartsWith('_');
        HashCode = Name.GetHashCode();
        AbstractForm = abs;
    }

    public Variable WithAbstractForm(Maybe<IAbstractTerm> abs) => new(Name, abs);

    public string Explain(bool canonical = false)
    {
        if (!canonical && AbstractForm.HasValue)
            return AbstractForm.GetOrThrow().Explain();
        return Name;
    }

    public ITerm Substitute(Substitution s)
    {
        if (Equals(s.Lhs)) return s.Rhs;
        return this;
    }

    public IEnumerable<Variable> Variables => Enumerable.Empty<Variable>().Append(this);

    public ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        if (vars.TryGetValue(Name, out var inst))
        {
            return inst;
        }

        return vars[Name] = new Variable($"__{ctx.VarPrefix}{ctx.GetFreeVariableId()}");
    }

    //public ITerm Qualify(Atom m)
    //{
    //    return new Variable($"{m.Explain()}:{Explain()}");
    //}

    public override bool Equals(object obj)
    {
        if (obj is not Variable other)
        {
            return false;
        }

        return Equals(Name, other.Name);
    }
    public bool Equals(ITerm obj) => Equals((object)obj);

    public override int GetHashCode() => HashCode;

    public int CompareTo(ITerm o)
    {
        if (o is Atom) return -1;
        if (o is Complex) return -1;
        if (o is not Variable other) throw new InvalidCastException();

        return Name.CompareTo(other.Name);
    }

    public static bool operator ==(Variable left, Variable right) => left.Equals(right);

    public static bool operator !=(Variable left, Variable right) => !(left == right);
}

