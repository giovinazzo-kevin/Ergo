using Ergo.Pipelines;
using Ergo.Pipelines.LoadModule;

namespace Ergo.Modules.Directives;

public class UseModule(IErgoEnv env, ILoadModulePipeline loadModule) 
    : ErgoDirective("", new("use_module"), 1, 1)
{
    public override  bool Execute(ErgoModuleTree moduleTree, Atom currentModule, ImmutableArray<ITerm> args)
    {
        if (args[0] is not Atom moduleName)
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.String, args[0].Explain());
        if (moduleTree[currentModule].GetOrThrow().ImportedModules.Select(x => x.Name).Contains(moduleName))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ModuleAlreadyImported, moduleName.Explain());
        if (!moduleTree[moduleName].TryGetValue(out var module))
        {
            var result = loadModule.Run(moduleName, env);
            if (result.TryGetB(out var err))
                throw err.Exception;
        }
        scope = scope
            .WithModule(module)
            .WithModule(scope.Modules[scope.Entry]
                .WithImport(moduleName));
        return true;
    }
}
