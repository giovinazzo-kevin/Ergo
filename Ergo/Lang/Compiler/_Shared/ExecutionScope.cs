namespace Ergo.Lang.Compiler;

public readonly record struct ExecutionScope(SubstitutionMap CurrentSubstitutions, bool IsSolution, bool IsCut)
{
    public static readonly ExecutionScope Empty = new(new(), false, false);
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
    public ExecutionScope ApplySubstitutions(SubstitutionMap subs)
    {
        return this with { CurrentSubstitutions = SubstitutionMap.MergeRef(subs, CurrentSubstitutions) };
    }
}
