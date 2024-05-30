namespace Ergo.Interpreter.Directives;

// SEE: https://eu.swi-prolog.org/pldoc/man?section=metapred

public class DeclareMetaPredicate : InterpreterDirective
{
    public DeclareMetaPredicate()
        : base("", "meta_predicate", 1, 50)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        var termArgs = args[0].GetArguments();
        var metaArgs = new char[termArgs.Length];
        for (int i = 0; i < termArgs.Length; i++)
        {
            if (!termArgs[i].Match<string>(out var str) || str.Length > 1)
            {
                scope.Throw(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, typeof(Char).Name, termArgs[i].Explain());
                return false;
            }
            metaArgs[i] = str[0];
        }
        scope = scope.WithModule(scope.EntryModule
            .WithMetaPredicate(new(args[0].GetSignature().WithModule(scope.Entry), ImmutableArray.CreateRange(metaArgs))));
        return true;
    }
}
