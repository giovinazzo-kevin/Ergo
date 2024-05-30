namespace Ergo.Interpreter.Directives;

public class DeclareOperator : InterpreterDirective
{

    public DeclareOperator()
        : base("", "op", 3, 10)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (!args[0].Match<int>(out var precedence))
        {
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, scope, WellKnown.Types.Integer, args[0].Explain());
        }

        if (!args[1].Match<OperatorType>(out var type))
        {
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, scope, "OperatorType", args[1].Explain());
        }

        if (!args[2].Match<string[]>(out var synonyms))
        {
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, scope, WellKnown.Types.List, args[2].Explain());
        }

        var (affix, assoc) = Operator.GetAffixAndAssociativity(type);
        var existingOperators = scope.VisibleOperators;
        foreach (var op in existingOperators.Where(x => x.Fixity == affix))
        {
            var intersectingSynonyms = op.Synonyms.Select(x => x.Explain()).Intersect(synonyms);
            // Operators can be re-defined, but only if the new definition covers all synonyms.
            if (intersectingSynonyms.Any())
            {
                if (intersectingSynonyms.Count() != op.Synonyms.Count)
                {
                    throw new InterpreterException(ErgoInterpreter.ErrorType.OperatorClash, scope, args[2].Explain());
                }
            }
        }

        var synonymAtoms = synonyms.Select(x => (Atom)x).ToHashSet();
        scope = scope.WithModule(scope.EntryModule
            .WithOperator(new(scope.Entry, affix, assoc, precedence, synonymAtoms)));
        return true;
    }
}
