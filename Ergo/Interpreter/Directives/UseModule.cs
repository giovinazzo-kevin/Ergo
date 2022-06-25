namespace Ergo.Interpreter.Directives;

public class UseModule : InterpreterDirective
{
    public UseModule()
        : base("", new("use_module"), Maybe.Some(1), 1)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (args[0] is not Atom moduleName)
        {
            throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, WellKnown.Types.String, args[0].Explain());
        }

        if (moduleName == scope.Entry || scope.EntryModule.Imports.Contents.Contains(moduleName))
        {
            throw new InterpreterException(InterpreterError.ModuleAlreadyImported, scope, args[0].Explain());
        }

        if (!scope.Modules.TryGetValue(moduleName, out var module))
        {
            var importScope = scope;
            if (!interpreter.LoadDirectives(ref importScope, moduleName).TryGetValue(out module))
                return false;
        }

        scope = scope
            .WithModule(module)
            .WithModule(scope.Modules[scope.Entry]
                .WithImport(moduleName));
        return true;
    }
}
