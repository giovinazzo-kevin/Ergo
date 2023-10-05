using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly partial struct Operator
{
    public readonly Atom CanonicalFunctor;
    public readonly HashSet<Atom> Synonyms;
    public readonly Atom DeclaringModule;
    public readonly int Precedence;
    public readonly Fixity Fixity;
    public readonly OperatorAssociativity Associativity;

    public Operator(Atom module, Fixity affix, OperatorAssociativity assoc, int precedence, HashSet<Atom> functors)
    {
        DeclaringModule = module;
        Fixity = affix;
        Associativity = assoc;
        Synonyms = functors;
        CanonicalFunctor = Synonyms.First();
        Precedence = precedence;
    }
    public Operator(Atom module, OperatorType type, int precedence, HashSet<Atom> functors)
    {
        DeclaringModule = module;
        (Fixity, Associativity) = GetAffixAndAssociativity(type);
        Synonyms = functors;
        CanonicalFunctor = Synonyms.First();
        Precedence = precedence;
    }

    public Complex MakeComplex(ITerm lhs, Maybe<ITerm> mbRhs)
    {
        if (Fixity != Fixity.Infix || !mbRhs.TryGetValue(out var rhs))
            return new Complex(CanonicalFunctor, lhs).AsOperator(this);
        return new Complex(CanonicalFunctor, lhs, rhs).AsOperator(this);
    }

    public static (Fixity, OperatorAssociativity) GetAffixAndAssociativity(OperatorType type)
    {
        return type switch
        {
            OperatorType.fx => (Fixity.Prefix, OperatorAssociativity.None),
            OperatorType.xf => (Fixity.Postfix, OperatorAssociativity.None),
            OperatorType.fy => (Fixity.Prefix, OperatorAssociativity.Right),
            OperatorType.yf => (Fixity.Postfix, OperatorAssociativity.Left),
            OperatorType.xfx => (Fixity.Infix, OperatorAssociativity.None),
            OperatorType.xfy => (Fixity.Infix, OperatorAssociativity.Right),
            OperatorType.yfx => (Fixity.Infix, OperatorAssociativity.Left),
            _ => throw new NotSupportedException()
        };
    }

    public static OperatorType GetOperatorType(Fixity affix, OperatorAssociativity associativity)
    {
        return (affix, associativity) switch
        {
            (Fixity.Prefix, OperatorAssociativity.Right) => OperatorType.fx,
            (Fixity.Postfix, OperatorAssociativity.Left) => OperatorType.xf,
            (Fixity.Infix, OperatorAssociativity.None) => OperatorType.xfx,
            (Fixity.Infix, OperatorAssociativity.Right) => OperatorType.xfy,
            (Fixity.Infix, OperatorAssociativity.Left) => OperatorType.yfx,
            _ => throw new NotSupportedException()
        };
    }

    public string Explain() => $"← op({Precedence}, {GetOperatorType(Fixity, Associativity)}, [{Synonyms.Join(s => s.Explain())}])";
}

