using Ergo.Compiler;
using Ergo.Lang.Compiler;

namespace Ergo.Pipelines;

public interface ICompilePredicatePipeline : IErgoPipeline<PredicateDefinition, PredicateNode, ICompilePredicatePipeline.Env>
{
    public interface Env
        : ICompilePredicateStep.Env
        ;
}
