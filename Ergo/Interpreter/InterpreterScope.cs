using Ergo.Events;
using Ergo.Interpreter.Directives;
using Ergo.Interpreter.Libraries;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Solver.BuiltIns;

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

    public readonly ImmutableHashSet<Atom> VisibleModules;
    public readonly ImmutableDictionary<Atom, Library> VisibleLibraries;
    public readonly ImmutableDictionary<Signature, SolverBuiltIn> VisibleBuiltIns;
    /// <summary>
    /// List optimized for enumeration otherwise identical to VisibleBuiltIns.Keys
    /// </summary>
    public readonly IReadOnlyList<Signature> VisibleBuiltInsKeys;
    public readonly ImmutableDictionary<Signature, InterpreterDirective> VisibleDirectives;

    public InterpreterScope(Module userModule)
    {
        Entry = userModule.Name;
        Modules = ImmutableDictionary.Create<Atom, Module>()
            .Add(userModule.Name, userModule);
        SearchDirectories = ImmutableArray<string>.Empty
            .Add(@".\ergo\")
            .Add(@".\user\")
            ;
        IsRuntime = userModule.IsRuntime;
        ExceptionHandler = default;
        KnowledgeBase = null;
        VisibleModules = GetVisibleModules(Entry, Modules).ToImmutableHashSet();
        VisibleLibraries = GetVisibleLibraries(VisibleModules, Modules);
        VisibleDirectives = GetVisibleDirectives(VisibleModules, Modules);
        VisibleBuiltIns = GetVisibleBuiltIns(VisibleModules, Modules);
        VisibleBuiltInsKeys = VisibleBuiltIns.Keys.ToList();
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
        VisibleModules = GetVisibleModules(Entry, Modules).ToImmutableHashSet();
        VisibleLibraries = GetVisibleLibraries(VisibleModules, Modules);
        VisibleDirectives = GetVisibleDirectives(VisibleModules, Modules);
        VisibleBuiltIns = GetVisibleBuiltIns(VisibleModules, Modules);
        VisibleBuiltInsKeys = VisibleBuiltIns.Keys.ToList();
        if (kb is null)
        {
            foreach (var module in VisibleModules)
            {
                foreach (var pred in Modules[module].Program.KnowledgeBase)
                {
                    var newPred = pred.WithModuleName(module);
                    if (newPred.Head.GetQualification(out var newHead).TryGetValue(out var newModule))
                        newPred = newPred.WithModuleName(newModule).WithHead(newHead);
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

    public T GetLibrary<T>(Atom module) where T : Library => Modules[module].LinkedLibrary
        .GetOrThrow(new ArgumentException(null, nameof(module)))
        as T;

    public T ForwardEventToLibraries<T>(T e)
        where T : ErgoEvent
    {
        foreach (var lib in VisibleLibraries.Values)
            lib.OnErgoEvent(e);
        return e;
    }

    private static ImmutableDictionary<Atom, Library> GetVisibleLibraries(ImmutableHashSet<Atom> visibleModules, ImmutableDictionary<Atom, Module> modules)
        => visibleModules
            .Select(m => (HasValue: modules[m].LinkedLibrary.TryGetValue(out var lib), Value: lib))
            .Where(x => x.HasValue)
            .Select(x => x.Value)
        .ToImmutableDictionary(l => l.Module);
    private static ImmutableDictionary<Signature, InterpreterDirective> GetVisibleDirectives(ImmutableHashSet<Atom> visibleModules, ImmutableDictionary<Atom, Module> modules)
        => visibleModules
            .SelectMany(m => modules[m].LinkedLibrary
                .Select(l => l.GetExportedDirectives())
                .GetOr(Enumerable.Empty<InterpreterDirective>()))
            .ToImmutableDictionary(x => x.Signature);

    private static ImmutableDictionary<Signature, SolverBuiltIn> GetVisibleBuiltIns(ImmutableHashSet<Atom> visibleModules, ImmutableDictionary<Atom, Module> modules)
        => visibleModules
            .SelectMany(m => modules[m].LinkedLibrary
                .Select(l => l.GetExportedBuiltins())
                .GetOr(Enumerable.Empty<SolverBuiltIn>()))
            .ToImmutableDictionary(x => x.Signature);

    /// <summary>
    /// Returns all operators that are visible from the entry module.
    /// </summary>
    public IEnumerable<Operator> GetOperators()
    {
        var operators = GetOperatorsInner(Entry, Modules)
            .ToList();
        foreach (var (op, _) in operators)
        {
            // if (!operators.Any(other => other.Depth < depth && other.Op.Synonyms.SequenceEqual(op.Synonyms)))
            yield return op;
        }

        static IEnumerable<(Operator Op, int Depth)> GetOperatorsInner(Atom defaultModule, ImmutableDictionary<Atom, Module> modules, Maybe<Atom> entry = default, HashSet<Atom> added = null, int depth = 0)
        {
            added ??= new();
            var currentModule = defaultModule;
            var entryModule = entry.GetOr(currentModule);
            if (added.Contains(entryModule) || !modules.TryGetValue(entryModule, out var module))
            {
                yield break;
            }

            added.Add(entryModule);
            var depth_ = depth;
            foreach (Atom import in module.Imports.Contents)
            {
                foreach (var importedOp in GetOperatorsInner(defaultModule, modules, import, added, ++depth_ * 1000))
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
    private static IEnumerable<Atom> GetVisibleModules(Atom entry, ImmutableDictionary<Atom, Module> modules, HashSet<Atom> seen = null)
    {
        seen ??= new();
        if (seen.Contains(entry) || !modules.ContainsKey(entry))
            yield break;
        var current = modules[entry];
        yield return entry;
        seen.Add(entry);
        foreach (var import in current.Imports.Contents.Cast<Atom>()
            .SelectMany(i => GetVisibleModules(i, modules, seen)))
        {
            yield return import;
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
        return Inner(name, entry.GetOr(Entry), Modules);

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