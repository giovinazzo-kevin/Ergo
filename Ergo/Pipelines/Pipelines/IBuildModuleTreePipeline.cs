using Ergo.Modules;

namespace Ergo.Pipelines;
public interface IBuildModuleTreePipeline : IErgoPipeline<Atom, ErgoModuleTree, IBuildModuleTreePipeline.Env>
{
    public interface Env
        : ILocateModuleStep.Env
        , IStreamFileStep.Env
        , IParseStreamStep.Env
        , IBuildModuleTreeStep.Env
        ;
}
