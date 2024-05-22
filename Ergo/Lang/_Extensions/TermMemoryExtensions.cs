﻿using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Compiler;

namespace Ergo.Lang.Extensions;

public static class TermMemoryExtensions
{
    public static ITerm Dereference(this TermMemory vm, ITermAddress addr)
    {
        return addr switch
        {
            ConstAddress c => new Atom(vm[c]),
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
                    return new Variable(name);
                return new Variable($"__V{v.Index}");
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
            var functor = new Atom(vm[const_functor]);
            var rest = args[1..]
                .Select(a => Dereference(vm, a))
                .ToArray();
            return new Complex(functor, rest);
        }
    }

    public static ITermAddress StoreTerm(this TermMemory vm, ITerm term)
    {
        if (term.GetFunctor().TryGetValue(out var functor))
        {
            var args = term.GetArguments();
            var addr_functor = vm.StoreAtom(functor.Value);
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

    public static bool Unify(this TermMemory mem, ITermAddress a, ITermAddress b)
    {
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

        static bool UnifyAbstract(TermMemory mem, AbstractAddress aa, ITermAddress b)
        {
            return true;
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
            return derefA.Equals(b);
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

