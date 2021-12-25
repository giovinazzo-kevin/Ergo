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

        public readonly Atom CurrentModule;

        public InterpreterScope(Module userModule)
        {
            Modules = ImmutableDictionary.Create<Atom, Module>()
                .Add(userModule.Name, userModule);
            SearchDirectories = ImmutableArray.Create<string>()
                .Add(string.Empty)
                .Add("./stdlib/");
            CurrentModule = userModule.Name;
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
            CurrentModule = currentModule;
            Runtime = runtime;
        }

        public InterpreterScope WithCurrentModule(Atom a) => new(a, Modules, SearchDirectories, Runtime);
        public InterpreterScope WithModule(Module m) => new(CurrentModule, Modules.SetItem(m.Name, m), SearchDirectories, Runtime);
        public InterpreterScope WithSearchDirectory(string s) => new(CurrentModule, Modules, SearchDirectories.Add(s), Runtime);
        public InterpreterScope WithRuntime(bool runtime) => new(CurrentModule, Modules, SearchDirectories, runtime);

        public InterpreterScope WithoutModules() => new(CurrentModule, ImmutableDictionary.Create<Atom, Module>().Add(Interpreter.Modules.Prologue, Modules[Interpreter.Modules.Prologue]), SearchDirectories, Runtime);
        public InterpreterScope WithoutSearchDirectories() => new(CurrentModule, Modules, ImmutableArray.Create<string>(), Runtime);

        public IEnumerable<Operator> GetUserDefinedOperators(Maybe<Atom> entry = default)
        {
            var currentModule = CurrentModule;
            var entryModule = entry.Reduce(some => some, () => currentModule);
            if (!Modules.TryGetValue(entryModule, out var module))
            {
                yield break;
            }
            foreach (var import in module.Imports.Contents)
            {
                foreach (var importedOp in GetUserDefinedOperators(Maybe.Some((Atom)import)))
                {
                    if (!Modules[(Atom)import].Exports.Contents.Any(t =>
                    {
                        var x = TermMarshall.FromTerm(t, new
                        { Predicate = default(string), Arity = default(int) },
                            TermMarshall.MarshallingMode.Positional
                        );
                        return importedOp.Synonyms.Any(s => Equals(s.Value, x.Predicate))
                        && (x.Arity == 1 && importedOp.Affix != OperatorAffix.Infix
                        || x.Arity == 2);
                    }))
                    {
                        continue;
                    }
                    yield return importedOp;
                }
            }
            foreach (var op in module.Operators)
            {
                yield return op;
            }
        }
    }
}
