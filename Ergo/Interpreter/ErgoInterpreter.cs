using Ergo.Facade;
using Ergo.Interpreter.Directives;
using Ergo.Lang.Utils;
using System.IO;

namespace Ergo.Interpreter;

public partial class ErgoInterpreter
{
    public readonly ErgoFacade Facade;
    public readonly InterpreterFlags Flags;

    protected readonly DiagnosticProbe Probe = new();

    private readonly Dictionary<Signature, InterpreterDirective> _directives = new();
    internal ErgoInterpreter(ErgoFacade facade, InterpreterFlags flags = InterpreterFlags.Default)
    {
        Flags = flags;
        Facade = facade;
    }

    public void AddDirective(InterpreterDirective d)
    {
        if (_directives.ContainsKey(d.Signature))
            throw new NotSupportedException($"Another directive with the same signature was already added");
        _directives[d.Signature] = d;
    }

    public virtual bool RunDirective(ref InterpreterScope scope, Directive d)
    {
        var watch = Probe.Enter();
        if (_directives.TryGetValue(d.Body.GetSignature(), out var directive))
        {
            var sig = directive.Signature.Explain();
            var directiveWatch = Probe.Enter(sig);
            var ret = directive.Execute(this, ref scope, ((Complex)d.Body).Arguments);
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
        => LoadDirectives(ref scope, FileStreamUtils.FileStream(scope.SearchDirectories, module.AsQuoted(false).Explain(false)));
    public virtual Maybe<Module> LoadDirectives(ref InterpreterScope scope, ErgoStream stream)
    {
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
        var directives = program.Directives.Select(d =>
        {
            if (_directives.TryGetValue(d.Body.GetSignature(), out var directive))
                return (Ast: d, Builtin: directive, Defined: true);
            return (Ast: d, Builtin: default, Defined: false);
        });
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
            Builtin.Execute(this, ref scope, ((Complex)Ast.Body).Arguments);
            Probe.Leave(builtinWatch, sig);
        }

        var module = scope.EntryModule
            .WithProgram(program);
        foreach (Atom import in module.Imports.Contents)
        {
            if (scope.Modules.TryGetValue(import, out var importedModule))
                continue;
            var importScope = scope;
            if (!LoadDirectives(ref importScope, import).TryGetValue(out importedModule))
                continue;
            scope = scope.WithModule(importedModule);
        }

        Probe.Leave(watch);
        return module;
    }

    public Maybe<Module> Load(ref InterpreterScope scope, Atom module)
        => Load(ref scope, FileStreamUtils.FileStream(scope.SearchDirectories, module.AsQuoted(false).Explain(false)));
    public virtual Maybe<Module> Load(ref InterpreterScope scope, ErgoStream stream)
    {
        if (!LoadDirectives(ref scope, stream).TryGetValue(out var module))
            return default;

        // By now, all imports have been loaded but some may still be partially loaded (only the directives exist)
        foreach (Atom import in module.Imports.Contents)
        {
            if (scope.Modules[import].Program.IsPartial)
            {
                var importScope = scope.WithoutModule(import);
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

        var ctx = new InstantiationContext("L");
        // At this point we can apply local transformations such as those required by tabling
        foreach (var sig in scope.EntryModule.TabledPredicates)
        {
            var auxFunctor = new Atom(sig.Functor.Explain() + "__aux_");
            var anon = sig.Functor.BuildAnonymousTerm(sig.Arity.GetOr(0));
            var aux = ((ITerm)new Complex(auxFunctor, anon.GetArguments())).Qualified(scope.Entry);

            var tblPred = new Predicate(
                "(auto-generated auxilliary predicate for tabling)",
                scope.Entry,
                anon,
                new NTuple(new ITerm[] { new Complex(new Atom("tabled"), aux) }),
                true,
                true
            );

            foreach (var match in scope.KnowledgeBase.GetMatches(ctx, anon.Qualified(scope.Entry), desugar: false))
            {
                match.Rhs.Head.GetQualification(out var head);
                var auxPred = new Predicate(
                    match.Rhs.Documentation,
                    match.Rhs.DeclaringModule,
                    head.WithFunctor(auxFunctor),
                    match.Rhs.Body,
                    match.Rhs.IsDynamic,
                    false
                );
                var check = program.KnowledgeBase.Retract(head);
                program.KnowledgeBase.AssertZ(auxPred);
            }
            program.KnowledgeBase.AssertZ(tblPred);
            scope = scope.WithModule(module = module.WithProgram(program));
        }

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
        return new Parsed<T>(Facade, data, onParseFail, userDefinedOps.ToArray())
            .Value;
    }
}
