namespace Ergo.Runtime.BuiltIns;

public sealed class LambdaCall : ErgoBuiltIn
{

    public LambdaCall()
        : base("", WellKnown.Functors.Lambda.First(), Maybe<int>.None, WellKnown.Modules.Lambda)
    {
    }

    private readonly Call CallInst = new();
    public override Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args.Length < 2)
        {
            vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(args.Length)).Explain());
            return;
        }
        var (parameters, lambda) = (args[0], args[1]);
        var rest = vm.Args[2..vm.Arity];
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
            var newSubs = LanguageExtensions.Unify(rest[i], list.Contents[i]).GetOr(SubstitutionMap.Pool.Acquire());
            lambda = lambda.Substitute(newSubs);
            SubstitutionMap.Pool.Release(newSubs);
        }
        /*
        var extraArgs = rest.Length > list.Contents.Length ? rest[list.Contents.Length..] : ImmutableArray<ITerm>.Empty;
        CallInst.Compile()([lambda, .. extraArgs])(vm);
         */
        var extraArgs = rest.Length > list.Contents.Length ? rest[list.Contents.Length..] : [];
        vm.SetArg(0, lambda);
        for (int i = 0; i < extraArgs.Length; i++)
            vm.SetArg(i + 1, extraArgs[i]);
        vm.Arity = 1 + extraArgs.Length;
        CallInst.Compile()(vm);
    };
}
