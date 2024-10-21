using Ergo.Compiler;

namespace Ergo;
public interface ICompilePredicateStep : IErgoPipeline<PredicateDefinition, PredicateNode, ICompilePredicateStep.Env>
{
    public interface Env : ICompileClauseStep.Env
    {
    }
}

public class CompilePredicateStep(ICompileClauseStep compileClause) : ICompilePredicateStep
{
    public Either<PredicateNode, PipelineError> Run(PredicateDefinition def, ICompilePredicateStep.Env env)
    {
        var clauseNodes = def.Clauses
            .Select(clause => compileClause.Run(clause, env)
                .GetAOrThrow());
        return new PredicateNode([..clauseNodes], def.BuiltIn.Select(x => new BuiltInNode(x)));
    }
}
