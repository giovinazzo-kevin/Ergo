namespace Ergo.Runtime.BuiltIns;

public sealed class Call : BuiltIn
{
    public Call()
        : base("", new("call"), Maybe<int>.None, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args.Length == 0)
        {
            vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(0)).Explain());
            return;
        }
        var goal = args[0];
        for (int i = 1; i < args.Length; i++)
            goal = goal.Concat(args[i]);
        if (goal is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, goal.Explain());
            return;
        }
        if (goal is not NTuple comma)
            comma = new([goal], goal.Scope);
        var query = new Query(comma);
        vm.CompileQuery(query)(vm);
    };
}
