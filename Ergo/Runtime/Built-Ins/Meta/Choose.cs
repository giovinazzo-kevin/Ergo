namespace Ergo.Runtime.BuiltIns;

public sealed class Choose : ErgoBuiltIn
{
    public Random Rng { get; set; } = new Random();
    private readonly Call CallInst = new();

    public Choose()
        : base("", new("choose"), default, WellKnown.Modules.Meta)
    {
    }

    public override Op Compile() => vm =>
    {
        var arg = vm.Args[Rng.Next(vm.Arity)];
        vm.Arity = 1;
        vm.SetArg(0, arg);
        CallInst.Compile()(vm);
    };
}
