
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public sealed class Lambda : BuiltIn
{

    public Lambda()
        : base("", WellKnown.Functors.Lambda.First(), Maybe<int>.None, WellKnown.Modules.Lambda)
    {
    }

    private readonly Call CallInst = new();
    public override ErgoVM.Goal Compile() => args => vm =>
    {
        if (args.Length < 2)
        {
            vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.WithArity(Maybe<int>.Some(args.Length)).Explain());
            return;
        }
        var (parameters, lambda, rest) = (args[0], args[1], args[2..]);
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
            var newSubs = LanguageExtensions.Unify(rest[i], list.Contents[i]).GetOr(Substitution.Pool.Acquire());
            lambda = lambda.Substitute(newSubs);
            Substitution.Pool.Release(newSubs);
        }

        var extraArgs = rest.Length > list.Contents.Length ? rest[list.Contents.Length..] : ImmutableArray<ITerm>.Empty;
        CallInst.Compile()([lambda, .. extraArgs])(vm);
    };
}
