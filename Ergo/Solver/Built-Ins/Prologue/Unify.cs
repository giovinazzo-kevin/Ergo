using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Unify : SolverBuiltIn
{
    public Unify()
        : base("", new("unify"), Maybe<int>.Some(2), WellKnown.Modules.Prologue)
    {
    }
    public override int OptimizationOrder => base.OptimizationOrder + 1000;
    public override List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes)
    {
        SimplifyConstantUnifications();
        RemoveDeadUnifications();
        return nodes;
        void SimplifyConstantUnifications()
        {
            // By now most evaluations on constants have reduced down to the form:
            // unify(__Kx, c)
            // When we find this pattern, we can remove the unification and replace __Kx with c wherever it occurs.
            var constantUnifications = nodes.Where(x => x is BuiltInNode b && b.BuiltIn is Unify && IsConstUnif(b.Goal.GetArguments()))
                .ToDictionary(x => (Variable)((BuiltInNode)x).Goal.GetArguments()[0]);
            var subs = constantUnifications
                .Select(kv => new Substitution(kv.Key, ((BuiltInNode)kv.Value).Goal.GetArguments()[1]))
                .ToList();
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var current = nodes[i];
                if (constantUnifications.Values.Contains(current))
                {
                    nodes.RemoveAt(i);
                    continue;
                }
                nodes[i] = current.Substitute(subs);
            }
            bool IsConstUnif(ImmutableArray<ITerm> args) => args[0] is Variable { Ignored: true } && args[1].IsGround;
        }

        void RemoveDeadUnifications()
        {
            // When compiling queries and hooks, unification stubs are generated for all arguments.
            // If these variables are not used, however, they result in dead code of the form:
            // unify(_X, _)
            // Where _X isn't referenced anywhere else. These are safe to remove.
            var refCounts = nodes.Where(x => x is DynamicNode).Cast<DynamicNode>()
                .SelectMany(x => x.Goal.Variables)
                .ToLookup(v => v.Name)
                .ToDictionary(l => l.Key, l => l.Count());
            var deadUnifications = nodes.Where(x => x is BuiltInNode b && b.BuiltIn is Unify && IsDeadUnif(b.Goal.GetArguments()))
                .ToDictionary(x => (Variable)((BuiltInNode)x).Goal.GetArguments()[0]);
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                var current = nodes[i];
                if (deadUnifications.Values.Contains(current))
                {
                    nodes.RemoveAt(i);
                    continue;
                }
            }
            bool IsDeadUnif(ImmutableArray<ITerm> args) => args[0] is Variable { Ignored: true, Name: var name } && refCounts[name] == 1;
        }
    }

    public override Maybe<ExecutionNode> Optimize(BuiltInNode node)
    {
        var args = node.Goal.GetArguments();
        //if (args[0] is Variable { Ignored: true } && args[1] is Variable)
        //    return TrueNode.Instance; // TODO: verify, might be sketchy
        if (!node.Goal.IsGround)
            return node;
        if (!args[0].Unify(args[1]).TryGetValue(out _))
            return FalseNode.Instance;
        return TrueNode.Instance;
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        if (Substitution.Unify(new(arguments[0], arguments[1])).TryGetValue(out var subs))
            yield return True(subs);
        else
            yield return False();
    }
}
