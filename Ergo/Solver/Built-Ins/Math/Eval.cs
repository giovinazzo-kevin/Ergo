using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Eval : MathBuiltIn
{
    public Eval()
        : base("", new("eval"), Maybe<int>.Some(2))
    {
    }

    public override int OptimizationOrder => base.OptimizationOrder + 20;
    public override ExecutionNode Optimize(BuiltInNode node)
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
            return new BuiltInNode(unifyNode, new Complex(WellKnown.Signatures.Unify.Functor, args[0], new Atom(ret)), node.Node.Graph.UnifyInstance);
        }
        return FalseNode.Instance;
    }

    public override ErgoVM.Op Compile(ImmutableArray<ITerm> arguments) => vm =>
    {
        var eval = vm.Scope.InterpreterScope.ExceptionHandler.TryGet(() => new Atom(Evaluate(vm.Context.Solver, vm.Scope, arguments[1])));
        if (!eval.TryGetValue(out var value))
        {
            vm.Fail();
            return;
        }
        ErgoVM.Ops.Unify(arguments[0], value)(vm);
    };

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
