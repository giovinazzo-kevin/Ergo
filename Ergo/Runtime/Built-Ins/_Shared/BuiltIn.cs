using Ergo.Lang.Compiler;
using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Ergo.Runtime.BuiltIns;

internal sealed class FunctionalBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module, ErgoVM.Op op) 
    : BuiltIn(documentation, functor, arity, module)
{
    private readonly ErgoVM.Op _op = op;
    public override ErgoVM.Op Compile() => _op;
}

public abstract class BuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
{
    public readonly Signature Signature = new(functor, arity, module, Maybe<Atom>.None);
    public readonly string Documentation = documentation;

    public virtual int OptimizationOrder => 0;
    public virtual bool IsDeterminate(ImmutableArray<ITerm> args) => false;

    public virtual ExecutionNode Optimize(BuiltInNode node) => node;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes) => nodes;

    public Predicate GetStub(ImmutableArray<ITerm> arguments)
    {
        var module = Signature.Module.GetOr(WellKnown.Modules.Stdlib);
        var head = ((ITerm)new Complex(Signature.Functor, arguments)).Qualified(module);
        return new Predicate(Documentation, module, head, NTuple.Empty, dynamic: false, exported: true, default);
    }
    public abstract ErgoVM.Op Compile();
    
    // del has the shape: IEnumerable
    public static BuiltIn MarshallDelegate(Atom module, Delegate del, Maybe<Atom> functor = default)
    {
        var handlerType = del.GetType();
        var invokeMethod = handlerType.GetMethod("Invoke");
        var parms = invokeMethod.GetParameters();
        var returnType = invokeMethod.ReturnType;
        if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
            throw new NotSupportedException($"delegate return type must be IEnumerable<T>");
        var ienumerableType = returnType.GetGenericArguments()[0];
        Type[] returnTypes = [ienumerableType];
        if (ienumerableType.IsAssignableTo(typeof(ITuple)))
            returnTypes = ienumerableType.GetGenericArguments();
        var arity = parms.Length + returnTypes.Length;
        var fun = functor.GetOr(new Atom(del.Method.Name.ToErgoCase()));
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
                else
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
