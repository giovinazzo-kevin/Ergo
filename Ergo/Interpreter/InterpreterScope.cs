using Ergo.Lang.Exceptions.Handler;

namespace Ergo.Interpreter;

public readonly struct InterpreterScope
{
    /// <summary>
    /// Indicates whether this scope pertains to a top level session. If false, this scope pertains to a file that is being loaded.
    /// </summary>
    public readonly bool IsRuntime;
    /// <summary>
    /// All modules that were loaded so far are stored here. Note that some modules may only be loaded partially.
    /// </summary>
    public readonly ImmutableDictionary<Atom, Module> Modules;
    /// <summary>
    /// A list of paths to look in when reading a file.
    /// </summary>
    public readonly ImmutableArray<string> SearchDirectories;
    /// <summary>
    /// Handles all Ergo exceptions; the ErgoShell uses its own handlers.
    /// </summary>
    public readonly ExceptionHandler ExceptionHandler;
    /// <summary>
    /// Contains all predicates that were defined in Modules. Can be used to create ErgoSolvers.
    /// </summary>
    public readonly KnowledgeBase KnowledgeBase;
    /// <summary>
    /// The name of the entry module.
    /// </summary>
    public readonly Atom Entry;
    /// <summary>
    /// The entry module.
    /// </summary>
    public Module EntryModule => Modules[Entry];

    public InterpreterScope(Module userModule)
    {
        Entry = userModule.Name;
        Modules = ImmutableDictionary.Create<Atom, Module>()
            .Add(userModule.Name, userModule);
        SearchDirectories = ImmutableArray<string>.Empty
            .Add(string.Empty)
            .Add("./ergo/stdlib/")
            .Add("./ergo/user/");
        IsRuntime = userModule.IsRuntime;
        ExceptionHandler = default;
        KnowledgeBase = null;
    }

    private InterpreterScope(
        Atom currentModule,
        ImmutableDictionary<Atom, Module> modules,
        ImmutableArray<string> dirs,
        bool runtime,
        ExceptionHandler handler,
        KnowledgeBase kb)
    {
        Modules = modules;
        SearchDirectories = dirs;
        Entry = currentModule;
        IsRuntime = runtime;
        ExceptionHandler = handler;
        KnowledgeBase = kb ?? new();
        if (kb is null)
        {
            foreach (var module in GetVisibleModules())
            {
                foreach (var pred in module.Program.KnowledgeBase)
                {
                    var newPred = pred.WithModuleName(module.Name);
                    if (!pred.IsExported)
                        newPred = newPred.Qualified();

                    KnowledgeBase.AssertZ(newPred);
                }
            }
        }
    }

    public InterpreterScope WithCurrentModule(Atom a) => new(a, Modules, SearchDirectories, IsRuntime, ExceptionHandler, KnowledgeBase);
    public InterpreterScope WithModule(Module m) => new(Entry, Modules.SetItem(m.Name, m), SearchDirectories, IsRuntime, ExceptionHandler, null);
    public InterpreterScope WithoutModule(Atom m) => new(Entry, Modules.Remove(m), SearchDirectories, IsRuntime, ExceptionHandler, null);
    public InterpreterScope WithSearchDirectory(string s) => new(Entry, Modules, SearchDirectories.Add(s), IsRuntime, ExceptionHandler, KnowledgeBase);
    public InterpreterScope WithRuntime(bool runtime) => new(Entry, Modules, SearchDirectories, runtime, ExceptionHandler, KnowledgeBase);
    public InterpreterScope WithExceptionHandler(ExceptionHandler newHandler) => new(Entry, Modules, SearchDirectories, IsRuntime, newHandler, KnowledgeBase);
    public InterpreterScope WithoutModules() => new(Entry, ImmutableDictionary.Create<Atom, Module>().Add(WellKnown.Modules.Stdlib, Modules[WellKnown.Modules.Stdlib]), SearchDirectories, IsRuntime, ExceptionHandler, null);
    public InterpreterScope WithoutSearchDirectories() => new(Entry, Modules, ImmutableArray<string>.Empty, IsRuntime, ExceptionHandler, KnowledgeBase);

    /// <summary>
    /// Returns all operators that are visible from the entry module.
    /// </summary>
    public IEnumerable<Operator> GetOperators()
    {
        var operators = GetOperatorsInner(Entry, Modules)
            .ToList();
        foreach (var (op, depth) in operators)
        {
            if (!operators.Any(other => other.Depth < depth && other.Op.Synonyms.SequenceEqual(op.Synonyms)))
                yield return op;
        }

        static IEnumerable<(Operator Op, int Depth)> GetOperatorsInner(Atom defaultModule, ImmutableDictionary<Atom, Module> modules, Maybe<Atom> entry = default, HashSet<Atom> added = null, int depth = 0)
        {
            added ??= new();
            var currentModule = defaultModule;
            var entryModule = entry.Reduce(some => some, () => currentModule);
            if (added.Contains(entryModule) || !modules.TryGetValue(entryModule, out var module))
            {
                yield break;
            }

            added.Add(entryModule);
            var depth_ = depth;
            foreach (Atom import in module.Imports.Contents)
            {
                foreach (var importedOp in GetOperatorsInner(defaultModule, modules, Maybe.Some(import), added, ++depth_ * 1000))
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
    }

    /// <summary>
    /// Returns all modules that are visible from the entry module.
    /// </summary>
    public IEnumerable<Module> GetVisibleModules()
    {
        return Inner(this, Entry);
        IEnumerable<Module> Inner(InterpreterScope scope, Atom entry, HashSet<Atom> seen = default)
        {
            seen ??= new();
            if (seen.Contains(entry) || !scope.Modules.ContainsKey(entry))
                yield break;
            var current = scope.Modules[entry];
            yield return current;
            seen.Add(entry);
            foreach (var import in current.Imports.Contents.Cast<Atom>()
                .SelectMany(i => Inner(scope, i, seen)))
            {
                yield return import;
            }
        }
    }

    /// <summary>
    /// Determines whether a module is visible from a given entry module.
    /// </summary>
    /// <param name="name">The name of the module to check</param>
    /// <param name="entry">The entry module, or None to use this scope's entry module</param>
    /// <param name="added"></param>
    public bool IsModuleVisible(Atom name, Maybe<Atom> entry = default)
    {
        return Inner(name, entry.Or(Entry), Modules);

        static bool Inner(Atom name, Atom entry, IDictionary<Atom, Module> modules, HashSet<Atom> added = null)
        {
            added ??= new();
            if (added.Contains(entry) || !modules.TryGetValue(entry, out var module))
            {
                return false;
            }

            added.Add(entry);
            foreach (var import in module.Imports.Contents)
            {
                if (import.Equals(name))
                    return true;
                if (Inner(name, (Atom)import, modules, added))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Throws an InterpreterException through the ExceptionHandler.
    /// </summary>
    public void Throw(InterpreterError error, params object[] args) => ExceptionHandler.Throw(new InterpreterException(error, this, args));
}