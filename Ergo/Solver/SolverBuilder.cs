using Ergo.Interpreter;
using Ergo.Shell;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;

namespace Ergo.Solver;

public static class SolverBuilder
{
    private static readonly ConcurrentDictionary<InterpreterScope, AsyncLocal<KnowledgeBase>> _scopeCache = new();

    public static ErgoSolver Build(ErgoInterpreter i, ref ShellScope scope, SolverFlags flags = SolverFlags.Default)
    {
        var interpreterScope = scope.InterpreterScope;
        var solver = Build(i, ref interpreterScope, flags, scope);
        scope = scope.WithInterpreterScope(interpreterScope);
        return solver;
    }

    public static ErgoSolver Build(ErgoInterpreter i, ref InterpreterScope scope, SolverFlags flags = SolverFlags.Default, ShellScope shellScope = default)
    {
        if (!_scopeCache.TryGetValue(scope, out var kb) || kb.Value == null)
        {
            kb ??= new();
            kb.Value = new();
            var added = LoadModule(ref scope, kb.Value, scope.Modules[scope.Module]);
            foreach (var module in scope.Modules.Values)
            {
                LoadModule(ref scope, kb.Value, module, added);
            }

            if (!_scopeCache.TryAdd(scope, kb))
            {
                kb = _scopeCache[scope];
            }
        }

        return new ErgoSolver(i, scope, kb.Value, flags, shellScope);
        HashSet<Atom> LoadModule(ref InterpreterScope scope, KnowledgeBase kb, Module module, HashSet<Atom> added = null)
        {
            added ??= new();
            if (added.Contains(module.Name))
                return added;
            added.Add(module.Name);
            foreach (var subModule in module.Imports.Contents.Select(c => (Atom)c))
            {
                if (added.Contains(subModule))
                    continue;
                if (!scope.Modules.TryGetValue(subModule, out var import))
                {
                    var importScope = scope;
                    scope = scope.WithModule(import = i.Load(ref importScope, subModule.Explain()));
                }

                LoadModule(ref scope, kb, import, added);
            }

            foreach (var pred in module.Program.KnowledgeBase)
            {
                var sig = pred.Head.GetSignature();
                kb.AssertZ(pred.WithModuleName(module.Name).Qualified());
                if (module.Name == scope.Module || module.ContainsExport(sig))
                {
                    kb.AssertZ(pred.WithModuleName(module.Name));
                }
            }

            foreach (var key in i.DynamicPredicates.Keys.Where(k => k.Module.Reduce(some => some, () => WellKnown.Modules.User) == module.Name))
            {
                foreach (var dyn in i.DynamicPredicates[key])
                {
                    if (!dyn.AssertZ)
                    {
                        kb.AssertA(dyn.Predicate);
                    }
                    else
                    {
                        kb.AssertZ(dyn.Predicate);
                    }
                }
            }

            return added;
        }
    }

}
