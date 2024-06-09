using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Compiler;

namespace Ergo.Lang.Extensions;

public static class TermMemoryExtensions
{
    public static ITerm Deref(this ITermAddress a, TermMemory memory)
        => memory.Dereference(a);
    public static ITerm Deref(this ITermAddress a, ErgoVM vm)
        => vm.Memory.Dereference(a);

    public static ITerm Dereference(this TermMemory vm, ITermAddress addr)
    {
        return addr switch
        {
            ConstAddress c => vm[c],
            VariableAddress v => DereferenceVariable(vm, v),
            StructureAddress s => DereferenceStruct(vm, s),
            AbstractAddress a when vm[a] is { } cell => cell.Compiler.Dereference(vm, cell.Address),
            _ => throw new NotSupportedException()
        };

        static ITerm DereferenceVariable(TermMemory mem, VariableAddress v)
        {
            var addr = mem[v];
            if (addr.Equals(v))
            {
                if (mem.InverseVariableLookup.TryGetValue(v, out var name))
                    return (Variable)name;
                return (Variable)$"__V{v.Index}";
            }
            return Dereference(mem, addr);
        }

        static Complex DereferenceStruct(TermMemory vm, StructureAddress s)
        {
            var args = vm[s];
            if (args.Length == 0)
                throw new InvalidOperationException();
            var addr_functor = args[0];
            if (addr_functor is not ConstAddress const_functor)
                throw new InvalidOperationException();
            var functor = vm[const_functor];
            var rest = args[1..]
                .Select(a => Dereference(vm, a))
                .ToArray();
            return new Complex(functor, rest);
        }
    }

    public static ITermAddress[] GetArgs(this ITermAddress a, TermMemory mem)
    {
        if (a is VariableAddress v && mem.IsVariableAssigned(v)) return mem[v].GetArgs(mem);
        if (a is StructureAddress s) return mem[s];
        if (a is AbstractAddress b) return mem[b].Compiler.GetArgs(mem, mem[b].Address);
        return [a];
    }


    public static Signature GetSignature(this ITermAddress ta, TermMemory mem) => ta switch
    {
        ConstAddress a => ForConst(mem, a),
        VariableAddress a => ForVariable(mem, a),
        StructureAddress a when mem[a][0] is ConstAddress => ForStructure(mem, a),
        AbstractAddress a => ForAbstract(mem, a),
        PredicateAddress a => mem[a].Head.GetSignature(mem),
        _ => throw new NotSupportedException()
    };

    static Signature ForConst(TermMemory mem, ConstAddress a) => new(mem[a], 1, default, default);
    static Signature ForVariable(TermMemory mem, VariableAddress a)
    {
        var reference = mem[a];
        if (reference is VariableAddress vr && !mem.IsVariableAssigned(vr))
            return new Signature(new Atom(mem.InverseVariableLookup[a]), default, default, default);
        return reference.GetSignature(mem);
    }

    static Signature ForStructure(TermMemory mem, StructureAddress a)
    {
        var args = mem[a];
        var functor = mem[(ConstAddress)args[0]];
        if (args.Length == 3
            && args[1] is ConstAddress c
            && WellKnown.Functors.Module.Contains(functor))
            return ForQualified(mem, functor, args);
        return new Signature(functor, args.Length - 1, default, default);
    }

    static Signature ForQualified(TermMemory mem, Atom functor, ITermAddress[] args)
    {
        var module = mem[(ConstAddress)args[1]];
        var sig = args[2].GetSignature(mem);
        return sig.WithModule(module);
    }

    static Signature ForAbstract(TermMemory mem, AbstractAddress a) => mem[a].Compiler.GetSignature(mem, a);

    public static ITermAddress StoreTerm(this TermMemory vm, ITerm term)
    {
        var hash = term.GetHashCode();
        if (vm.TermLookup.TryGetValue(hash, out var c))
            return c;
        if (term.GetFunctor().TryGetValue(out var functor))
        {
            var args = term.GetArguments();
            var addr_functor = vm.StoreAtom(functor);
            if (args.Length > 0)
            {
                var addr_args = args.Select(vm.StoreTerm)
                    .Prepend(addr_functor)
                    .ToArray();
                return vm.StoreStructure(addr_args);
            }
            return addr_functor;
        }
        else if (term is Variable { Name: var name })
            return vm.StoreVariable(name);
        else if (term is AbstractTerm abs)
            return vm.StoreAbstract(abs);
        throw new NotSupportedException();
    }

    public static bool Unify(this TermMemory mem, ITermAddress a, ITermAddress b, bool transaction = true)
    {
        if (!transaction)
            return UnifyTerm(mem, a, b);
        var state = mem.SaveState();
        var ret = UnifyTerm(mem, a, b);
        if (!ret)
            mem.LoadState(state);
        return ret;

        static ITermAddress DerefVar(TermMemory mem, VariableAddress va)
        {
            ITermAddress derefA = va;
            while (derefA is VariableAddress va1)
            {
                derefA = mem[va1];
                if (va1.Index == derefA.Index) break;
            }
            return derefA;
        }

        static bool UnifyTerm(TermMemory mem, ITermAddress a, ITermAddress b)
        {
            return (a, b) switch
            {
                (ConstAddress ca, ConstAddress cb) => UnifyConst(mem, ca, cb),
                (StructureAddress va, StructureAddress vb) => UnifyStruct(mem, vb, va),
                (VariableAddress va, VariableAddress vb) => UnifyVar(mem, va, vb),
                (VariableAddress va, _) => UnifyVarNonVar(mem, va, b),
                (_, VariableAddress vb) => UnifyVarNonVar(mem, vb, a),
                (AbstractAddress va, _) => UnifyAbstract(mem, va, b),
                (_, AbstractAddress vb) => UnifyAbstract(mem, vb, a),
                _ => false
            };
        }

        static bool UnifyConst(TermMemory mem, ConstAddress ca, ConstAddress cb)
        {
            return mem[ca].Equals(mem[cb]);
        }

        static bool UnifyAbstract(TermMemory mem, AbstractAddress a, ITermAddress b)
        {
            return mem[a].Compiler.Unify(mem, a, b);
        }

        static bool UnifyStruct(TermMemory mem, StructureAddress va, StructureAddress vb)
        {
            var (argsa, argsb) = (mem[va], mem[vb]);
            if (argsa.Length != argsb.Length)
                return false;
            if (!UnifyConst(mem, (ConstAddress)argsa[0], (ConstAddress)argsb[0]))
                return false;
            for (int i = 0; i < argsa.Length; i++)
            {
                if (!UnifyTerm(mem, argsa[i], argsb[i]))
                    return false;
            }
            return true;
        }

        static bool UnifyVarNonVar(TermMemory mem, VariableAddress va, ITermAddress b)
        {
            var derefA = DerefVar(mem, va);
            if (derefA is VariableAddress va1)
            {
                mem[va1] = b;
                return true;
            }
            return Unify(mem, derefA, b, transaction: false);
        }

        static bool UnifyVar(TermMemory mem, VariableAddress va, VariableAddress vb)
        {
            var (derefA, derefB) = (DerefVar(mem, va), DerefVar(mem, vb));
            if (derefA is VariableAddress va2 && derefB is VariableAddress vb2)
            {
                // still variables, must handle the unification
                mem[va2] = mem[vb2];
                return true;
            }
            return Unify(mem, derefA, derefB);
        }
    }
}

