using Ergo.Events;
using Ergo.Events.Interpreter;
using Ergo.Facade;
using Ergo.Interpreter.Directives;
using Ergo.Interpreter.Libraries;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Interpreter;

public readonly struct InterpreterScope
{
    public readonly ErgoFacade Facade;
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
    /// The name of the entry module.
    /// </summary>
    public readonly Atom Entry;
    /// <summary>
    /// The name of the base module that will be automatically imported by all modules being loaded.
    /// Usually, this is going to be the stdlib but you can create your own base module that imports stdlib and have all new modules depend on that base.
    /// </summary>
    public readonly Atom BaseImport;
    /// <summary>
    /// The entry module.
    /// </summary>
    public Module EntryModule => Modules[Entry];

    public readonly ImmutableArray<Operator> VisibleOperators;
    public readonly ImmutableArray<Atom> VisibleModules;
    public readonly ImmutableHashSet<Library> VisibleLibraries;
    public readonly ImmutableDictionary<Signature, BuiltIn> VisibleBuiltIns;
    /// <summary>
    /// List optimized for enumeration otherwise identical to VisibleBuiltIns.Keys
    /// </summary>
    public readonly IReadOnlyList<Signature> VisibleBuiltInsKeys;
    public readonly ImmutableDictionary<Signature, InterpreterDirective> VisibleDirectives;

    public InterpreterScope(ErgoFacade facade, Module userModule)
    {
        Facade = facade;
        Entry = userModule.Name;
        Modules = ImmutableDictionary.Create<Atom, Module>()
            .Add(userModule.Name, userModule);
        SearchDirectories = [@".\ergo\", @".\user\"]
            ;
        IsRuntime = userModule.IsRuntime;
        ExceptionHandler = default;
        VisibleModules = GetVisibleModules(Entry, Modules).ToImmutableArray();
        VisibleLibraries = GetVisibleLibraries(VisibleModules, Modules);
        VisibleDirectives = GetVisibleDirectives(VisibleModules, Modules);
        VisibleBuiltIns = GetVisibleBuiltIns(VisibleModules, Modules);
        VisibleBuiltInsKeys = VisibleBuiltIns.Keys.ToList();
        BaseImport = WellKnown.Modules.Stdlib;
        VisibleOperators = GetOperators().ToImmutableArray();
    }

    private InterpreterScope(
        ErgoFacade facade,
        Atom @base,
        Atom currentModule,
        ImmutableDictionary<Atom, Module> modules,
        ImmutableArray<string> dirs,
        bool runtime,
        ExceptionHandler handler)
    {
        Facade = facade;
        BaseImport = @base;
        Modules = modules;
        SearchDirectories = dirs;
        Entry = currentModule;
        IsRuntime = runtime;
        ExceptionHandler = handler;
        VisibleModules = GetVisibleModules(Entry, Modules).ToImmutableArray();
        VisibleLibraries = GetVisibleLibraries(VisibleModules, Modules);
        VisibleDirectives = GetVisibleDirectives(VisibleModules, Modules);
        VisibleBuiltIns = GetVisibleBuiltIns(VisibleModules, Modules);
        VisibleBuiltInsKeys = VisibleBuiltIns.Keys.ToList();
        VisibleOperators = GetOperators().ToImmutableArray();
    }

    public KnowledgeBase BuildKnowledgeBase(CompilerFlags vmFlags = CompilerFlags.Default, Action<KnowledgeBase> beforeCompile = null)
    {
        var kb = new KnowledgeBase(this);
        foreach (var builtIn in VisibleBuiltIns.Values)
        {
            kb.AssertZ(new Predicate(builtIn));
        }
        foreach (var module in VisibleModules)
        {
            foreach (var pred in Modules[module].Program.KnowledgeBase)
            {
                var newPred = pred;
                if (!pred.IsBuiltIn)
                {
                    newPred = pred.WithModuleName(module);
                    //if (newPred.Head.GetQualification(out var newHead).TryGetValue(out var newModule))
                    //    newPred = newPred.WithModuleName(newModule).WithHead(newHead);
                    if (!pred.IsExported)
                        newPred = newPred.Qualified();
                }
                kb.AssertZ(newPred);
            }
        }
        beforeCompile?.Invoke(kb);
        ForwardEventToLibraries(new KnowledgeBaseCreatedEvent(kb, vmFlags));
        return kb;
    }

    public int GetImportDepth(Atom a, Maybe<Atom> entry = default)
    {
        return GetImportDepthInner(a, entry.GetOr(Entry), 0).GetOr(-1);
    }
    private Maybe<int> GetImportDepthInner(Atom a, Atom entry, int d, HashSet<Atom> visited = null)
    {
        visited ??= [];
        visited.Add(a);
        if (!Modules.ContainsKey(a))
            return default;
        if (a.Equals(entry))
            return d;
        var imports = Modules[a].Imports.Contents;
        if (!imports.Any())
            return default;
        foreach (var import in imports)
        {
            if (visited.Contains(a))
                continue;
            if (GetImportDepthInner(a, (Atom)import, d + 1).TryGetValue(out var D))
                return D;
        }
        return default;
    }

    public InterpreterScope WithBaseModule(Atom a) => new(Facade, a, Entry, Modules, SearchDirectories, IsRuntime, ExceptionHandler);
    public InterpreterScope WithCurrentModule(Atom a) => new(Facade, BaseImport, a, Modules, SearchDirectories, IsRuntime, ExceptionHandler);
    public InterpreterScope WithModule(Module m) => new(Facade, BaseImport, Entry, Modules.SetItem(m.Name, m), SearchDirectories, IsRuntime, ExceptionHandler);
    public InterpreterScope WithoutModule(Atom m) => new(Facade, BaseImport, Entry, Modules.Remove(m), SearchDirectories, IsRuntime, ExceptionHandler);
    public InterpreterScope WithSearchDirectory(string s) => new(Facade, BaseImport, Entry, Modules, SearchDirectories.Add(s), IsRuntime, ExceptionHandler);
    public InterpreterScope WithRuntime(bool runtime) => new(Facade, BaseImport, Entry, Modules, SearchDirectories, runtime, ExceptionHandler);
    public InterpreterScope WithExceptionHandler(ExceptionHandler newHandler) => new(Facade, BaseImport, Entry, Modules, SearchDirectories, IsRuntime, newHandler);
    public InterpreterScope WithoutModules() => new(Facade, BaseImport, Entry, ImmutableDictionary.Create<Atom, Module>().Add(WellKnown.Modules.Stdlib, Modules[WellKnown.Modules.Stdlib]), SearchDirectories, IsRuntime, ExceptionHandler);
    public InterpreterScope WithoutSearchDirectories() => new(Facade, BaseImport, Entry, Modules, [], IsRuntime, ExceptionHandler);

    public T GetLibrary<T>(Maybe<Atom> module = default) where T : Library => Modules[module.GetOr(Entry)].LinkedLibrary
        .GetOrThrow(new ArgumentException(null, nameof(module)))
        as T;

    public T ForwardEventToLibraries<T>(T e)
        where T : ErgoEvent
    {
        foreach (var lib in VisibleLibraries.OrderBy(x => x.LoadOrder))
            lib.OnErgoEvent(e);
        return e;
    }

    private static ImmutableHashSet<Library> GetVisibleLibraries(IEnumerable<Atom> visibleModules, ImmutableDictionary<Atom, Module> modules)
        => visibleModules
            .Select(m => (HasValue: modules[m].LinkedLibrary.TryGetValue(out var lib), Value: lib))
            .Where(x => x.HasValue)
            .Select(x => x.Value)
        .ToImmutableHashSet();
    private static ImmutableDictionary<Signature, InterpreterDirective> GetVisibleDirectives(IEnumerable<Atom> visibleModules, ImmutableDictionary<Atom, Module> modules)
        => visibleModules
            .SelectMany(m => modules[m].LinkedLibrary
                .Select(l => l.GetExportedDirectives())
                .GetOr([]))
            .Where(d => visibleModules.Contains(d.Signature.Module.GetOr(WellKnown.Modules.Stdlib)))
            .ToImmutableDictionary(x => x.Signature);

    private static ImmutableDictionary<Signature, BuiltIn> GetVisibleBuiltIns(IEnumerable<Atom> visibleModules, ImmutableDictionary<Atom, Module> modules)
        => visibleModules
            .SelectMany(m => modules[m].LinkedLibrary
                .Select(l => l.GetExportedBuiltins())
                .GetOr([]))
            .Where(b => visibleModules.Contains(b.Signature.Module.GetOr(WellKnown.Modules.Stdlib)))
            .ToImmutableDictionary(x => x.Signature);

    /// <summary>
    /// Returns all operators that are visible from the entry module.
    /// </summary>
    private IEnumerable<Operator> GetOperators()
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
            added ??= [];
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
            foreach (var op in WellKnown.Operators.DeclaredOperators)
            {
                if (op.DeclaringModule == entryModule)
                    yield return (op, int.MaxValue);
            }
        }
    }

    /// <summary>
    /// Returns all modules that are visible from the entry module.
    /// </summary>
    private static IEnumerable<Atom> GetVisibleModules(Atom entry, ImmutableDictionary<Atom, Module> modules)
    {
        return Inner(entry, modules, [])
            .Select((x, i) => (x.A, x.D, i))
            .OrderBy(x => x.D)
            .ThenByDescending(x => x.i)
            .Select(x => x.A);
        static IEnumerable<(Atom A, int D)> Inner(Atom entry, ImmutableDictionary<Atom, Module> modules, HashSet<Atom> seen, int depth = 0)
        {
            if (seen.Contains(entry) || !modules.ContainsKey(entry))
                yield break;
            var current = modules[entry];
            yield return (entry, depth);
            seen.Add(entry);
            foreach (var import in current.Imports.Contents.Cast<Atom>()
                .SelectMany(i => Inner(i, modules, seen, depth + 1)))
            {
                yield return import;
            }
        }
    }

    public Maybe<T> Parse<T>(string data, Func<string, Maybe<T>> onParseFail = null)
    {
        var self = this;
        onParseFail ??= (str =>
        {
            self.Throw(ErgoInterpreter.ErrorType.CouldNotParseTerm, typeof(T), data);
            return Maybe<T>.None;
        });
        var fac = Facade;
        var userDefinedOps = VisibleOperators;
        return ExceptionHandler.TryGet(() => new Parsed<T>(fac, data, userDefinedOps.ToArray(), onParseFail).Value).Map(x => x);
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
            added ??= [];
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
    public void Throw(ErgoInterpreter.ErrorType error, params object[] args) => ExceptionHandler.Throw(new InterpreterException(error, this, args));

}