using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly partial struct Operator
{
    public readonly Atom CanonicalFunctor;
    public readonly Atom[] Synonyms;
    public readonly Atom DeclaringModule;
    public readonly int Precedence;
    public readonly OperatorAffix Affix;
    public readonly OperatorAssociativity Associativity;

    public Operator(Atom module, OperatorAffix affix, OperatorAssociativity assoc, int precedence, params Atom[] functors)
    {
        DeclaringModule = module;
        Affix = affix;
        Associativity = assoc;
        Synonyms = functors;
        CanonicalFunctor = Synonyms.First();
        Precedence = precedence;
    }
    public Operator(Atom module, OperatorType type, int precedence, params Atom[] functors)
    {
        DeclaringModule = module;
        (Affix, Associativity) = GetAffixAndAssociativity(type);
        Synonyms = functors;
        CanonicalFunctor = Synonyms.First();
        Precedence = precedence;
    }

    public static (OperatorAffix, OperatorAssociativity) GetAffixAndAssociativity(OperatorType type)
    {
        return type switch
        {
            OperatorType.fx => (OperatorAffix.Prefix, OperatorAssociativity.Right),
            OperatorType.xf => (OperatorAffix.Postfix, OperatorAssociativity.Left),
            OperatorType.xfx => (OperatorAffix.Infix, OperatorAssociativity.None),
            OperatorType.xfy => (OperatorAffix.Infix, OperatorAssociativity.Right),
            OperatorType.yfx => (OperatorAffix.Infix, OperatorAssociativity.Left),
            _ => throw new NotSupportedException()
        };
    }

    public static OperatorType GetOperatorType(OperatorAffix affix, OperatorAssociativity associativity)
    {
        return (affix, associativity) switch
        {
            (OperatorAffix.Prefix, OperatorAssociativity.Right) => OperatorType.fx,
            (OperatorAffix.Postfix, OperatorAssociativity.Left) => OperatorType.xf,
            (OperatorAffix.Infix, OperatorAssociativity.None) => OperatorType.xfx,
            (OperatorAffix.Infix, OperatorAssociativity.Right) => OperatorType.xfy,
            (OperatorAffix.Infix, OperatorAssociativity.Left) => OperatorType.yfx,
            _ => throw new NotSupportedException()
        };
    }

    public string Explain() => $"← op({Precedence}, {GetOperatorType(Affix, Associativity)}, [{string.Join(",", Synonyms.Select(s => s.Explain()))}])";
}

