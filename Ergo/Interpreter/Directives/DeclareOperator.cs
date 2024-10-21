namespace Ergo.Modules.Directives;

public class DeclareOperator() : ErgoDirective("", new("op"), 3, 10)
{

    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        if (!args[0].Match<int>(out var precedence))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Integer, args[0].Explain());
        if (!args[1].Match<OperatorType>(out var type))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, "OperatorType", args[1].Explain());
        if (!args[2].Match<string[]>(out var synonyms))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.List, args[2].Explain());
        var (affix, assoc) = Operator.GetAffixAndAssociativity(type);
        foreach (var op in ctx.ModuleTree.Operators.Where(x => x.Fixity == affix))
        {
            var intersectingSynonyms = op.Synonyms
                .Select(x => x.Explain())
                .Intersect(synonyms);
            // Operators can be re-defined, but only if the new definition covers all synonyms.
            if (intersectingSynonyms.Any())
            {
                if (intersectingSynonyms.Count() != op.Synonyms.Count)
                    throw new InterpreterException(ErgoInterpreter.ErrorType.OperatorClash, args[2].Explain());
            }
        }
        var synonymAtoms = synonyms.Select(x => new Atom(x)).ToHashSet();
        ctx.CurrentModule.Operators.Add(new(ctx.CurrentModule.Name, affix, assoc, precedence, synonymAtoms));
        return true;
    }
}
