using Ergo.Lang.Parser;

namespace Ergo.Lang.Compiler;

public class NTupleCompiler : IAbstractTermCompiler<NTuple>
{
    private static readonly NTupleParser Parser = new();
    public NTuple Dereference(TermMemory vm, ITermAddress address)
    {
        if (address is AtomAddress) return NTuple.Empty;
        if (address is StructureAddress sAddr)
        {
            var canonical = (Complex)vm.Dereference(sAddr);
            return Parser.FromCanonical(canonical).GetOrThrow();
        }
        throw new NotSupportedException();
    }
    public ITermAddress Store(TermMemory vm, NTuple term)
    {
        return vm.StoreTerm(term.CanonicalForm);
    }
    public bool Unify(TermMemory mem, AbstractAddress a1, ITermAddress other)
    {
        return mem.Unify(mem[a1].Address, other, transaction: false);
    }
    public ITermAddress[] GetArgs(TermMemory mem, ITermAddress a) => a switch
    {
        AtomAddress => [mem.StoreTerm(WellKnown.Literals.EmptyCommaList)],
        StructureAddress s => mem[s],
        _ => throw new NotSupportedException()
    };
    public Signature GetSignature(TermMemory mem, AbstractAddress a) =>
        new(WellKnown.Operators.Conjunction.CanonicalFunctor, GetArgs(mem, a).Length, default, default);
}