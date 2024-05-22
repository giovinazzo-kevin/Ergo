namespace Ergo.Runtime.BuiltIns;

public sealed class Call : BuiltIn
{
    public Call()
        : base("", new("call"), Maybe<int>.None, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args2;
        if (args.Length == 0)
        {
            vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(0)).Explain());
            return;
        }
        var goal = vm.Memory.Dereference(args[1]);
        for (int i = 2; i < args.Length; i++)
            goal = goal.Concat(vm.Memory.Dereference(args[i]));
        if (goal is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, goal.Explain());
            return;
        }
        if (goal is not NTuple comma)
            comma = new([goal], goal.Scope);
        var query = new Query(comma);
        var scope = vm.ScopedInstance();
        scope.Query = scope.CompileQuery(query);
        var any = false;
        foreach (var sol in scope.RunInteractive())
        {
            any = true;
            vm.Solution(sol.Substitutions);
        }
        if (!any && scope.State == ErgoVM.VMState.Fail)
        {
            vm.Fail();
            return;
        }
    };
}
