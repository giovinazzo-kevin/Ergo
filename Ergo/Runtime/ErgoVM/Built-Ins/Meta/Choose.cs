namespace Ergo.Runtime.BuiltIns;

public sealed class Choose : BuiltIn
{
    public Random Rng { get; set; } = new Random();
    private readonly Call CallInst = new();

    public Choose()
        : base("", "choose", default, WellKnown.Modules.Meta)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var arg = vm.Args2[Rng.Next(1, vm.Arity)];
        vm.Arity = 1;
        vm.SetArg2(1, arg);
        CallInst.Compile()(vm);
    };
}
