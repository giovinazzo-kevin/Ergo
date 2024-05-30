namespace Ergo.Interpreter.Directives;

public class UseModule : InterpreterDirective
{
    public UseModule()
        : base("", "use_module", 1, 1)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (args[0] is not Atom moduleName)
        {
            scope.Throw(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.String, args[0].Explain());
            return false;
        }

        if (moduleName == scope.Entry || scope.EntryModule.Imports.Contents.Contains(moduleName))
        {
            scope.Throw(ErgoInterpreter.ErrorType.ModuleAlreadyImported, args[0].Explain());
            return false;
        }

        if (!scope.Modules.TryGetValue(moduleName, out var module))
        {
            var importScope = scope
                .WithModule(new Module(moduleName, scope.IsRuntime));
            if (!interpreter.LoadDirectives(ref importScope, moduleName).TryGetValue(out module))
                return false;
            // Pull all new modules that were imported by the module currently being imported
            foreach (var newModule in importScope.Modules)
            {
                if (newModule.Key != module.Name && !scope.Modules.ContainsKey(newModule.Key))
                {
                    scope = scope.WithModule(newModule.Value);
                }
            }
        }
        scope = scope
            .WithModule(module)
            .WithModule(scope.Modules[scope.Entry]
                .WithImport(moduleName));
        return true;
    }
}
