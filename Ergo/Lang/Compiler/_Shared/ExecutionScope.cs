using System.Diagnostics;

namespace Ergo.Lang.Compiler;

public readonly record struct ExecutionScope(Stopwatch Stopwatch, ImmutableStack<(TimeSpan Time, Maybe<ExecutionNode> Node)> Callers, SubstitutionMap CurrentSubstitutions, bool IsSolution, bool IsCut, bool IsBranch)
{
    public static ExecutionScope Empty() => new(new(), ImmutableStack<(TimeSpan Time, Maybe<ExecutionNode> Node)>.Empty, new(), false, false, false);
    public ExecutionScope Now(ExecutionNode caller)
    {
#if ERGO_COMPILER_DIAGNOSTICS
        return this with { Callers = Callers.Push((Stopwatch.Elapsed, caller)) };
#endif
        return this;
    }
    public ExecutionScope AsSolution(bool isSol = true)
    {
        return this with { IsSolution = isSol };
    }
    public ExecutionScope ChoicePoint()
    {
        return this with { IsCut = false };
    }
    public ExecutionScope Cut()
    {
        return this with { IsCut = true };
    }
    public ExecutionScope Branch(bool branch = true)
    {
        return this with { IsBranch = branch };
    }
    public ExecutionScope ApplySubstitutions(SubstitutionMap subs)
    {
        return this with { CurrentSubstitutions = SubstitutionMap.MergeRef(subs, CurrentSubstitutions) };
    }
}
