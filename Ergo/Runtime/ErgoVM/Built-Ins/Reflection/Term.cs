namespace Ergo.Runtime.BuiltIns;

public sealed class Term : BuiltIn
{
    public Term()
        : base("", "term", Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var (functorArg, args, termArg) = (vm.Arg(0), vm.Arg(1), vm.Arg(2));
        var state = vm.Memory.SaveState();
        if (termArg is not Variable)
        {
            if (termArg is Dict dict)
            {
                var tag = dict.Functor.Reduce<ITerm>(a => a, v => v);
                vm.SetArg(0, functorArg);
                vm.SetArg(1, (Atom)"dict");
                ErgoVM.Goals.Unify2(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
                var newList = new List((new[] { tag }).Append(new List(dict.KeyValuePairs, default, dict.Scope)));
                vm.SetArg(0, args);
                vm.SetArg(1, newList);
                ErgoVM.Goals.Unify2(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
            }

            if (termArg is Complex complex)
            {
                vm.SetArg(0, functorArg);
                vm.SetArg(1, complex.Functor);
                ErgoVM.Goals.Unify2(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
                vm.SetArg(0, args);
                vm.SetArg(1, new List(complex.Arguments, default, complex.Scope));
                ErgoVM.Goals.Unify2(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
            }

            if (termArg is Atom atom)
            {
                vm.SetArg(0, functorArg);
                vm.SetArg(1, atom);
                ErgoVM.Goals.Unify2(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
                vm.SetArg(0, args);
                vm.SetArg(1, WellKnown.Literals.EmptyList);
                ErgoVM.Goals.Unify2(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
            }
        }
        else if (functorArg is Variable)
        {
            vm.Throw(ErgoVM.ErrorType.TermNotSufficientlyInstantiated, functorArg.Explain());
            return;
        }
        else if (functorArg is not Atom functor)
        {
            vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.Atom, functorArg.Explain());
            return;
        }
        else if (args is not List argsList || argsList.Contents.Length == 0)
        {
            if (args is not Variable && !args.Equals(WellKnown.Literals.EmptyList))
            {
                vm.Throw(ErgoVM.ErrorType.ExpectedTermOfTypeAt, WellKnown.Types.List, args.Explain());
                return;
            }
            vm.SetArg(0, termArg);
            vm.SetArg(1, functor);
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
            vm.SetArg(0, args);
            vm.SetArg(1, WellKnown.Literals.EmptyList);
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
        }
        else
        {
            vm.SetArg(0, termArg);
            vm.SetArg(1, new Complex(functor, argsList.Contents));
            ErgoVM.Goals.Unify2(vm);
            if (ReleaseAndRestoreEarlyReturn()) return;
        }
        vm.Solution();
        ReleaseAndRestore();
        void ReleaseAndRestore()
        {
            vm.Memory.LoadState(state);
        }
        bool ReleaseAndRestoreEarlyReturn()
        {
            if (vm.State == ErgoVM.VMState.Fail)
            {
                ReleaseAndRestore();
                return true;
            }
            return false;
        }
    };
}
