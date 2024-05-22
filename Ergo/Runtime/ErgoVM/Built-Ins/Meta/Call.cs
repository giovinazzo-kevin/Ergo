using Ergo.Lang.Compiler;

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
        if (args.Length <= 1)
        {
            vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(0)).Explain());
            return;
        }
        if (args[1] is VariableAddress)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, args[1].Deref(vm).Explain());
            return;
        }
        //var strct = args[1] switch
        //{
        //    StructureAddress s => vm.Memory[s],
        //    AbstractAddress a when vm.Memory[a].Address is StructureAddress s => vm.Memory[s],
        //    _ => throw new NotSupportedException()
        //};
        var goal = args[1].Deref(vm);
        for (int i = 2; i < args.Length; i++)
            goal = goal.Concat(args[i].Deref(vm));
        if (goal is not NTuple comma)
            comma = new([goal], goal.Scope);
        var query = new Query(comma);
        var newVm = vm.ScopedInstance();
        newVm.Query = newVm.CompileQuery(query);
        newVm.Run();
        foreach (var sol in newVm.Solutions)
            vm.Solution(sol.Substitutions);
    };
}
