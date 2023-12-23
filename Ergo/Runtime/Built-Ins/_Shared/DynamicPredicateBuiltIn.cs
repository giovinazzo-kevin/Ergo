using Ergo.Interpreter;
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public abstract class DynamicPredicateBuiltIn : BuiltIn
{
    protected DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, WellKnown.Modules.Prologue)
    {
    }

    protected static Maybe<Predicate> GetPredicate(ErgoVM vm, ITerm arg)
    {
        if (!Predicate.FromCanonical(arg, vm.KB.Scope.Entry, out var pred))
        {
            vm.KB.Scope.Throw(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Predicate, arg.Explain());
            return default;
        }

        pred = pred.Dynamic();
        if (vm.KB.Scope.Modules.TryGetValue(pred.DeclaringModule, out var declaringModule) && declaringModule.ContainsExport(pred.Head.GetSignature()))
            pred = pred.Exported();
        return pred.Qualified()
            .WithExecutionGraph(pred.ToExecutionGraph(vm.KB.DependencyGraph));
    }

    protected static bool Assert(ErgoVM vm, ITerm arg, bool z)
    {
        if (!GetPredicate(vm, arg).TryGetValue(out var pred))
            return false;
        if (!z)
        {
            vm.KB.AssertA(pred);
        }
        else
        {
            vm.KB.AssertZ(pred);
        }
        return true;
    }

    protected static bool Retract(ErgoVM vm, ITerm term, bool all)
    {
        var sig = term.GetSignature();
        if (!term.IsQualified)
            term = term.Qualified(vm.KB.Scope.Entry);
        var any = false;
        foreach (var match in vm.KB.Retract(term, isItRuntime: true))
        {
            if (!match.IsDynamic)
            {
                vm.Throw(ErgoVM.ErrorType.CannotRetractStaticPredicate, sig.Explain());
                return false;
            }

            if (vm.KB.Scope.Entry != match.DeclaringModule)
            {
                vm.Throw(ErgoVM.ErrorType.CannotRetractImportedPredicate, sig.Explain(), vm.KB.Scope.Entry.Explain(), match.DeclaringModule.Explain());
                return false;
            }
            any = true;
            if (!all)
                break;

        }
        return any;
    }
}
