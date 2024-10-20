using Ergo.Pipelines.LoadModule;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Modules.Directives;

namespace Ergo;

public interface IErgoPipeline
{
    public Type InterType { get; }
    public Type OutputType { get; }
    public Type EnvType { get; }
}
public interface IErgoPipeline<TInput, TOutput, in TEnv> : IErgoPipeline
{
    Type IErgoPipeline.InterType => typeof(TInput);
    Type IErgoPipeline.OutputType => typeof(TOutput);
    Type IErgoPipeline.EnvType => typeof(TEnv);
    public Either<TOutput, PipelineError> Run(TInput input, TEnv environment);
}