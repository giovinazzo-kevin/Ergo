using Ergo.Pipelines;
using Ergo.Pipelines.LoadModule;

namespace Ergo.Modules.Directives;

public class UseModule(IErgoEnv env, ILoadModulePipeline loadModule) 
    : ErgoDirective("", new("use_module"), 1, 1)
{
    public override bool Execute(ref Context ctx, ImmutableArray<ITerm> args)
    {
        if (args[0] is not Atom moduleName)
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.String, args[0].Explain());
        if (ctx.CurrentModule.Imports.Select(x => x.Name).Contains(moduleName))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ModuleAlreadyImported, moduleName.Explain());
        if (!ctx.ModuleTree[moduleName].TryGetValue(out var module))
        {
            env.ModuleTree = ctx.ModuleTree;
            var result = loadModule.Run(moduleName, env);
            if (result.TryGetB(out var err))
                throw err.Exception;
            module = result
                .GetAOrThrow()[moduleName]
                .GetOrThrow();
        }
        ctx.CurrentModule.Imports.Add(module);
        return true;
    }
}
