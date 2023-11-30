namespace Ergo.Interpreter.Directives;

public class DeclareModule : InterpreterDirective
{
    public DeclareModule()
        : base("", new("module"), 2, 0)
    {
    }
    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (args[0] is not Atom moduleName)
        {
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, scope, WellKnown.Types.String, args[0].Explain());
        }

        if (!scope.IsRuntime && scope.Entry == moduleName)
        {
            throw new InterpreterException(ErgoInterpreter.ErrorType.ModuleRedefinition, scope, scope.Entry.Explain(), moduleName.Explain());
        }

        if (args[1] is not List exports)
        {
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, scope, WellKnown.Types.List, args[1].Explain());
        }

        if (scope.Modules.TryGetValue(moduleName, out var module))
        {
            if (!module.IsRuntime)
            {
                throw new InterpreterException(ErgoInterpreter.ErrorType.ModuleNameClash, scope, moduleName.Explain());
            }

            module = module.WithExports(exports.Contents);
        }
        else
        {
            module = new Module(moduleName, runtime: scope.IsRuntime)
                .WithExports(exports.Contents)
                .WithImport(scope.BaseImport);
        }
        module = module.WithLinkedLibrary(interpreter.GetLibrary(module.Name));
        scope = scope
            .WithModule(module)
            .WithCurrentModule(module.Name);
        foreach (var item in exports.Contents)
        {
            // make sure that 'item' is in the form 'predicate/arity'
            if (!item.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
            {
                throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, scope, WellKnown.Types.Signature, item.Explain());
            }
        }

        return true;
    }
}
