using Ergo.Compiler;

namespace Ergo.Pipelines;

public interface IBuildDependencyGraphPipeline : IErgoPipeline<Atom, ErgoDependencyGraph, IBuildDependencyGraphPipeline.Env>
{
    public interface Env
        : IBuildModuleTreePipeline.Env
        , IBuildDependencyGraphStep.Env
        ;
}
