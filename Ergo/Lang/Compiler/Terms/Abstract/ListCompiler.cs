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
            var ret = Parser.FromCanonical(canonical).GetOrThrow();
            return ret;
        }
        throw new NotSupportedException();
    }
    public ITermAddress Store(TermMemory vm, List term)
    {
        return vm.StoreTerm(term.CanonicalForm);
    }
    public bool Unify(TermMemory mem, AbstractAddress a1, ITermAddress other)
    {
        var args = mem[a1].Address;
        return (args, other) switch
        {
            (StructureAddress sa, StructureAddress sb) => UnifySubstructure(sa, sb),
            _ => mem.Unify(args, other, transaction: false)
        };

        bool UnifyVariableWithTail(VariableAddress v, ITermAddress tail)
        {
            if (tail is VariableAddress _)
                return mem.Unify(v, tail, transaction: false);
            if (tail is StructureAddress || tail is ConstAddress)
            {
                // The tail is still a canonical list, so we can create an abstract pointer to it!
                // The associated compiler will know how to dereference the sublist accordingly. Neat.
                var subList = mem.StoreAbstract(tail, mem[a1].Compiler);
                return mem.Unify(v, subList, transaction: false);
            }
            return false;
        }

        bool UnifySubstructure(StructureAddress sa, StructureAddress sb)
        {
            var (a, b) = (mem[sa], mem[sb]);
            if (a.Length != 3 || b.Length != 3)
                return false;
            // first arg should be the list constructor ('[|]')
            // second arg should be the current element, and third arg
            // should be the tail of the list, so either of:
            // - Another list (so [|]/3)
            // - An empty list literal ([])
            // - A variable 
            // NOTE: The first two may be expressed as:
            // - Plain Const/Struct pointers
            // - Abstract list pointers to the former
            if (!mem.Unify(a[1], b[1], transaction: false))
                return false;
            return (a[2], b[2]) switch
            {
                (VariableAddress va1, _) => UnifyVariableWithTail(va1, b[2]),
                (_, VariableAddress vb1) => UnifyVariableWithTail(vb1, a[2]),
                (ConstAddress ca1, ConstAddress cb1) => mem.Unify(ca1, cb1, transaction: false),
                (StructureAddress sa1, StructureAddress sb1) => UnifySubstructure(sa1, sb1),
                (_, AbstractAddress ab1) => Unify(mem, ab1, a[2]),
                (AbstractAddress aa1, _) => Unify(mem, aa1, b[2]),
                _ => false
            };
        }
    }
    public ITermAddress[] GetArgs(TermMemory mem, ITermAddress a) => a switch
    {
        ConstAddress => [mem.StoreTerm(WellKnown.Literals.EmptyList)],
        StructureAddress s => mem[s],
        _ => throw new NotSupportedException()
    };
    public Signature GetSignature(TermMemory mem, AbstractAddress a) =>
        new(WellKnown.Operators.List.CanonicalFunctor, GetArgs(mem, a).Length, default, default);
}
