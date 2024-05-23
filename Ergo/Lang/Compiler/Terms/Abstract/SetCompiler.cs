using Ergo.Lang.Parser;

namespace Ergo.Lang.Compiler;

public class SetCompiler : IAbstractTermCompiler<Set>
{
    private static readonly SetParser Parser = new();
    public Set Dereference(TermMemory vm, ITermAddress address)
    {
        if (address is ConstAddress) return Set.Empty;
        if (address is StructureAddress sAddr)
        {
            var canonical = (Complex)vm.Dereference(sAddr);
            return Parser.FromCanonical(canonical).GetOrThrow();
        }
        throw new NotSupportedException();
    }
    public ITermAddress Store(TermMemory vm, Set term)
    {
        return vm.StoreTerm(term.CanonicalForm);
    }
    public bool Unify(TermMemory mem, AbstractAddress a1, ITermAddress other)
    {
        return mem.Unify(mem[a1].Address, other, transaction: false);
    }
}
