namespace Ergo.Interpreter.Libraries;


public class Hook
{
    private static readonly InstantiationContext ctx = new("_H");
    private readonly ITerm[] args;
    public readonly Signature Signature;
    private Maybe<ErgoVM.Op> cached = default;
    private ITerm head = default;
    public Hook(Signature sig)
    {
        Signature = sig;
        args = new ITerm[Signature.Arity.GetOr(ErgoVM.MAX_ARGUMENTS)];
        for (int i = 0; i < args.Length; i++)
            args[i] = ctx.GetFreeVariable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArg(int i, ITerm arg)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, args.Length);
        args[i] = arg;
    }

    /// <summary>
    /// Creates a wrapper that compiles the hook just in time when it is first called.
    /// </summary>
    public ErgoVM.Op Compile(bool throwIfNotDefined = true)
    {
        return vm =>
        {
            if (cached.TryGetValue(out var op))
            {
                op(vm);
                return;
            }
            // Compile and cache the hook the first time it's called
            // TODO: Invalidate cache when any predicate matching this hook is asserted or retracted
            if (!vm.KB.Get(Signature).TryGetValue(out var preds))
            {
                if (throwIfNotDefined)
                    vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.Explain());
                else
                    vm.Fail();
                return;
            }
            var ops = new ErgoVM.Op[preds.Count];
            for (int i = 0; i < preds.Count; i++)
            {
                var predHead = preds[i].Unqualified().Head;
                if (!preds[i].ExecutionGraph.TryGetValue(out var graph))
                {
                    return;
                }
                var gOp = graph.Compile();
                ops[i] = vm =>
                {
                    vm.SetArg(0, head);
                    vm.SetArg(1, predHead);
                    ErgoVM.Goals.Unify2(vm);
                    if (vm.State == ErgoVM.VMState.Fail)
                        return;
                    gOp(vm);
                };
            }
            var branch = ErgoVM.Ops.Or(ops);
            cached = op = vm =>
            {
                head = args.Length > 0 ? new Complex(Signature.Functor, args) : Signature.Functor;
                branch(vm);
            };
            op(vm);
        };
    }

}