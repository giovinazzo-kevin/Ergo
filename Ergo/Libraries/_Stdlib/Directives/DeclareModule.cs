namespace Ergo.Modules.Directives;

public class DeclareModule() : ErgoDirective("", new("module"), 2, 0)
{
    public static readonly DeclareModule Instance = new();

    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        if (args[0] is not Atom moduleName)
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, default, WellKnown.Types.String, args[0].Explain());
        if (args[1] is not List exports)
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, default, WellKnown.Types.List, args[1].Explain());
        if (ctx.ModuleTree[moduleName].TryGetValue(out var module))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ModuleRedefinition, default, moduleName.Explain());
        var exportSigs = new List<Signature>();
        foreach (var exp in exports.Contents)
        {
            if (!exp.Match(out var sig, new { Predicate = default(string), Arity = default(int) }))
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, default, WellKnown.Types.Signature, exp.Explain());
            exportSigs.Add(new Signature(new Atom(sig.Predicate), sig.Arity, default, default));
        }
        module = ctx.ModuleTree.Declare(moduleName);
        foreach (var sig in exportSigs)
        {
            var pInfo = module.GetMetaTableEntry(sig);
            module.SetMetaTableEntry(sig, pInfo with { IsExported = true });
        }
        ctx = ctx with { CurrentModuleName = moduleName };
        return true;
    }
}
