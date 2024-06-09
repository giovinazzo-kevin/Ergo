using Ergo.Interpreter;
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public abstract class DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
    : BuiltIn(documentation, functor, arity, WellKnown.Modules.Prologue)
{
    protected static Maybe<Predicate> GetPredicate(ErgoVM vm, ITerm arg)
    {
        if (!Predicate.FromCanonical(arg, vm.CKB.Scope.Entry, out var pred))
        {
            vm.CKB.Scope.Throw(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Predicate, arg.Explain());
            return default;
        }

        pred = pred.Dynamic();
        if (vm.CKB.Scope.Modules.TryGetValue(pred.DeclaringModule, out var declaringModule) && declaringModule.ContainsExport(pred.Head.GetSignature()))
            pred = pred.Exported();
        return pred.Qualified()
            .WithExecutionGraph(pred.ToExecutionGraph(vm.CKB.Graph));
    }

    protected static bool Assert(ErgoVM vm, ITermAddress arg, bool z)
    {
        if (!GetPredicate(vm, arg.Deref(vm)).TryGetValue(out var pred))
            return false;
        if (!z)
        {
            vm.CKB.CompileAndAssertA(pred);
        }
        else
        {
            vm.CKB.CompileAndAssertZ(pred);
        }
        return true;
    }

    protected static bool Retract(ErgoVM vm, ITermAddress term, bool all)
    {
        if (all)
            return vm.CKB.RetractAll(term) > 0;
        else
            return vm.CKB.Retract(term).HasValue;
    }
}
