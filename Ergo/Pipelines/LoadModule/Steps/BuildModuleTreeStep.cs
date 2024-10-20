using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Lang.Utils;
using Ergo.Modules;
using Ergo.Modules.Directives;
using Ergo.Modules.Libraries;

namespace Ergo;
public interface IBuildModuleTreeStep : IErgoPipeline<LegacyErgoParser, ErgoModuleTree, IBuildModuleTreeStep.Env>
{
    public interface Env
    {
        Maybe<ErgoModuleTree> ModuleTree { get; set; }
        Maybe<Atom> CurrentModule { get; set; }
        ISet<Operator> Operators { get; }
    }
}
public class BuildModuleTreeStep(IServiceProvider sp) : IBuildModuleTreeStep
{
    public Either<ErgoModuleTree, PipelineError> Run(LegacyErgoParser parser, IBuildModuleTreeStep.Env env)
    {
        var moduleTree = env.ModuleTree.GetOrLazy(() => new ErgoModuleTree(sp));
        // Build the module tree recursively by executing the directives found in each module
        var directivesAST = parser.ProgramDirectives2();
        var ctx = new ErgoDirective.Context(moduleTree, env.CurrentModule);
        foreach(var ast in directivesAST)
        {
            var sig = ast.Body.GetSignature();
            if (!TryGetImpl(sig, out var impl))
                throw new InterpreterException(ErgoInterpreter.ErrorType.UndefinedDirective, sig.Explain());
            impl.Execute(ref ctx, ast.Body.GetArguments());
        }
        // Make the parser aware of all operators that were just defined
        env.Operators.UnionWith(ctx.CurrentModule.Operators);
        parser.AddOperators(env.Operators);
        // Parse the clauses and return the module as-is
        ctx.CurrentModule.Clauses.AddRange(parser.ProgramClauses2());
        return moduleTree;
        bool TryGetImpl(Signature sig, out ErgoDirective impl)
        {
            impl = DeclareModule.Instance;
            if (sig.Equals(DeclareModule.Instance.Signature))
                return true;
            return moduleTree.Directives.TryGetValue(sig, out impl!)
                || moduleTree.Directives.TryGetValue(sig.WithArity(default), out impl!); // variadic match
        }
    }
}

