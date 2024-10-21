﻿using Ergo.Lang.Compiler;
using System.Collections;
using System.Diagnostics;

namespace Ergo.Runtime.BuiltIns;

internal sealed class FunctionalBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module, Op op) 
    : ErgoBuiltIn(documentation, functor, arity, module)
{
    private readonly Op _op = op;
    public override Op Compile() => _op;
}

public abstract class ErgoBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
{
    public readonly Signature Signature = new(functor, arity, module, Maybe<Atom>.None);
    public readonly string Documentation = documentation;

    public virtual int OptimizationOrder => 0;
    public virtual bool IsDeterminate(ImmutableArray<ITerm> args) => false;

    public virtual ExecutionNode Optimize(OldBuiltInNode node) => node;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes) => nodes;

    public Clause GetStub(ImmutableArray<ITerm> arguments)
    {
        var module = Signature.Module.GetOr(WellKnown.Modules.Stdlib);
        var head = ((ITerm)new Complex(Signature.Functor, arguments)).Qualified(module);
        return new Clause(Documentation, module, head, NTuple.Empty, dynamic: false, exported: true, default);
    }
    public abstract Op Compile();
    
    public static ErgoBuiltIn MarshallDelegate(Atom module, Delegate del, Maybe<Atom> functor = default)
    {
        var handlerType = del.GetType();
        var invokeMethod = handlerType.GetMethod("Invoke");
        var parms = invokeMethod.GetParameters();
        var returnType = invokeMethod.ReturnType;
        if (!returnType.IsAssignableTo(typeof(IEnumerable)))
            throw new NotSupportedException($"delegate return type must be IEnumerable or IEnumerable<T>");
        Type[] returnTypes = [];
        int arity = parms.Length;
        var fun = functor.GetOr(new Atom(del.Method.Name.ToErgoCase()));
        if(returnType.IsGenericType)
        {
            var ienumerableType = returnType.GetGenericArguments()[0];
            returnTypes = [ienumerableType];
            if (ienumerableType.IsAssignableTo(typeof(ITuple)))
                returnTypes = ienumerableType.GetGenericArguments();
            arity = parms.Length + returnTypes.Length;
        }
        return new FunctionalBuiltIn(string.Empty, fun, arity, module, CallDelegate);

        void CallDelegate(ErgoVM vm)
        {
            Debug.Assert(vm.Arity == arity);
            var args = parms
                .Select((p, i) => TermMarshall.FromTerm(vm.Arg(i), p.ParameterType))
                .ToArray();
            var sols = del.DynamicInvoke(args) as IEnumerable;
            var enumerator = sols.GetEnumerator();
            Next(vm);
            void Next(ErgoVM vm)
            {
                if(enumerator is null || !enumerator.MoveNext())
                {
                    vm.Fail();
                    return;
                }
                vm.PushChoice(Next);
                if(enumerator.Current is ITuple tuple)
                {
                    for (int i = 0; i < tuple.Length; i++)
                    {
                        UnifyArg(tuple[i], returnTypes[i], i);
                        if (vm.State == ErgoVM.VMState.Fail)
                            return;
                    }
                }
                else if(returnType.IsGenericType)
                {
                    UnifyArg(enumerator.Current, enumerator.Current.GetType(), 0);
                }
            }

            void UnifyArg(object obj, Type type, int ofs)
            {
                var term = TermMarshall.ToTerm(obj, type);
                vm.SetArg(0, vm.Arg(parms.Length + ofs));
                vm.SetArg(1, term);
                ErgoVM.Goals.Unify2(vm);
            }
        }
    }
}
