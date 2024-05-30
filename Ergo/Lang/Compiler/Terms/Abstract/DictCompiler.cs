namespace Ergo.Lang.Compiler;

public class DictCompiler : IAbstractTermCompiler<Dict>
{
    public Dict Dereference(TermMemory vm, ITermAddress address)
    {
        if (address is not StructureAddress sAddr)
            throw new NotSupportedException();
        var args = vm[sAddr];
        if (args.Length != 3)
            throw new NotSupportedException();
        var functor = vm.Dereference(args[1]);
        var arg = vm.Dereference(args[2]);
        return (functor, arg) switch
        {
            (Atom a, Variable v) => new Dict(a, v),
            (Atom a, Set s) => FromSet(a, s),
            (Variable v, Set s) => FromSet(v, s),
            _ => throw new NotSupportedException()
        };

        Dict FromSet(Either<Atom, Variable> f, Set s)
        {
            return new Dict(f, s.Contents.Select(Unfold));
            KeyValuePair<Atom, ITerm> Unfold(ITerm term)
            {
                var args = term.GetArguments();
                return new((Atom)args[0], args[1]);
            }
        }
    }
    private static readonly Atom functor_dict = "dict";
    public ITermAddress Store(TermMemory vm, Dict term)
    {
        var args = new List<ITermAddress>();
        if (term.Functor.TryGetB(out var varFunctor))
            args.Add(vm.StoreVariable(varFunctor.Name));
        else if (term.Functor.TryGetA(out var atomFunctor))
            args.Add(vm.StoreAtom(atomFunctor));
        else throw new NotSupportedException();
        if (term.Argument.TryGetA(out var varArg))
            args.Add(vm.StoreVariable(varArg.Name));
        else if (term.Argument.TryGetB(out var varSet))
            args.Add(vm.StoreAbstract(varSet));
        else throw new NotSupportedException();
        return vm.StoreStructure(args.Prepend(vm.StoreAtom(functor_dict)).ToArray());
    }
    public bool Unify(TermMemory mem, AbstractAddress a1, ITermAddress other)
    {
        return mem.Unify(mem[a1].Address, other, transaction: false);
    }
}
