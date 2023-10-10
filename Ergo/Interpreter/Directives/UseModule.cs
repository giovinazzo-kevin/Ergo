namespace Ergo.Interpreter.Directives;

public class UseModule : InterpreterDirective
{
    public UseModule()
        : base("", new("use_module"), 1, 1)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (args[0] is not Atom moduleName)
        {
            scope.Throw(InterpreterError.ExpectedTermOfTypeAt, WellKnown.Types.String, args[0].Explain());
            return false;
        }

        if (moduleName == scope.Entry || scope.EntryModule.Imports.Contents.Contains(moduleName))
        {
            scope.Throw(InterpreterError.ModuleAlreadyImported, args[0].Explain());
            return false;
        }

        if (!scope.Modules.TryGetValue(moduleName, out var module))
        {
            var importScope = scope
                .WithModule(new Module(moduleName, scope.IsRuntime)
                    /*.WithImport(scope.Entry)*/);
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
