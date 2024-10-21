using Ergo.Compiler;

namespace Ergo;

public interface ICompileClauseStep : IErgoPipeline<ClauseDefinition, ClauseNode, ICompileClauseStep.Env>
{
    public interface Env : ICompileGoalStep.Env
    {
    }
}

public class CompileClauseStep(ICompileGoalStep compileGoal) : ICompileClauseStep
{
    public Either<ClauseNode, PipelineError> Run(ClauseDefinition def, ICompileClauseStep.Env env)
    {
        foreach (var arg in def.Args)
        {

        }

        var goalNodes = new List<CallNode>();
        foreach (var goal in def.Goals)
        {
            var result = compileGoal.Run(goal, env);
            if (result.TryGetB(out var error))
                return error;
            goalNodes.Add(result.GetAOrThrow());
        }
        return new ClauseNode([.. goalNodes]);
    }
}