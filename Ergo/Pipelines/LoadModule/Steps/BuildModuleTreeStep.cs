using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Lang.Utils;
using Ergo.Modules;
using Ergo.Modules.Directives;
using Ergo.Modules.Libraries;

namespace Ergo;
public interface IBuildModuleTreeStep : IErgoPipeline<ErgoStream, ErgoModuleTree, IBuildModuleTreeStep.Env>
{
    public interface Env
    {
        ISet<Operator> Operators { get; }
        ErgoLexer.StreamState StreamState { get; set; }
    }
}
public class BuildModuleTreeStep(IServiceProvider sp) : IBuildModuleTreeStep
{
    public Either<ErgoModuleTree, PipelineError> Run(ErgoStream input, IBuildModuleTreeStep.Env env)
    {
        var moduleTree = new ErgoModuleTree(sp);
        var lexer = new ErgoLexer(default, input, env.Operators);
        var parser = new ErgoParser(default, lexer);
        Commit();
        // Bootstrap the parser by first loading the operator symbols defined in this module
        var newOperators = parser.OperatorDeclarations();
        lexer.AddOperators(newOperators);
        env.Operators.Union(newOperators);
        Backtrack();
        // Build the module tree recursively by executing the directives found in each module
        var directivesAST = parser.ProgramDirectives2();
        Backtrack();
        var directivesImpl = directivesAST
            .Select(x => (Signature: x.Body.GetSignature(), x.Body))
            .Select(x => (x.Signature, x.Body, Implementation: Maybe.FromTryGet(() => (TryGetImpl(x.Signature, out var impl), impl))))
            .ToArray();
        var undefinedDirectives = directivesImpl
            .Where(x => !x.Implementation.HasValue)
            .Select(x => new InterpreterException(ErgoInterpreter.ErrorType.UndefinedDirective, x.Signature.Explain()))
            .ToArray();
        if (undefinedDirectives.Length > 0)
            throw new AggregateException(undefinedDirectives);
        var definedDirectives = directivesImpl
            .Where(x => x.Implementation.HasValue);
        foreach (var (sig, body, dir) in definedDirectives)
        {
            if (TryGetImpl(sig, out var impl))
                impl.Execute(moduleTree, body.GetArguments());
        }
        // Then parse the clauses and return the module
        foreach (var clause in parser.ProgramClauses2())
        {
        }


        return moduleTree;

        bool TryGetImpl(Signature sig, out ErgoDirective impl)
            => moduleTree.Directives.TryGetValue(sig, out impl!)
            || moduleTree.Directives.TryGetValue(sig.WithArity(default), out impl!); // variadic match
        void Commit() => env.StreamState = parser.Lexer.State;
        void Backtrack() => parser.Lexer.Seek(env.StreamState);
    }
}

