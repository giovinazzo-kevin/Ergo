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
                .Add("./stdlib/");
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
        public InterpreterScope WithSearchDirectory(string s) => new(Module, Modules, SearchDirectories.Add(s), Runtime);
        public InterpreterScope WithRuntime(bool runtime) => new(Module, Modules, SearchDirectories, runtime);

        public InterpreterScope WithoutModules() => new(Module, ImmutableDictionary.Create<Atom, Module>().Add(Interpreter.Modules.Prologue, Modules[Interpreter.Modules.Prologue]), SearchDirectories, Runtime);
        public InterpreterScope WithoutSearchDirectories() => new(Module, Modules, ImmutableArray<string>.Empty, Runtime);

        public IEnumerable<Operator> GetUserDefinedOperators(Maybe<Atom> entry = default, HashSet<Atom> added = null)
        {
            added ??= new();
            var currentModule = Module;
            var entryModule = entry.Reduce(some => some, () => currentModule);
            if (added.Contains(entryModule) || !Modules.TryGetValue(entryModule, out var module))
            {
                yield break;
            }
            added.Add(entryModule);
            foreach (var import in module.Imports.Contents)
            {
                foreach (var importedOp in GetUserDefinedOperators(Maybe.Some((Atom)import), added))
                {
                    yield return importedOp;
                }
            }
            foreach (var op in module.Operators)
            {
                if (!module.Exports.Contents.Any(t =>
                {
                    var x = TermMarshall.FromTerm(t, new
                    { Predicate = default(string), Arity = default(int) },
                        TermMarshall.MarshallingMode.Positional
                    );
                    return op.Synonyms.Any(s => Equals(s.Value, x.Predicate))
                        && (x.Arity == 1 && op.Affix != OperatorAffix.Infix
                        || x.Arity == 2);
                }))
                {
                    continue;
                }
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
    }
}
