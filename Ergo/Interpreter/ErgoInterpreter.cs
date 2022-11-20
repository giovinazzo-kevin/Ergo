using Ergo.Events.Interpreter;
using Ergo.Facade;
using Ergo.Interpreter.Libraries;
using Ergo.Lang.Utils;
using System.IO;

namespace Ergo.Interpreter;

public partial class ErgoInterpreter
{
    public readonly ErgoFacade Facade;
    public readonly InterpreterFlags Flags;

    protected readonly DiagnosticProbe Probe = new();

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
            var ret = directive.Execute(this, ref scope, ((Complex)d.Body).Arguments.ToArray());
            Probe.Leave(directiveWatch, sig);
            return ret;
        }

        if (Flags.HasFlag(InterpreterFlags.ThrowOnDirectiveNotFound))
        {
            throw new InterpreterException(InterpreterError.UndefinedDirective, scope, d.Explain(canonical: false));
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
        return LoadDirectives(ref scope, FileStreamUtils.FileStream(scope.SearchDirectories, module.AsQuoted(false).Explain(false)));
    }
    public virtual Maybe<Module> LoadDirectives(ref InterpreterScope scope, ErgoStream stream)
    {
        if (scope.Modules.TryGetValue(scope.Entry, out var m) && m.Program.IsPartial)
            return m;
        var watch = Probe.Enter();
        var operators = scope.GetOperators();
        var parser = Facade.BuildParser(stream, operators);
        var pos = parser.Lexer.State;
        // Bootstrap a new parser by first loading the operator symbols defined in this module
        var newOperators = parser.OperatorDeclarations();
        parser.Lexer.Seek(pos);
        parser = Facade.BuildParser(stream, operators.Concat(newOperators).Distinct());
        if (!scope.ExceptionHandler.TryGet(() => parser.ProgramDirectives()).Map(x => x).TryGetValue(out var program))
        {
            stream.Dispose();
            scope.Throw(InterpreterError.CouldNotLoadFile, stream.FileName);
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
        if (_libraries.TryGetValue(scope.Entry, out var linkedLib))
        {
            linkLibrary = linkedLib;
            foreach (var dir in linkedLib.GetExportedDirectives())
            {
                if (visibleDirectives.ContainsKey(dir.Signature))
                    break; // This library was already added
                visibleDirectives = visibleDirectives.Add(dir.Signature, dir);
            }
        }
        foreach (var (Ast, Builtin, _) in directives.Where(x => !x.Defined))
        {
            stream.Dispose();
            scope.Throw(InterpreterError.UndefinedDirective, Ast.Explain(false));
            Probe.Leave(watch);
            return default;
        }

        foreach (var (Ast, Builtin, _) in directives.Where(x => x.Defined).OrderBy(x => x.Builtin.Priority))
        {
            var sig = Builtin.Signature.Explain();
            var builtinWatch = Probe.Enter(sig);
            Builtin.Execute(this, ref scope, ((Complex)Ast.Body).Arguments.ToArray());
            Probe.Leave(builtinWatch, sig);
        }

        var module = scope.EntryModule
            .WithProgram(program);

        Probe.Leave(watch);
        return module;
    }

    public Maybe<Module> Load(ref InterpreterScope scope, Atom module)
    {
        return Load(ref scope, FileStreamUtils.FileStream(scope.SearchDirectories, module.AsQuoted(false).Explain(false)));
    }
    public virtual Maybe<Module> Load(ref InterpreterScope scope, ErgoStream stream)
    {
        if (!LoadDirectives(ref scope, stream).TryGetValue(out var module))
            return default;
        // By now, all imports have been loaded but some may still be partially loaded (only the directives exist)
        foreach (Atom import in module.Imports.Contents)
        {
            if (scope.Modules[import].Program.IsPartial)
            {
                var importScope = scope.WithCurrentModule(import);
                if (!Load(ref importScope, import).TryGetValue(out var importModule))
                    return default;
                scope = scope.WithModule(importModule);
            }
        }

        using var parser = Facade.BuildParser(stream, scope.GetOperators());
        if (!scope.ExceptionHandler.TryGet(() => parser.Program()).Map(x => x).TryGetValue(out var program))
        {
            stream.Dispose();
            scope.Throw(InterpreterError.CouldNotLoadFile, stream.FileName);
            return default;
        }

        stream.Dispose();
        scope = scope.WithModule(module = module.WithProgram(program));

        // Invoke ModuleLoaded so that libraries can, e.g., rewrite predicates
        scope = scope
            .ForwardEventToLibraries(new ModuleLoadedEvent(this) { Scope = scope })
            .Scope;
        return module;
    }

    public InterpreterScope CreateScope(Func<InterpreterScope, InterpreterScope> configureStdlibScope = null)
    {
        configureStdlibScope ??= s => s;
        var stdlibScope = new InterpreterScope(new Module(WellKnown.Modules.Stdlib, runtime: true));
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

    public Maybe<T> Parse<T>(InterpreterScope scope, string data, Func<string, Maybe<T>> onParseFail = null)
    {
        onParseFail ??= (str =>
        {
            scope.Throw(InterpreterError.CouldNotParseTerm, typeof(T), data);
            return Maybe<T>.None;
        });
        var userDefinedOps = scope.GetOperators();
        return scope.ExceptionHandler.TryGet(() => new Parsed<T>(Facade, data, onParseFail, userDefinedOps.ToArray())
            .Value).Map(x => x);
    }
}
