using Ergo.Compiler;

namespace Ergo.Pipelines.LoadModule;

public interface IBuildDependencyGraphPipeline : IErgoPipeline<Atom, ErgoDependencyGraph, IBuildDependencyGraphPipeline.Env>
{
    public interface Env
        : IBuildModuleTreePipeline.Env
        , IBuildDependencyGraphStep.Env
        ;
}