namespace Ergo.Modules.Directives;

public class DeclareModule : ErgoDirective
{
    public DeclareModule()
        : base("", new("module"), 2, 0)
    {
    }
    public override  bool Execute(ErgoModuleTree moduleTree, Atom currentModule, ImmutableArray<ITerm> args)
    {
        if (args[0] is not Atom moduleName)
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, default, WellKnown.Types.String, args[0].Explain());
        if (!args[0].Equals(currentModule))
            throw new InvalidOperationException($"module/2 can only be executed in the context of the module being loaded");
        if (args[1] is not List exports)
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, default, WellKnown.Types.List, args[1].Explain());
        if (moduleTree[moduleName].TryGetValue(out var module))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ModuleRedefinition, default, moduleName.Explain());
        module = moduleTree.Define(moduleName);
        foreach (var exp in exports.Contents)
        {
            if (!exp.Match(out var sig, new { Predicate = default(string), Arity = default(int) }))
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, default, WellKnown.Types.Signature, exp.Explain());
            module.Export(new Signature(new Atom(sig.Predicate), sig.Arity, default, default));
        }
        return true;
    }
}
