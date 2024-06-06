using Ergo.Interpreter;
using Ergo.Lang.Compiler;

namespace Ergo.Runtime.BuiltIns;

public abstract class DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
    : BuiltIn(documentation, functor, arity, WellKnown.Modules.Prologue)
{
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
        var toRemove = new List<ITerm>();
        foreach (var match in vm.KB.GetMatches(new("R"), term, desugar: true)
            .AsEnumerable().SelectMany(x => x))
        {
            if (!match.Predicate.IsDynamic)
            {
                vm.Throw(ErgoVM.ErrorType.CannotRetractStaticPredicate, sig.Explain());
                return false;
            }

            //if (vm.KB.Scope.Entry != match.Predicate.DeclaringModule)
            //{
            //    vm.Throw(ErgoVM.ErrorType.CannotRetractImportedPredicate, sig.Explain(), vm.KB.Scope.Entry.Explain(), match.Predicate.DeclaringModule.Explain());
            //    return false;
            //}

            toRemove.Add(match.Predicate.Head);

            if (!all)
            {
                // TODO: Use TermMemory in KnowledgeBase!!
                foreach (var sub in match.Substitutions)
                {
                    var rhs = vm.Memory.StoreTerm(sub.Rhs);
                    var lhs = vm.Memory.StoreVariable(((Variable)sub.Lhs).Name);
                    vm.Memory[lhs] = rhs;
                }

                break;
            }

        }

        foreach (var item in toRemove)
            vm.KB.Retract(item);

        return toRemove.Count > 0;
    }
}
