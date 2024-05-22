using Ergo.Lang.Parser;

namespace Ergo.Lang.Compiler;

public class ListCompiler : IAbstractTermCompiler<List>
{
    private static readonly ListParser Parser = new();
    public List Dereference(TermMemory vm, ITermAddress address)
    {
        if (address is ConstAddress) return List.Empty;
        if (address is StructureAddress sAddr)
        {
            var canonical = (Complex)vm.Dereference(sAddr);
            return Parser.FromCanonical(canonical).GetOrThrow();
        }
        throw new NotSupportedException();
    }
    public ITermAddress Store(TermMemory vm, List term)
    {
        return vm.StoreTerm(term.CanonicalForm);
    }
}
