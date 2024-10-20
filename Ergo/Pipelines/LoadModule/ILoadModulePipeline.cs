using Ergo.Lang.Ast;
using Ergo.Modules;

namespace Ergo.Pipelines.LoadModule;
public interface ILoadModulePipeline : IErgoPipeline<Atom, ErgoModuleTree, ILoadModulePipeline.Env>
{
    public interface Env
        : ILocateModuleStep.Env
        , IStreamFileStep.Env
        , IParseStreamStep.Env
        , IBuildModuleTreeStep.Env
        ;
}
