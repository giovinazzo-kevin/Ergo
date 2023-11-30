using Ergo.Interpreter;
using Ergo.Interpreter.Libraries;
using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Compiler;
using Ergo.Lang.Parser;
using Ergo.Lang.Utils;
using Ergo.Shell;
using Ergo.Shell.Commands;
using Ergo.VM;
using System.IO;
using System.Reflection;
using System.Text;

namespace Ergo.Facade;

/// <summary>
/// A simple, front-facing API for the configuration of a complete Ergo environment.
/// </summary>
public readonly struct ErgoFacade
{
    private static readonly MethodInfo Parser_TryAddAbstractParser = typeof(ErgoParser).GetMethod(nameof(ErgoParser.AddAbstractParser));
    private static readonly MethodInfo This_AddParser = typeof(ErgoFacade).GetMethod(nameof(ErgoFacade.AddAbstractParser));

    private static readonly Dictionary<string, ErgoParser> ParserCache = new();

    /// <summary>
    /// The default Ergo environment complete with all the standard Built-Ins, Directives, Commands and Abstract Term Parsers.
    /// </summary>
    public static readonly ErgoFacade Standard = new ErgoFacade()
        .AddLibrariesByReflection(typeof(Library).Assembly)
        .AddCommandsByReflection(typeof(Save).Assembly)
        .AddParsersByReflection(typeof(DictParser).Assembly)
        ;

    private readonly ImmutableHashSet<Func<Library>> _libraries = ImmutableHashSet<Func<Library>>.Empty;
    private readonly ImmutableHashSet<ShellCommand> _commands = ImmutableHashSet<ShellCommand>.Empty;
    private readonly ImmutableDictionary<Type, IAbstractTermParser> _parsers = ImmutableDictionary<Type, IAbstractTermParser>.Empty;
    public readonly Maybe<TextReader> Input = default;
    public readonly Maybe<TextWriter> Output = default;
    public readonly Maybe<TextWriter> Error = default;
    public readonly Maybe<IAsyncInputReader> InputReader = default;

    public ErgoFacade() { }

    private ErgoFacade(
        ImmutableHashSet<Func<Library>> libs,
        ImmutableHashSet<ShellCommand> commands,
        ImmutableDictionary<Type, IAbstractTermParser> parsers,
        Maybe<TextReader> inStream,
        Maybe<TextWriter> outStream,
        Maybe<TextWriter> errStream,
        Maybe<IAsyncInputReader> inReader
    )
    {
        _libraries = libs;
        _commands = commands;
        _parsers = parsers;
        Input = inStream;
        Output = outStream;
        Error = errStream;
        InputReader = inReader;
    }

    public ErgoFacade AddLibrary(Func<Library> lib)
        => new(_libraries.Add(lib), _commands, _parsers, Input, Output, Error, InputReader);
    public ErgoFacade AddCommand(ShellCommand command)
        => new(_libraries, _commands.Add(command), _parsers, Input, Output, Error, InputReader);
    public ErgoFacade RemoveCommand(ShellCommand command)
        => new(_libraries, _commands.Remove(command), _parsers, Input, Output, Error, InputReader);
    public ErgoFacade AddAbstractParser<A>(IAbstractTermParser<A> parser) where A : AbstractTerm
        => new(_libraries, _commands, _parsers.SetItem(typeof(A), parser), Input, Output, Error, InputReader);
    public ErgoFacade RemoveAbstractParser<A>() where A : AbstractTerm
        => new(_libraries, _commands, _parsers.Remove(typeof(A)), Input, Output, Error, InputReader);
    public ErgoFacade SetInput(TextReader input, Maybe<IAsyncInputReader> reader = default) => new(_libraries, _commands, _parsers, input, Output, Error, reader);
    public ErgoFacade SetOutput(TextWriter output) => new(_libraries, _commands, _parsers, Input, output, Error, InputReader);
    public ErgoFacade SetError(TextWriter err) => new(_libraries, _commands, _parsers, Input, Output, err, InputReader);
    /// <summary>
    /// Adds all libraries with a public parameterless constructor in the target assembly.
    /// </summary>
    public ErgoFacade AddLibrariesByReflection(Assembly lookInAssembly)
    {
        var newFacade = this;
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(Library))) continue;
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            newFacade = newFacade.AddLibrary(() => (Library)Activator.CreateInstance(type));
        }

        return newFacade;
    }

    /// <summary>
    /// Adds all commands with a public parameterless constructor in the target assembly.
    /// </summary>
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

    /// <summary>
    /// Adds all abstract term parsers with a public parameterless constructor in the target assembly.
    /// </summary>
    public ErgoFacade AddParsersByReflection(Assembly lookInAssembly)
    {
        var newFacade = this;
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            if (type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAbstractTermParser<>)) is not { } inter) continue;
            var inst = Activator.CreateInstance(type);
            newFacade = (ErgoFacade)This_AddParser.MakeGenericMethod(inter.GetGenericArguments().Single()).Invoke(newFacade, new object[] { inst });
        }

        return newFacade;
    }

    private ErgoVM ConfigureVM(ErgoVM vm)
    {
        if (Input.TryGetValue(out var input))
            vm.SetIn(input);
        if (Output.TryGetValue(out var output))
            vm.SetOut(output);
        if (Error.TryGetValue(out var error))
            vm.SetErr(error);
        return vm;
    }

    private ErgoInterpreter ConfigureInterpreter(ErgoInterpreter interpreter)
    {
        foreach (var createLib in _libraries)
            interpreter.AddLibrary(createLib());
        return interpreter;
    }

    private ErgoShell ConfigureShell(ErgoShell shell)
    {
        shell.SetIn(Input.GetOrLazy(() => new StreamReader(Console.OpenStandardInput(), shell.Encoding)), InputReader.GetOr(shell.InputReader));
        shell.SetOut(Output.GetOrLazy(() => new StreamWriter(Console.OpenStandardOutput(), shell.Encoding)));
        shell.SetErr(Error.GetOrLazy(() => new StreamWriter(Console.OpenStandardError(), shell.Encoding)));
        foreach (var command in _commands)
            shell.AddCommand(command);
        return shell;
    }

    private ErgoParser ConfigureParser(ErgoParser parser)
    {
        foreach (var (type, absParser) in _parsers)
            Parser_TryAddAbstractParser.MakeGenericMethod(type).Invoke(parser, new object[] { absParser });
        return parser;
    }

    public ErgoParser BuildParser(ErgoStream stream, IEnumerable<Operator> operators = null)
        => ConfigureParser(new(this, new(this, stream, operators ?? Enumerable.Empty<Operator>())));
    public ErgoInterpreter BuildInterpreter(InterpreterFlags flags = InterpreterFlags.Default)
        => ConfigureInterpreter(new(this, flags));
    public ErgoVM BuildVM(KnowledgeBase kb, VMFlags flags = VMFlags.Default, DecimalType decimalType = DecimalType.CliDecimal)
        => ConfigureVM(new(kb, flags, decimalType));
    public ErgoShell BuildShell(Func<LogLine, string> formatter = null, Encoding encoding = null)
        => ConfigureShell(new(this, formatter, encoding));

    public Maybe<T> Parse<T>(InterpreterScope scope, string data, Func<string, Maybe<T>> onParseFail = null)
    {
        onParseFail ??= (str =>
        {
            scope.Throw(ErgoInterpreter.ErrorType.CouldNotParseTerm, typeof(T), data);
            return Maybe<T>.None;
        });
        var userDefinedOps = scope.VisibleOperators;
        var self = this;
        return scope.ExceptionHandler.TryGet(() => new Parsed<T>(self, data, userDefinedOps.ToArray(), onParseFail).Value).Map(x => x);
    }
}
