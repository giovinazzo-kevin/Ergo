
using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public sealed class Term : SolverBuiltIn
{
    public Term()
        : base("", new("term"), Maybe<int>.Some(3), WellKnown.Modules.Reflection)
    {
    }

    public override ErgoVM.Goal Compile() => arguments =>
    {
        var (functorArg, args, termArg) = (arguments[0], arguments[1], arguments[2]);
        return vm =>
        {
            var env = vm.CloneEnvironment();
            if (termArg is not Variable)
            {
                if (termArg is Dict dict)
                {
                    var tag = dict.Functor.Reduce<ITerm>(a => a, v => v);
                    ErgoVM.Goals.Unify([functorArg, new Atom("dict")])(vm);
                    if (ReleaseAndRestoreEarlyReturn()) return;
                    var newList = new List((new[] { tag }).Append(new List(dict.KeyValuePairs, default, dict.Scope)));
                    ErgoVM.Goals.Unify([args, newList])(vm);
                    if (ReleaseAndRestoreEarlyReturn()) return;
                }

                if (termArg is Complex complex)
                {
                    ErgoVM.Goals.Unify([functorArg, complex.Functor])(vm);
                    if (ReleaseAndRestoreEarlyReturn()) return;
                    ErgoVM.Goals.Unify([args, new List(complex.Arguments, default, complex.Scope)])(vm);
                    if (ReleaseAndRestoreEarlyReturn()) return;
                }

                if (termArg is Atom atom)
                {
                    ErgoVM.Goals.Unify([functorArg, atom])(vm);
                    if (ReleaseAndRestoreEarlyReturn()) return;
                    ErgoVM.Goals.Unify([args, WellKnown.Literals.EmptyList])(vm);
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

                ErgoVM.Goals.Unify([termArg, functor])(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
                ErgoVM.Goals.Unify([args, WellKnown.Literals.EmptyList])(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
            }
            else
            {
                ErgoVM.Goals.Unify([termArg, new Complex(functor, argsList.Contents.ToArray())])(vm);
                if (ReleaseAndRestoreEarlyReturn()) return;
            }
            vm.Solution();
            ReleaseAndRestore();
            void ReleaseAndRestore()
            {
                if (vm.State == ErgoVM.VMState.Fail)
                {
                    ReleaseAndRestore();
                    return;
                }
                Substitution.Pool.Release(vm.Environment);
                vm.Environment = env;
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
    };
}
