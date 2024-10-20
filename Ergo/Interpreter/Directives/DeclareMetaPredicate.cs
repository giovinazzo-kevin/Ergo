namespace Ergo.Modules.Directives;

// SEE: https://eu.swi-prolog.org/pldoc/man?section=metapred

public class DeclareMetaPredicate() : ErgoDirective("", new ("meta_predicate"), 1, 50)
{
    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        var termArgs = args[0].GetArguments();
        var metaArgs = new char[termArgs.Length];
        for (int i = 0; i < termArgs.Length; i++)
        {
            if (!termArgs[i].Match<string>(out var str) || str.Length > 1)
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, typeof(Char).Name, termArgs[i].Explain());
            metaArgs[i] = str[0];
        }
        var pTable = ctx.CurrentModule.MetaPredicateTable;
        var sig = args[0].GetSignature().WithModule(ctx.CurrentModule.Name);
        if (!pTable.TryGetValue(sig, out var pInfo))
            pTable[sig] = pInfo = new();
        pTable[sig] = pInfo with { MetaArguments = ImmutableArray.CreateRange(metaArgs) };
        return true;
    }
}
