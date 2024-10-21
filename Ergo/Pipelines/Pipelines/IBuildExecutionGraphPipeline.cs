using Ergo.Compiler;

namespace Ergo.Pipelines;

public interface IBuildExecutionGraphPipeline : IErgoPipeline<Atom, ErgoExecutionGraph, IBuildExecutionGraphPipeline.Env>
{
    public interface Env
        : IBuildModuleTreePipeline.Env
        , IBuildDependencyGraphPipeline.Env
        , IBuildExecutionGraphStep.Env
        ;
}