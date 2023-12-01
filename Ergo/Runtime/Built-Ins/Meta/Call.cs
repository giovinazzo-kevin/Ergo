
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Call : BuiltIn
{
    public Call()
        : base("", new("call"), Maybe<int>.None, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Goal Compile() => args =>
    {
        var undefined = args.Length == 0;
        var goal = args.Aggregate((a, b) => a.Concat(b));
        var goalIsVar = goal is Variable;
        return vm =>
        {
            if (undefined)
            {
                vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(0)).Explain());
                return;
            }
            if (goalIsVar)
            {
                vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, goal.Explain());
                return;
            }
            if (goal is not NTuple comma)
            {
                comma = new(ImmutableArray<ITerm>.Empty.Add(goal), goal.Scope);
            }
            var query = new Query(comma);
            vm.CompileQuery(query)(vm);
        };
    };
}
