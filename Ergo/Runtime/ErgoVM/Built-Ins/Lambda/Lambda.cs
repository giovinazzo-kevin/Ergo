namespace Ergo.Runtime.BuiltIns;

public sealed class Lambda : BuiltIn
{

    public Lambda()
        : base("", WellKnown.Functors.Lambda.First(), Maybe<int>.None, WellKnown.Modules.Lambda)
    {
    }

    private readonly Call CallInst = new();
    public override ErgoVM.Op Compile() => vm =>
    {
        if (vm.Args2.Length < 3)
        {
            vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(vm.Args2.Length - 1)).Explain());
            return;
        }
        var (parameters, lambda) = (vm.Memory.Dereference(vm.Arg2(1)), vm.Memory.Dereference(vm.Arg2(2)));
        var rest = vm.Args2[3..vm.Arity];
        if (parameters is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, parameters.Explain());
            return;
        }
        // parameters is a plain list of variables; We don't need to capture free variables, unlike SWIPL which is compiled.
        if (parameters is not List list || list.Contents.Length > rest.Length || list.Contents.Any(x => x is not Variable))
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.LambdaParameters, parameters.Explain());
            return;
        }
        var vars = new Dictionary<string, Variable>();
        list = (List)list.Instantiate(vm.InstantiationContext, vars);
        lambda = lambda.Instantiate(vm.InstantiationContext, vars);
        for (var i = 0; i < Math.Min(rest.Length, list.Contents.Length); i++)
        {
            if (list.Contents[i].IsGround)
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.FreeVariable, list.Contents[i].Explain());
                return;
            }
            var todoRemove = vm.Memory.StoreTerm(list.Contents[i]);
            if (!vm.Memory.Unify(rest[i], todoRemove))
            {
                vm.Fail();
                return;
            }
            lambda = lambda.Substitute(vm.Env);
        }
        /*
        var extraArgs = rest.Length > list.Contents.Length ? rest[list.Contents.Length..] : ImmutableArray<ITerm>.Empty;
        CallInst.Compile()([lambda, .. extraArgs])(vm);
         */
        var extraArgs = rest.Length > list.Contents.Length ? rest[list.Contents.Length..] : [];
        vm.SetArg2(1, vm.Memory.StoreTerm(lambda));
        for (int i = 0; i < extraArgs.Length; i++)
            vm.SetArg2(i + 1, extraArgs[i]);
        vm.Arity = 2 + extraArgs.Length;
        CallInst.Compile()(vm);
    };
}
