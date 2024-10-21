using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Parser;
using Ergo.Lang.Utils;
using Ergo.Modules;
using Ergo.Modules.Libraries;
using Ergo.Shell;
using Ergo.Shell.Commands;
using System.IO;
using System.Reflection;
using System.Text;

namespace Ergo.Facade;

/// <summary>
/// A simple, front-facing API for the configuration of a complete Ergo environment.
/// </summary>
public readonly struct ErgoFacade
{
    private static readonly MethodInfo Parser_TryAddAbstractParser = typeof(LegacyErgoParser).GetMethod(nameof(LegacyErgoParser.AddAbstractParser));
    private static readonly MethodInfo This_AddParser = typeof(ErgoFacade).GetMethod(nameof(ErgoFacade.AddAbstractParser));

    private static readonly Dictionary<string, LegacyErgoParser> ParserCache = [];

    /// <summary>
    /// The default Ergo environment complete with all the standard Built-Ins, Directives, Commands and Abstract Term Parsers.
    /// </summary>
    public static readonly ErgoFacade Standard = new ErgoFacade()
        .AddLibrariesByReflection(typeof(ErgoLibrary).Assembly)
        .AddCommandsByReflection(typeof(Save).Assembly)
        .AddParsersByReflection(typeof(DictParser).Assembly)
        ;

    private readonly ImmutableHashSet<Func<ErgoLibrary>> _libraries = ImmutableHashSet<Func<ErgoLibrary>>.Empty;
    private readonly ImmutableHashSet<ShellCommand> _commands = ImmutableHashSet<ShellCommand>.Empty;
    private readonly ImmutableDictionary<Type, IAbstractTermParser> _parsers = ImmutableDictionary<Type, IAbstractTermParser>.Empty;

    public readonly Func<InterpreterScope, InterpreterScope> ConfigureStdlibScopeHandler;
    public readonly Func<ErgoInterpreter, InterpreterScope, InterpreterScope> ConfigureInterpreterScopeHandler;
    public readonly Action<LegacyKnowledgeBase> BeforeKbCompiledHandler;
    public readonly Action<LegacyKnowledgeBase> AfterKbCompiledHandler;

    public readonly bool TrimKnowledgeBase = true;

    public readonly Maybe<TextReader> Input = default;
    public readonly Maybe<TextWriter> Output = default;
    public readonly Maybe<TextWriter> Error = default;
    public readonly Maybe<IAsyncInputReader> InputReader = default;
    public readonly InterpreterFlags InterpreterFlags = InterpreterFlags.Default;
    public readonly CompilerFlags CompilerFlags = CompilerFlags.Default;
    public readonly DecimalType DecimalType = DecimalType.CliDecimal;

    public ErgoFacade() { }

    private ErgoFacade(
        ImmutableHashSet<Func<ErgoLibrary>> libs,
        ImmutableHashSet<ShellCommand> commands,
        ImmutableDictionary<Type, IAbstractTermParser> parsers,
        Maybe<TextReader> inStream,
        Maybe<TextWriter> outStream,
        Maybe<TextWriter> errStream,
        Maybe<IAsyncInputReader> inReader,
        Func<InterpreterScope, InterpreterScope> configureStdlibScope,
        Func<ErgoInterpreter, InterpreterScope, InterpreterScope> configureInterpreterScope,
        Action<LegacyKnowledgeBase> beforeKbCompiled,
        Action<LegacyKnowledgeBase> afterKbCompiled,
        InterpreterFlags interpreterFlags,
        CompilerFlags compilerFlags,
        DecimalType decimalType,
        bool trimKnowledgeBase
    )
    {
        _libraries = libs;
        _commands = commands;
        _parsers = parsers;
        ConfigureStdlibScopeHandler = configureStdlibScope ?? (s => s);
        ConfigureInterpreterScopeHandler = configureInterpreterScope ?? ((i, s) => s);
        BeforeKbCompiledHandler = beforeKbCompiled ?? (kb => { });
        AfterKbCompiledHandler = afterKbCompiled ?? (kb => { });
        InterpreterFlags = interpreterFlags;
        CompilerFlags = compilerFlags;
        DecimalType = decimalType;
        TrimKnowledgeBase = trimKnowledgeBase;
        Input = inStream;
        Output = outStream;
        Error = errStream;
        InputReader = inReader;
    }

    public ErgoFacade AddLibrary(Func<ErgoLibrary> lib)
        => new(_libraries.Add(lib), _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade AddCommand(ShellCommand command)
        => new(_libraries, _commands.Add(command), _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade RemoveCommand(ShellCommand command)
        => new(_libraries, _commands.Remove(command), _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade AddAbstractParser<A>(IAbstractTermParser<A> parser) where A : AbstractTerm
        => new(_libraries, _commands, _parsers.SetItem(typeof(A), parser), Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade RemoveAbstractParser<A>() where A : AbstractTerm
        => new(_libraries, _commands, _parsers.Remove(typeof(A)), Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade SetInput(TextReader input, Maybe<IAsyncInputReader> reader = default) => new(_libraries, _commands, _parsers, input, Output, Error, reader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade SetOutput(TextWriter output) => new(_libraries, _commands, _parsers, Input, output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade SetError(TextWriter err) => new(_libraries, _commands, _parsers, Input, Output, err, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade ConfigureStdlibScope(Func<InterpreterScope, InterpreterScope> f) => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, f, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade ConfigureInterpreterScope(Func<ErgoInterpreter, InterpreterScope, InterpreterScope> f) => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, f, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade BeforeKnowledgeBaseCompile(Action<LegacyKnowledgeBase> a) => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, a, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade AfterKnowledgeBaseCompile(Action<LegacyKnowledgeBase> a) => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, a, InterpreterFlags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade SetInterpreterFlags(InterpreterFlags flags)
        => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, flags, CompilerFlags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade SetCompilerFlags(CompilerFlags flags)
        => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, flags, DecimalType, TrimKnowledgeBase);
    public ErgoFacade SetDecimalType(DecimalType type)
        => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, type, TrimKnowledgeBase);
    public ErgoFacade SetTrimKnowledgeBase(bool trimKnowledgeBase)
        => new(_libraries, _commands, _parsers, Input, Output, Error, InputReader, ConfigureStdlibScopeHandler, ConfigureInterpreterScopeHandler, BeforeKbCompiledHandler, AfterKbCompiledHandler, InterpreterFlags, CompilerFlags, DecimalType, trimKnowledgeBase);

    /// <summary>
    /// Adds all libraries with a public parameterless constructor in the target assembly.
    /// </summary>
    public ErgoFacade AddLibrariesByReflection(Assembly lookInAssembly)
    {
        var newFacade = this;
        foreach (var type in lookInAssembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(IErgoLibrary))) continue;
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            newFacade = newFacade.AddLibrary(() => (ErgoLibrary)Activator.CreateInstance(type));
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

    private LegacyErgoParser ConfigureParser(LegacyErgoParser parser)
    {
        foreach (var (type, absParser) in _parsers)
            Parser_TryAddAbstractParser.MakeGenericMethod(type).Invoke(parser, new object[] { absParser });
        return parser;
    }

    public LegacyErgoParser BuildParser(ErgoStream stream, IEnumerable<Operator> operators = null)
        => ConfigureParser(new(new(stream, operators ?? [])));
    public ErgoInterpreter BuildInterpreter()
        => ConfigureInterpreter(new(this));
    public ErgoVM BuildVM(LegacyKnowledgeBase kb)
        => ConfigureVM(new(kb));
    public ErgoVM BuildVM(ref InterpreterScope scope)
        => BuildVM((scope = scope.WithFacade(this)).BuildKnowledgeBase());
    public ErgoVM BuildVM() {
        var interpreter = BuildInterpreter();
        var scope = interpreter.CreateScope();
        var kb = scope.BuildKnowledgeBase();
        return BuildVM(kb);
    }
    public ErgoShell BuildShell(Func<LogLine, string> formatter = null, Encoding encoding = null)
        => ConfigureShell(new(this, formatter, encoding));
}
