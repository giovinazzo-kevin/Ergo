using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Compiler;

public interface IAbstractTermCompiler
{
    ITermAddress Store(TermMemory vm, AbstractTerm term);
    AbstractTerm Dereference(TermMemory vm, ITermAddress address);
    bool Unify(TermMemory mem, AbstractAddress address, ITermAddress other);
}

public interface IAbstractTermCompiler<T> : IAbstractTermCompiler
    where T : AbstractTerm
{
    ITermAddress Store(TermMemory vm, T term);
    new T Dereference(TermMemory vm, ITermAddress address);

    ITermAddress IAbstractTermCompiler.Store(TermMemory vm, AbstractTerm term)
        => Store(vm, (T)term);
    AbstractTerm IAbstractTermCompiler.Dereference(TermMemory vm, ITermAddress addr)
        => Dereference(vm, addr);
}

