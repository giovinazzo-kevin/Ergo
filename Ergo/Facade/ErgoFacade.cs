﻿using Ergo.Interpreter;
using Ergo.Interpreter.Directives;
using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Parser;
using Ergo.Lang.Utils;
using Ergo.Shell;
using Ergo.Shell.Commands;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;
using System.Reflection;

namespace Ergo.Facade;

/// <summary>
/// A simple, front-facing API for the configuration of a complete Ergo environment.
/// </summary>
public readonly struct ErgoFacade
{
    public static readonly ErgoFacade Default = new ErgoFacade()
        .AddBuiltInsByReflection(typeof(AssertZ).Assembly)
        .AddDirectivesByReflection(typeof(UseModule).Assembly)
        .AddCommandsByReflection(typeof(Save).Assembly)
        ;

    private static readonly MethodInfo Parser_TryAddAbstractParser = typeof(ErgoParser).GetMethod(nameof(ErgoParser.TryAddAbstractParser));
    private static readonly MethodInfo Solver_BindDataSource = typeof(ErgoSolver).GetMethod(nameof(ErgoSolver.BindDataSource));
    private static readonly MethodInfo Solver_BindDataSink = typeof(ErgoSolver).GetMethod(nameof(ErgoSolver.BindDataSink));

    private readonly ImmutableHashSet<InterpreterDirective> _directives = ImmutableHashSet<InterpreterDirective>.Empty;
    private readonly ImmutableHashSet<SolverBuiltIn> _builtIns = ImmutableHashSet<SolverBuiltIn>.Empty;
    private readonly ImmutableHashSet<ShellCommand> _commands = ImmutableHashSet<ShellCommand>.Empty;
    private readonly ImmutableDictionary<Type, ImmutableHashSet<IAbstractTermParser>> _parsers = ImmutableDictionary<Type, ImmutableHashSet<IAbstractTermParser>>.Empty;
    private readonly ImmutableDictionary<Type, ImmutableHashSet<IDataSink>> _dataSinks = ImmutableDictionary<Type, ImmutableHashSet<IDataSink>>.Empty;
    private readonly ImmutableDictionary<Type, ImmutableHashSet<IDataSource>> _dataSources = ImmutableDictionary<Type, ImmutableHashSet<IDataSource>>.Empty;

    public ErgoFacade() { }

    private ErgoFacade(ImmutableHashSet<InterpreterDirective> directives, ImmutableHashSet<SolverBuiltIn> builtIns, ImmutableHashSet<ShellCommand> commands, ImmutableDictionary<Type, ImmutableHashSet<IAbstractTermParser>> parsers, ImmutableDictionary<Type, ImmutableHashSet<IDataSink>> dataSinks, ImmutableDictionary<Type, ImmutableHashSet<IDataSource>> dataSources)
    {
        _directives = directives;
        _builtIns = builtIns;
        _commands = commands;
        _parsers = parsers;
        _dataSinks = dataSinks;
        _dataSources = dataSources;
    }

    public ErgoFacade AddDirective(InterpreterDirective directive)
        => new(_directives.Add(directive), _builtIns, _commands, _parsers, _dataSinks, _dataSources);
    public ErgoFacade RemoveDirective(InterpreterDirective directive)
        => new(_directives.Remove(directive), _builtIns, _commands, _parsers, _dataSinks, _dataSources);
    public ErgoFacade AddBuiltIn(SolverBuiltIn builtin)
        => new(_directives, _builtIns.Add(builtin), _commands, _parsers, _dataSinks, _dataSources);
    public ErgoFacade RemoveBuiltIn(SolverBuiltIn builtin)
        => new(_directives, _builtIns.Remove(builtin), _commands, _parsers, _dataSinks, _dataSources);
    public ErgoFacade AddCommand(ShellCommand command)
        => new(_directives, _builtIns, _commands.Add(command), _parsers, _dataSinks, _dataSources);
    public ErgoFacade RemoveCommand(ShellCommand command)
        => new(_directives, _builtIns, _commands.Remove(command), _parsers, _dataSinks, _dataSources);
    public ErgoFacade AddParser<A>(AbstractTermParser<A> parser) where A : IAbstractTerm
        => new(_directives, _builtIns, _commands, _parsers.SetItem(typeof(A), (_parsers.TryGetValue(typeof(A), out var set) ? set : ImmutableHashSet<IAbstractTermParser>.Empty).Add(parser)), _dataSinks, _dataSources);
    public ErgoFacade RemoveParser<A>(AbstractTermParser<A> parser) where A : IAbstractTerm
        => new(_directives, _builtIns, _commands, _parsers.SetItem(typeof(A), (_parsers.TryGetValue(typeof(A), out var set) ? set : ImmutableHashSet<IAbstractTermParser>.Empty).Remove(parser)), _dataSinks, _dataSources);
    public ErgoFacade AddDataSink<A>(DataSink<A> sink) where A : new()
        => new(_directives, _builtIns, _commands, _parsers, _dataSinks.SetItem(typeof(A), (_dataSinks.TryGetValue(typeof(A), out var set) ? set : ImmutableHashSet<IDataSink>.Empty).Add(sink)), _dataSources);
    public ErgoFacade RemoveDataSink<A>(DataSink<A> sink) where A : new()
        => new(_directives, _builtIns, _commands, _parsers, _dataSinks.SetItem(typeof(A), (_dataSinks.TryGetValue(typeof(A), out var set) ? set : ImmutableHashSet<IDataSink>.Empty).Remove(sink)), _dataSources);
    public ErgoFacade AddDataSource<A>(DataSource<A> source) where A : new()
        => new(_directives, _builtIns, _commands, _parsers, _dataSinks, _dataSources.SetItem(typeof(A), (_dataSources.TryGetValue(typeof(A), out var set) ? set : ImmutableHashSet<IDataSource>.Empty).Add(source)));
    public ErgoFacade RemoveDataSource<A>(DataSource<A> source) where A : new()
        => new(_directives, _builtIns, _commands, _parsers, _dataSinks, _dataSources.SetItem(typeof(A), (_dataSources.TryGetValue(typeof(A), out var set) ? set : ImmutableHashSet<IDataSource>.Empty).Remove(source)));

    public ErgoFacade AddDirectivesByReflection(Assembly lookInAssembly)
    {
        var newFacade = this;
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(InterpreterDirective))) continue;
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            var inst = (InterpreterDirective)Activator.CreateInstance(type);
            newFacade = newFacade.AddDirective(inst);
        }

        return newFacade;
    }

    public ErgoFacade AddBuiltInsByReflection(Assembly lookInAssembly)
    {
        var newFacade = this;
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(SolverBuiltIn))) continue;
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            var inst = (SolverBuiltIn)Activator.CreateInstance(type);
            newFacade = newFacade.AddBuiltIn(inst);
        }

        return newFacade;
    }

    public ErgoFacade AddCommandsByReflection(Assembly lookInAssembly)
    {
        var newFacade = this;
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(ShellCommand))) continue;
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            var inst = (ShellCommand)Activator.CreateInstance(type);
            newFacade = newFacade.AddCommand(inst);
        }

        return newFacade;
    }

    private ErgoSolver ConfigureSolver(ErgoSolver solver)
    {
        foreach (var builtIn in _builtIns)
            solver.TryAddBuiltIn(builtIn);
        foreach (var (type, sources) in _dataSources)
        {
            foreach (var source in sources)
                Solver_BindDataSource.MakeGenericMethod(type).Invoke(solver, new object[] { source });
        }

        foreach (var (type, sinks) in _dataSinks)
        {
            foreach (var sink in sinks)
                Solver_BindDataSink.MakeGenericMethod(type).Invoke(solver, new object[] { sink });
        }

        return solver;
    }

    private ErgoInterpreter ConfigureInterpreter(ErgoInterpreter interpreter)
    {
        foreach (var directive in _directives)
            interpreter.TryAddDirective(directive);
        return interpreter;
    }

    private ErgoShell ConfigureShell(ErgoShell shell)
    {
        foreach (var command in _commands)
            shell.TryAddCommand(command);
        return shell;
    }

    private ErgoParser ConfigureParser(ErgoParser parser)
    {
        foreach (var (type, parsers) in _parsers)
        {
            foreach (var abs in parsers)
                Parser_TryAddAbstractParser.MakeGenericMethod(type).Invoke(parser, new object[] { abs });
        }

        return parser;
    }

    public ErgoParser BuildParser(ErgoStream stream, IEnumerable<Operator> operators = null)
        => ConfigureParser(new(this, new(this, stream, operators ?? Enumerable.Empty<Operator>())));
    public ErgoInterpreter BuildInterpreter(InterpreterFlags flags = InterpreterFlags.Default)
        => ConfigureInterpreter(new(this, flags));
    public ErgoSolver BuildSolver(KnowledgeBase kb = null, SolverFlags flags = SolverFlags.Default)
        => ConfigureSolver(new(this, kb ?? new(), flags));
    public ErgoShell BuildShell(Func<ErgoShell.LogLine, string> formatter = null)
        => ConfigureShell(new(this, formatter));
}