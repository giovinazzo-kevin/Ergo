﻿using Ergo.Lang.Compiler;

namespace Ergo.Solver.BuiltIns;

public abstract class SolverBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
{
    public readonly Signature Signature = new(functor, arity, module, Maybe<Atom>.None);
    public readonly string Documentation = documentation;

    public virtual int OptimizationOrder => 0;

    public virtual ExecutionNode Optimize(BuiltInNode node) => node;
    public virtual List<ExecutionNode> OptimizeSequence(List<ExecutionNode> nodes) => nodes;

    public Predicate GetStub(ImmutableArray<ITerm> arguments)
    {
        var module = Signature.Module.GetOr(WellKnown.Modules.Stdlib);
        var head = ((ITerm)new Complex(Signature.Functor, arguments)).Qualified(module);
        return new Predicate(Documentation, module, head, NTuple.Empty, dynamic: false, exported: true, default);
    }
    public abstract ErgoVM.Goal Compile();
}
