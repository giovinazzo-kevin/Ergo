namespace Ergo;

public record PipelineError(IErgoPipeline Step, Exception Exception);
