using Ergo.Solver;
using Ergo.Solver.BuiltIns;

namespace Ergo.Lang.Compiler;

public class BuiltInNode : GoalNode
{
    static readonly Signature UnifySignature = new(new Atom("unify"), 2, WellKnown.Modules.Prologue, default);

    public SolverBuiltIn BuiltIn { get; }
    public BuiltInNode(DependencyGraphNode node, ITerm goal, SolverBuiltIn builtIn) : base(node, goal)
    {
        BuiltIn = builtIn;
    }

    public override IEnumerable<ExecutionScope> Execute(SolverContext ctx, SolverScope solverScope, ExecutionScope execScope)
    {
        Goal.Substitute(execScope.CurrentSubstitutions).GetQualification(out var inst);
        foreach (var eval in BuiltIn.Apply(ctx, solverScope, inst.GetArguments()))
        {
            if (eval.Result)
                yield return execScope.ApplySubstitutions(eval.Substitutions).AsSolution();
            else yield break;
        }
    }
    public override ExecutionNode Optimize()
    {
        if (BuiltIn is Ground) return Ground();
        if (BuiltIn is Unify) return Unify();
        if (BuiltIn is Not) return Not();
        if (BuiltIn is Eval) return Eval();
        return this;
        ExecutionNode Ground() => Goal.IsGround ? TrueNode.Instance : FalseNode.Instance;
        ExecutionNode Unify()
        {
            if (!Goal.IsGround)
                return this;
            var args = Goal.GetArguments();
            if (args[0].Unify(args[1]).TryGetValue(out _))
                return TrueNode.Instance;
            return FalseNode.Instance;
        }
        ExecutionNode Not()
        {
            if (!Goal.IsGround)
                return this;
            var arg = Goal.GetArguments()[0].ToExecutionNode(Node.Graph).Optimize();
            if (arg is TrueNode)
                return FalseNode.Instance;
            if (arg is FalseNode)
                return TrueNode.Instance;
            return this;
        }
        ExecutionNode Eval()
        {
            var args = Goal.GetArguments();
            if (!args[1].IsGround)
                return this;
            var ret = new Eval().Evaluate(null, default, args[1]);
            if (args[0].IsGround)
            {
                if (args[0].Equals(new Atom(ret)))
                    return TrueNode.Instance;
            }
            else if (Node.Graph.GetNode(UnifySignature).TryGetValue(out var unifyNode))
            {
                return new BuiltInNode(unifyNode, new Complex(new Atom("unify"), args[0], new Atom(ret)), new Unify());
            }
            return FalseNode.Instance;
        }
    }
    public override ExecutionNode Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        return new BuiltInNode(Node, Goal.Instantiate(ctx, vars), BuiltIn);
    }
    public override ExecutionNode Substitute(IEnumerable<Substitution> s)
    {
        return new BuiltInNode(Node, Goal.Substitute(s), BuiltIn);
    }
}
