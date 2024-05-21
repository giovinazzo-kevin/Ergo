using System.Reflection;
using Expression = System.Linq.Expressions.Expression;
namespace Ergo.Interpreter.Libraries;


public class DisposableHook(Signature sig) : Hook(sig), IDisposable
{
    internal Action<DisposableHook> DisposeAction { get; set; }

    public void Dispose() => DisposeAction(this);
}

public class Hook
{
    private static readonly InstantiationContext ctx = new("_H");
    private readonly ITerm[] args;
    public readonly Signature Signature;
    private Maybe<ErgoVM.Op> cached = default;
    private ITerm head = default;
    public Hook(Signature sig)
    {
        Signature = sig;
        args = new ITerm[Signature.Arity.GetOr(ErgoVM.MAX_ARGUMENTS)];
        for (int i = 0; i < args.Length; i++)
            args[i] = ctx.GetFreeVariable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArg(int i, ITerm arg)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, args.Length);
        args[i] = arg;
    }

    public IEnumerable<Solution> CallInteractive(ErgoVM vm, params object[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is ITerm term)
                SetArg(i, term);
            else if (args[i] is { } obj)
                SetArg(i, TermMarshall.ToTerm(obj, obj.GetType()));
        }
        vm.Query = Compile();
        return vm.RunInteractive();
    }
    public void Call(ErgoVM vm, params object[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is ITerm term)
                SetArg(i, term);
            else if (args[i] is { } obj)
                SetArg(i, TermMarshall.ToTerm(obj, obj.GetType()));
        }
        vm.Query = Compile();
        vm.Run();
    }


    /// <summary>
    /// Creates a hook that automagically binds to a C# event until it is disposed.
    /// </summary>
    /// <param name="evt">The Event to marshall. It can be any C# event as long as its parameters can be marshalled and as long as it doesn't return anything.</param>
    /// <param name="target">The target of the event handler.</param>
    /// <param name="functor">The functor to use for this hook.</param>
    /// <param name="module">The module in which this hook is defined.</param>
    /// <returns>A wrapper function that produces a disposable hook that binds to the given vm and unbinds once disposed.</returns>
    public static Func<ErgoVM, DisposableHook> MarshallEvent(EventInfo evt, object target, Atom functor, Maybe<Atom> module = default) => vm =>
    {
        var handlerType = evt.EventHandlerType;
        var invokeMethod = handlerType.GetMethod("Invoke");
        var parms = invokeMethod.GetParameters()
            .Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();
        var returnType = invokeMethod.ReturnType;

        var hook = new DisposableHook(new(functor, parms.Length, module, default));
        var hookInvokeMethod = typeof(DisposableHook).GetMethod(nameof(Hook.Call), BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
        var hookInvokeCall = Expression.Call(
            Expression.Constant(hook),
            hookInvokeMethod,
            Expression.Constant(vm),
            Expression.NewArrayInit(
                typeof(object),
                parms.Select(p => Expression.Convert(p, typeof(object)))
            )
        );

        if (returnType != typeof(void))
            throw new NotSupportedException("Marshalling of Events where the return type is not void is not supported.");

        var lambda = Expression.Lambda(handlerType, hookInvokeCall, parms);
        Delegate handler = lambda.Compile();

        evt.AddEventHandler(target, handler);
        hook.DisposeAction = _ => evt.RemoveEventHandler(target, handler);
        return hook;
    };

    /// <summary>
    /// Creates a patched delegate that calls a hook automatically whenever it is invoked.
    /// </summary>
    /// <param name="del">The delegate to marshall. It can be any C# delegate as long as its parameters can be marshalled.</param>
    /// <param name="functor">The functor to use for this hook.</param>
    /// <param name="module">The module in which this hook is defined.</param>
    /// <returns>A wrapper function that produces a delegate calls the specified hook when it is invoked.</returns>
    public static Func<ErgoVM, T> MarshallDelegate<T>(T del, Atom functor, Maybe<Atom> module = default, bool insertAfterCall = true)
    where T : Delegate
        => vm =>
        {
            var handlerType = del.GetType();
            var invokeMethod = handlerType.GetMethod("Invoke");
            var parms = invokeMethod.GetParameters()
                .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                .ToArray();
            var returnType = invokeMethod.ReturnType;

            var hook = new Hook(new(functor, parms.Length, module, default));
            var hookInvokeMethod = typeof(Hook).GetMethod(nameof(Hook.Call), BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
            var hookInvokeCall = Expression.Call(
                Expression.Constant(hook),
                hookInvokeMethod,
                Expression.Constant(vm),
                Expression.NewArrayInit(
                    typeof(object),
                    parms.Select(p => Expression.Convert(p, typeof(object)))
                )
            );

            Expression combinedBody;

            if (returnType == typeof(void))
            {
                var delegateInvokeCall = Expression.Invoke(Expression.Constant(del), parms);
                combinedBody = insertAfterCall
                    ? Expression.Block(delegateInvokeCall, hookInvokeCall)
                    : Expression.Block(hookInvokeCall, delegateInvokeCall);
            }
            else
            {
                var delegateInvokeCall = Expression.Invoke(Expression.Constant(del), parms);
                var resultVariable = Expression.Variable(returnType, "result");
                var assignResult = Expression.Assign(resultVariable, delegateInvokeCall);

                combinedBody = insertAfterCall
                    ? Expression.Block(
                        new[] { resultVariable },
                        assignResult,
                        hookInvokeCall,
                        resultVariable
                      )
                    : Expression.Block(
                        new[] { resultVariable },
                        hookInvokeCall,
                        assignResult,
                        resultVariable
                      );
            }

            var lambda = Expression.Lambda(handlerType, combinedBody, parms);
            Delegate combinedHandler = lambda.Compile();
            return (T)combinedHandler;
        };

    /// <summary>
    /// Creates a wrapper that compiles the hook just in time when it is first called.
    /// </summary>
    public ErgoVM.Op Compile(bool throwIfNotDefined = true)
    {
        return vm =>
        {
            if (cached.TryGetValue(out var op))
            {
                op(vm);
                return;
            }
            // Compile and cache the hook the first time it's called
            // TODO: Invalidate cache when any predicate matching this hook is asserted or retracted
            if (!vm.KB.Get(Signature).TryGetValue(out var preds))
            {
                if (throwIfNotDefined)
                    vm.Throw(ErgoVM.ErrorType.UndefinedPredicate, Signature.Explain());
                else
                    vm.Fail();
                return;
            }
            var ops = new ErgoVM.Op[preds.Count];
            for (int i = 0; i < preds.Count; i++)
            {
                var predHead = preds[i].Unqualified().Head;
                if (!preds[i].ExecutionGraph.TryGetValue(out var graph))
                {
                    return;
                }
                var gOp = graph.Compile();
                ops[i] = vm =>
                {
                    vm.SetArg(0, head);
                    vm.SetArg(1, predHead);
                    ErgoVM.Goals.Unify2(vm);
                    if (vm.State == ErgoVM.VMState.Fail)
                        return;
                    gOp(vm);
                };
            }
            var branch = ErgoVM.Ops.Or(ops);
            cached = op = vm =>
            {
                head = args.Length > 0 ? new Complex(Signature.Functor, args) : Signature.Functor;
                branch(vm);
            };
            op(vm);
        };
    }

}