using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Expression = System.Linq.Expressions.Expression;
namespace Ergo.Interpreter.Libraries;


public class DisposableHook : CompiledHook, IDisposable
{
    internal DisposableHook(Signature sig) : base(sig) { }

    internal Action<DisposableHook> DisposeAction { get; set; }

    public void Dispose() => DisposeAction(this);
}

public class CompiledHook
{
    private static readonly InstantiationContext CTX = new("_H");

    public readonly Signature Signature;
    public ITerm[] Args { get; internal set; }
    public ErgoVM.Op Op { get; internal set; }

    public CompiledHook(Signature sig)
    {
        Signature = sig;
        Args = new ITerm[sig.Arity.GetOr(ErgoVM.MAX_ARGUMENTS)];
        for (int i = 0; i < Args.Length; i++)
            Args[i] = CTX.GetFreeVariable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetArg(int i, ITerm arg)
    {
        Args[i] = arg;
    }

    public void SetArgs(ErgoVM vm, params object[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is ITerm term)
                SetArg(i, term);
            else if (args[i] is { } obj)
                SetArg(i, TermMarshall.ToTerm(obj, obj.GetType()));
        }
    }

    public IEnumerable<Solution> CallInteractive(ErgoVM vm, params object[] args)
    {
        SetArgs(vm, args);
        vm.Query = Op;
        return vm.RunInteractive();
    }
    public void Call(ErgoVM vm, params object[] args)
    {
        SetArgs(vm, args);
        vm.Query = Op;
        vm.Run();
    }
}

public class Hook
{
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
        var hookInvokeMethod = typeof(DisposableHook).GetMethod(nameof(DisposableHook.Call), BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
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

            var sig = new Signature(functor, parms.Length, module, default);
            var hook = Compile(sig, vm.KB);

            var hookInvokeMethod = typeof(CompiledHook).GetMethod(nameof(CompiledHook.Call), BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
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
    public static CompiledHook CompileJIT(Signature sig, bool throwIfNotDefined = true)
    {
        var lazyHook = new CompiledHook(sig);
        ErgoVM.Op cached = default;
        lazyHook.Op = vm =>
        {
            if (cached is { } op)
            {
                op(vm);
                return;
            }
            var compiledHook = Compile(sig, vm.KB);
            compiledHook.Args = lazyHook.Args;
            (cached = compiledHook.Op)(vm);
        };
        return lazyHook;
    }

    public static CompiledHook Compile(Signature sig, KnowledgeBase kb)
    {
        var compiledHook = new CompiledHook(sig);
        compiledHook.Op = CompileOp(compiledHook, sig, kb);
        return compiledHook;
    }

    static ErgoVM.Op CompileOp(CompiledHook compiledHook, Signature sig, KnowledgeBase kb)
    {
        sig = sig.WithModule(sig.Module.GetOr(kb.Scope.Entry));
        if (!kb.Get(sig).TryGetValue(out var clauses))
        {
            kb.Scope.ExceptionHandler.Throw(new RuntimeException(ErgoVM.ErrorType.UndefinedPredicate, sig.Explain()));
            return default;
        }
        var ops = new ErgoVM.Op[clauses.Count];
        for (int i = 0; i < clauses.Count; i++)
        {
            var predHead = clauses[i].Unqualified().Head;
            if (!clauses[i].ExecutionGraph.TryGetValue(out var graph))
                return default;
            var gOp = graph.Compile();
            ops[i] = vm => {
                vm.SetArg(0, compiledHook.Args.Length > 0 ? new Complex(sig.Functor, compiledHook.Args) : sig.Functor);
                vm.SetArg(1, predHead);
                ErgoVM.Goals.Unify2(vm);
                if (vm.State == ErgoVM.VMState.Fail)
                    return;
                gOp(vm);
            };
        }
        return ErgoVM.Ops.Or(ops);
    }

}