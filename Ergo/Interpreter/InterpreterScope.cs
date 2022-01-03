using Ergo.Lang.Ast;
using Ergo.Solver.BuiltIns;
using Ergo.Interpreter.Directives;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ergo.Lang.Exceptions;
using Ergo.Solver;
using System.Linq;
using Ergo.Lang;

namespace Ergo.Interpreter
{
    public readonly struct InterpreterScope
    {
        public readonly ImmutableDictionary<Atom, Module> Modules;
        public readonly ImmutableArray<string> SearchDirectories;
        public readonly bool Runtime;

        public readonly Atom Module;

        public InterpreterScope(Module userModule)
        {
            Modules = ImmutableDictionary.Create<Atom, Module>()
                .Add(userModule.Name, userModule);
            SearchDirectories = ImmutableArray<string>.Empty
                .Add(string.Empty)
                .Add("./ergo/stdlib/")
                .Add("./ergo/user/");
            Module = userModule.Name;
            Runtime = userModule.Runtime;
        }

        private InterpreterScope(
            Atom currentModule, 
            ImmutableDictionary<Atom, Module> modules, 
            ImmutableArray<string> dirs,
            bool runtime)
        {
            Modules = modules;
            SearchDirectories = dirs;
            Module = currentModule;
            Runtime = runtime;
        }

        public InterpreterScope WithCurrentModule(Atom a) => new(a, Modules, SearchDirectories, Runtime);
        public InterpreterScope WithModule(Module m) => new(Module, Modules.SetItem(m.Name, m), SearchDirectories, Runtime);
        public InterpreterScope WithoutModule(Atom m) => new(Module, Modules.Remove(m), SearchDirectories, Runtime);
        public InterpreterScope WithSearchDirectory(string s) => new(Module, Modules, SearchDirectories.Add(s), Runtime);
        public InterpreterScope WithRuntime(bool runtime) => new(Module, Modules, SearchDirectories, runtime);

        public InterpreterScope WithoutModules() => new(Module, ImmutableDictionary.Create<Atom, Module>().Add(Interpreter.Modules.Stdlib, Modules[Interpreter.Modules.Stdlib]), SearchDirectories, Runtime);
        public InterpreterScope WithoutSearchDirectories() => new(Module, Modules, ImmutableArray<string>.Empty, Runtime);

        private IEnumerable<(Operator Op, int Depth)> GetOperatorsInner(Maybe<Atom> entry = default, HashSet<Atom> added = null, int depth = 0)
        {
            added ??= new();
            var currentModule = Module;
            var entryModule = entry.Reduce(some => some, () => currentModule);
            if (added.Contains(entryModule) || !Modules.TryGetValue(entryModule, out var module))
            {
                yield break;
            }
            added.Add(entryModule);
            var depth_ = depth;
            foreach (Atom import in module.Imports.Contents)
            {
                foreach (var importedOp in GetOperatorsInner(Maybe.Some(import), added, ++depth_ * 1000))
                {
                    yield return importedOp;
                }
            }
            foreach (var op in module.Operators)
            {
                yield return (op, depth);
            }
            // Add well-known operators in a way that allows for their re-definition by modules down the import chain.
            // An example is the arity indicator (/)/2, that gets re-defined by the math module as the division operator.
            // In practice user code will only ever see the division operator, but the arity indicator ensures proper semantics when the math module is not loaded.
            foreach (var op in WellKnown.Operators.DefinedOperators)
            {
                if (op.DeclaringModule == entryModule)
                    yield return (op, int.MaxValue);
            }
        }

        public IEnumerable<Operator> GetOperators()
        {
            var operators = GetOperatorsInner()
                .ToList();
            foreach (var (op, depth) in operators)
            {
                if (!operators.Any(other => other.Depth < depth && other.Op.Synonyms.SequenceEqual(op.Synonyms)))
                    yield return op;
            }
        }


        public bool TryReplaceLiterals(ITerm term, out ITerm changed, Maybe<Atom> entry = default, HashSet<Atom> added = null)
        {
            changed = default;
            if (term is Variable) 
                return false;
            if (term.IsQualified && term.TryGetQualification(out var qm, out var qv))
                return TryReplaceLiterals(qv, out changed, Maybe.Some(qm));
            added ??= new();
            var currentModule = Module;
            var entryModule = entry.Reduce(some => some, () => currentModule);
            if (added.Contains(entryModule) || !Modules.TryGetValue(entryModule, out var module))
            {
                return false;
            }
            added.Add(entryModule);
            if(term is Atom a)
            {
                if(module.Literals.TryGetValue(a, out var literal))
                {
                    changed = literal.Value;
                    return true;
                }
            }
            if(term is Complex c)
            {
                var args = new ITerm[c.Arguments.Length];
                var any = false;
                for (int i = 0; i < args.Length; i++)
                {
                    if(TryReplaceLiterals(c.Arguments[i], out var arg, entry))
                    {
                        any = true;
                        args[i] = arg;
                    }
                    else
                    {
                        args[i] = c.Arguments[i];
                    }
                }
                changed = c.WithArguments(args);
                return any;
            }
            foreach (var import in module.Imports.Contents.Reverse())
            {
                if(TryReplaceLiterals(term, out changed, Maybe.Some((Atom)import), added))
                    return true;
            }
            return false;
        }
        
        public bool IsModuleVisible(Atom name, Maybe<Atom> entry = default, HashSet<Atom> added = null)
        {
            added ??= new();
            var currentModule = Module;
            var entryModule = entry.Reduce(some => some, () => currentModule);
            if (added.Contains(entryModule) || !Modules.TryGetValue(entryModule, out var module))
            {
                return false;
            }
            added.Add(entryModule);
            foreach (var import in module.Imports.Contents)
            {
                if (import.Equals(name))
                    return true;
                if (IsModuleVisible(name, Maybe.Some((Atom)import), added))
                    return true;
            }
            return false;
        }
    }
}
