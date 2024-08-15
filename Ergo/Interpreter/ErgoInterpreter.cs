﻿using Ergo.Events.Interpreter;
using Ergo.Facade;
using Ergo.Interpreter.Directives;
using Ergo.Interpreter.Libraries;
using Ergo.Lang.Utils;
using System.Collections.Concurrent;
using System.IO;

namespace Ergo.Interpreter;

public partial class ErgoInterpreter
{
    public readonly ErgoFacade Facade;
    public readonly InterpreterFlags Flags;
    private readonly Dictionary<Atom, InterpreterScope> ModuleCache = new();

    protected readonly DiagnosticProbe Probe = new()
    {
#if !ERGO_INTERPRETER_DIAGNOSTICS
        IsEnabled = false,
#endif
    };

    private readonly Dictionary<Atom, Library> _libraries = new();
    public Maybe<Library> GetLibrary(Atom module) => _libraries.TryGetValue(module, out var lib) ? Maybe.Some(lib) : default;

    internal ErgoInterpreter(ErgoFacade facade, InterpreterFlags flags = InterpreterFlags.Default)
    {
        Flags = flags;
        Facade = facade;

    }

    public void AddLibrary(Library l)
    {
        if (_libraries.ContainsKey(l.Module))
            throw new ArgumentException($"A library for module {l.Module} was already added");
        _libraries[l.Module] = l;
    }

    public virtual bool RunDirective(ref InterpreterScope scope, Directive d)
    {
        var watch = Probe.Enter();
        if (scope.VisibleDirectives.TryGetValue(d.Body.GetSignature(), out var directive))
        {
            var sig = directive.Signature.Explain();
            var directiveWatch = Probe.Enter(sig);
            var ret = directive.Execute(this, ref scope, [.. ((Complex)d.Body).Arguments]);
            Probe.Leave(directiveWatch, sig);
            return ret;
        }

        if (Flags.HasFlag(InterpreterFlags.ThrowOnDirectiveNotFound))
        {
            throw new InterpreterException(ErrorType.UndefinedDirective, scope, d.Explain(canonical: false));
        }
        Probe.Leave(watch);
        return false;
    }

    public Module EnsureModule(ref InterpreterScope scope, Atom name)
    {
        if (!scope.Modules.TryGetValue(name, out var module))
        {
            try
            {
                if (Load(ref scope, name).TryGetValue(out module))
                {
                    scope = scope.WithModule(module);
                }
            }
            catch (FileNotFoundException)
            {
                scope = scope
                    .WithModule(module = new Module(name, runtime: true));
            }
        }

        return module;
    }

    public Maybe<Module> LoadDirectives(ref InterpreterScope scope, Atom module)
    {
        return LoadDirectives(ref scope, module, FileStreamUtils.FileStream(scope.SearchDirectories, module.AsQuoted(false).Explain(false)));
    }
    public virtual Maybe<Module> LoadDirectives(ref InterpreterScope scope, Atom moduleName, ErgoStream stream)
    {
        if (scope.Modules.TryGetValue(moduleName, out var module) && module.Program.IsPartial)
            return module;
        var watch = Probe.Enter();
        Probe.Count(moduleName.Explain(), 1);
        var operators = scope.VisibleOperators;
        var parser = Facade.BuildParser(stream, operators);
        var pos = parser.Lexer.State;
        // Bootstrap a new parser by first loading the operator symbols defined in this module
        var newOperators = parser.OperatorDeclarations();
        parser.Lexer.Seek(pos);
        parser = Facade.BuildParser(stream, operators.Concat(newOperators).Distinct());
        if (!scope.ExceptionHandler.TryGet(() => parser.ProgramDirectives()).Map(x => x).TryGetValue(out var program))
        {
            stream.Dispose();
            scope.Throw(ErrorType.CouldNotLoadFile, stream.FileName);
            Probe.Leave(watch);
            return default;
        }
        parser.Lexer.Seek(pos);

        var linkLibrary = Maybe<Library>.None;
        var visibleDirectives = scope.VisibleDirectives;
        var directives = program.Directives.Select(d =>
        {
            if (visibleDirectives.TryGetValue(d.Body.GetSignature(), out var directive))
                return (Ast: d, Builtin: directive, Defined: true);
            return (Ast: d, Builtin: default, Defined: false);
        });
        if (_libraries.TryGetValue(moduleName, out var linkedLib))
        {
            linkLibrary = linkedLib;
            foreach (var dir in linkedLib.GetExportedDirectives())
            {
                if (visibleDirectives.ContainsKey(dir.Signature))
                    break; // This library was already added
                visibleDirectives = visibleDirectives.Add(dir.Signature, dir);
            }
        }
        foreach (var (Ast, Builtin, _) in directives.Where(x => x.Defined).OrderBy(x => x.Builtin.Priority))
        {
            Execute(Ast, Builtin, ref scope);
        }
        var newVisibleDirectives = scope.VisibleDirectives.Concat(visibleDirectives)
            .DistinctBy(x => x.Key)
            .ToImmutableDictionary();
        foreach (var (Ast, _, _) in directives.Where(x => !x.Defined))
        {
            if (newVisibleDirectives.TryGetValue(Ast.Body.GetSignature(), out var directive))
            {
                Execute(Ast, directive, ref scope);
            }
            else
            {
                stream.Dispose();
                scope.Throw(ErrorType.UndefinedDirective, Ast.Explain(false));
                Probe.Leave(watch);
                return default;
            }
        }
        if (!scope.Modules.ContainsKey(moduleName))
        {
            stream.Dispose();
            scope.Throw(ErrorType.ModuleNameDoesNotMatchFileName, moduleName.Explain(false), stream.FileName);
            Probe.Leave(watch);
            return default;
        }
        module = scope.Modules[moduleName]
            .WithImport(scope.BaseImport)
            .WithProgram(program.AsPartial(true));
        stream.Dispose();
        Probe.Leave(watch);
        return module;

        void Execute(Directive Ast, InterpreterDirective BuiltIn, ref InterpreterScope scope)
        {
            var sig = BuiltIn.Signature.Explain();
            var builtinWatch = Probe.Enter(sig);
            BuiltIn.Execute(this, ref scope, [.. ((Complex)Ast.Body).Arguments]);
            // NOTE: It's only after module/2 has been called that the module actually gets its name!
            Probe.Leave(builtinWatch, sig);
        }
    }

    public Maybe<Module> Load(ref InterpreterScope scope, Atom module, int loadOrder = 0)
    {
        if (ModuleCache.TryGetValue(module, out var cachedScope))
        {
            scope = cachedScope;
            return cachedScope.Modules[module];
        }
        if (Load(ref scope, module, FileStreamUtils.FileStream(scope.SearchDirectories, module.AsQuoted(false).Explain(false)), loadOrder)
            .TryGetValue(out var ret))
        {
            if (!ret.Program.IsPartial)
                ModuleCache[module] = scope;
            return ret;
        }
        return default;
    }
    public virtual Maybe<Module> Load(ref InterpreterScope scope, Atom moduleName, ErgoStream stream, int loadOrder = 0)
    {
        if (!LoadDirectives(ref scope, moduleName, stream).TryGetValue(out var module))
            return default;
        // By now, some imports may still be only partially loaded or outright unloaded in the case of deep import trees
        var checkedModules = new HashSet<Atom>();
        var modulesToCheck = new Stack<Atom>();
        foreach (Atom m in module.Imports.Contents)
            modulesToCheck.Push(m);
        var checkedModulesConcurrent = new ConcurrentDictionary<Atom, bool>();
        while (modulesToCheck.Count > 0)
        {
            if (modulesToCheck.TryPop(out var import))
            {
                if (!scope.Modules[import].Program.IsPartial)
                    continue;
                var importScope = scope.WithCurrentModule(moduleName);
                if (!Load(ref importScope, import, loadOrder + 1).TryGetValue(out var importModule))
                    return Maybe.None<Module>();
                importModule.Imports.Contents
                    .Select(x => (Atom)x)
                    .Where(m => checkedModulesConcurrent.TryAdd(m, true))
                    .ToList()
                    .ForEach(modulesToCheck.Push);
                scope = scope.WithModule(importModule);
            }
        }

        var parser = Facade.BuildParser(stream, scope.VisibleOperators);
        if (!scope.ExceptionHandler.TryGet(() => parser.Program()).Map(x => x).TryGetValue(out var program))
        {
            stream.Dispose();
            scope.Throw(ErrorType.CouldNotLoadFile, stream.FileName);
            return default;
        }

        stream.Dispose();
        scope = scope.WithModule(module = module
            .WithProgram(program.AsPartial(false))
            .WithLoadOrder(loadOrder));

        // Invoke ModuleLoaded so that libraries can, e.g., rewrite predicates
        scope = scope
            .ForwardEventToLibraries(new ModuleLoadedEvent(this, module.Name) { Scope = scope })
            .Scope;

        scope.ExceptionHandler.Try(() => parser.Dispose());
        return module;
    }

    public InterpreterScope CreateScope(Func<InterpreterScope, InterpreterScope> configureStdlibScope = null)
    {
        configureStdlibScope ??= s => s;
        var stdlibScope = new InterpreterScope(Facade, new Module(WellKnown.Modules.Stdlib, runtime: true));
        stdlibScope = configureStdlibScope(stdlibScope);
        Load(ref stdlibScope, WellKnown.Modules.Stdlib);
        var scope = stdlibScope
            .WithRuntime(false)
            .WithCurrentModule(WellKnown.Modules.User)
            .WithModule(new Module(WellKnown.Modules.User, runtime: true)
                .WithImport(WellKnown.Modules.Stdlib));
#if ERGO_INTERPRETER_DIAGNOSTICS
        Console.WriteLine(Probe.GetDiagnostics());
#endif
        return scope;
    }
}
