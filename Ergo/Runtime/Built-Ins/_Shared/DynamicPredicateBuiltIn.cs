﻿using Ergo.Lang.Compiler;
using Ergo.Modules;

namespace Ergo.Runtime.BuiltIns;

public abstract class DynamicPredicateBuiltIn : ErgoBuiltIn
{
    protected DynamicPredicateBuiltIn(string documentation, Atom functor, Maybe<int> arity)
        : base(documentation, functor, arity, WellKnown.Modules.Prologue)
    {
    }

    protected static Maybe<Clause> GetPredicate(ErgoVM vm, ITerm arg)
    {
        if (!Clause.FromCanonical(arg, vm.KB.Scope.Entry, out var pred))
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
                vm.Environment.AddRange(match.Substitutions);
                break;
            }

        }

        foreach (var item in toRemove)
            vm.KB.Retract(item);

        return toRemove.Count > 0;
    }
}
