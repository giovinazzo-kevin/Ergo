using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(2))
    {
    }

    public override Maybe<ExecutionNode> Optimize(BuiltInNode node)
    {
        var args = node.Goal.GetArguments();
        if (!args[1].IsGround)
            return node;
        var ret = new Eval().Evaluate(null, default, args[1]);
        if (args[0].IsGround)
        {
            if (args[0].Equals(new Atom(ret)))
                return TrueNode.Instance;
        }
        else if (node.Node.Graph.GetNode(WellKnown.Signatures.Unify).TryGetValue(out var unifyNode))
        {
            return new BuiltInNode(unifyNode, new Complex(WellKnown.Signatures.Unify.Functor, args[0], new Atom(ret)), Unify.Instance);
        }
        return FalseNode.Instance;
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var eval = scope.InterpreterScope.ExceptionHandler.TryGet(() => new Atom(Evaluate(context.Solver, scope, arguments[1])));
        if (eval.TryGetValue(out var result) && LanguageExtensions.Unify(arguments[0], result).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }
        yield return False();
    }
}
