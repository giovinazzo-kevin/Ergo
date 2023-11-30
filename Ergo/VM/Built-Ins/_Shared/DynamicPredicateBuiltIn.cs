using Ergo.Interpreter;
using Ergo.Lang.Compiler;

namespace Ergo.VM.BuiltIns;

public abstract class DynamicPredicateBuiltIn : BuiltIn
{
    protected DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, WellKnown.Modules.Prologue)
    {
    }

    protected static Maybe<Predicate> GetPredicate(ErgoVM vm, ITerm arg)
    {
        if (!Predicate.FromCanonical(arg, vm.KnowledgeBase.Scope.Entry, out var pred))
        {
            vm.KnowledgeBase.Scope.Throw(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Predicate, arg.Explain());
            return default;
        }

        pred = pred.Dynamic();
        if (vm.KnowledgeBase.Scope.Modules.TryGetValue(pred.DeclaringModule, out var declaringModule) && declaringModule.ContainsExport(pred.Head.GetSignature()))
            pred = pred.Exported();
        return pred.Qualified()
            .WithExecutionGraph(pred.ToExecutionGraph(vm.KnowledgeBase.DependencyGraph));
    }

    protected static bool Assert(ErgoVM vm, ITerm arg, bool z)
    {
        if (!GetPredicate(vm, arg).TryGetValue(out var pred))
            return false;
        if (!z)
        {
            vm.KnowledgeBase.AssertA(pred);
        }
        else
        {
            vm.KnowledgeBase.AssertZ(pred);
        }
        return true;
    }

    protected static bool Retract(ErgoVM vm, ITerm term, bool all)
    {
        var sig = term.GetSignature();
        if (!term.IsQualified)
            term = term.Qualified(vm.KnowledgeBase.Scope.Entry);
        var toRemove = new List<ITerm>();
        foreach (var match in vm.KnowledgeBase.GetMatches(new("R"), term, desugar: true)
            .AsEnumerable().SelectMany(x => x))
        {
            if (!match.Predicate.IsDynamic)
            {
                vm.Throw(ErgoVM.ErrorType.CannotRetractStaticPredicate, sig.Explain());
                return false;
            }

            if (vm.KnowledgeBase.Scope.Entry != match.Predicate.DeclaringModule)
            {
                vm.Throw(ErgoVM.ErrorType.CannotRetractImportedPredicate, sig.Explain(), vm.KnowledgeBase.Scope.Entry.Explain(), match.Predicate.DeclaringModule.Explain());
                return false;
            }

            toRemove.Add(match.Predicate.Head);

            if (!all)
                break;

        }

        foreach (var item in toRemove)
            vm.KnowledgeBase.Retract(item);

        return toRemove.Count > 0;
    }
}
