using Ergo.Modules;

namespace Ergo.Lang.Compiler;

public class LegacyExecutionGraph
{
    private Maybe<Op> Compiled;
    public readonly ExecutionNode Root;
    public readonly ITerm Head;

    public LegacyExecutionGraph(ITerm head, ExecutionNode root)
    {
        if (root is SequenceNode seq)
            root = seq.AsRoot(); // Enables some optimizations
        Root = root;
        head.GetQualification(out Head);
    }
    private Op CompileAndCache()
    {
        Root.Analyze(); // Do static analysis on the optimized graph before compiling
        if (Root is not SequenceNode)
            Root.IsContinuationDet = true;
        var compiledRoot = Root.Compile();
        //Debug.WriteLine(Root.Explain(false));
        // NOTE: PrepareDelegate pre-JITs 'op' so that we don't incur JIT overhead at runtime.
        RuntimeHelpers.PrepareDelegate(compiledRoot);
        Compiled = compiledRoot;
        return compiledRoot;
    }
    public LegacyExecutionGraph Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        if (Root.IsGround)
            return this;
        vars ??= [];
        return new(Head.Instantiate(ctx, vars), Root.Instantiate(ctx, vars))
        { Compiled = Compiled };
    }
    public LegacyExecutionGraph Substitute(IEnumerable<Substitution> s)
    {
        if (Root.IsGround)
            return this;
        return new(Head.Substitute(s), Root.Substitute(s))
        { Compiled = Compiled };
    }

    public LegacyExecutionGraph Optimized() => new(Head, new SequenceNode([Root]).Optimize());

    /// <summary>
    /// Compiles the current graph to a Goal that can run on the ErgoVM.
    /// Caches the result, since ExecutionGraphs are immutable by design and stored by Predicates.
    /// </summary>
    public Op Compile()
    {
        return Compiled.GetOr(CompileAndCache());
    }
}

public static class ExecutionGraphExtensions
{
    public static readonly InstantiationContext CompilerContext = new("E");

    public static LegacyExecutionGraph ToExecutionGraph(this Clause clause, LegacyDependencyGraph graph, Dictionary<Signature, CyclicalCallNode> cyclicalCallMap = null)
    {
        var root = ToExecutionNode(clause.Body, graph, graph.KnowledgeBase.Scope, clause.DeclaringModule, cyclicalCallMap: cyclicalCallMap);
        return new(clause.Head, root);
    }

    public static ExecutionNode ToExecutionNode(this ITerm goal, LegacyDependencyGraph graph, Maybe<InterpreterScope> mbScope = default, Maybe<Atom> callerModule = default, InstantiationContext ctx = null, Dictionary<Signature, CyclicalCallNode> cyclicalCallMap = null)
    {
        return null;
    }
}