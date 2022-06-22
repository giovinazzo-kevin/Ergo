using Ergo.Interpreter;
using Ergo.Shell.Commands;
using Ergo.Solver;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Ergo.Shell;

public partial class ErgoShell
{
    public readonly ErgoInterpreter Interpreter;
    public readonly CommandDispatcher Dispatcher;
    public readonly Func<LogLine, string> LineFormatter;
    public readonly ExceptionHandler LoggingExceptionHandler;
    public readonly ExceptionHandler ThrowingExceptionHandler;
    public readonly Action<ErgoSolver> ConfigureSolver;

    public TextReader In { get; private set; }
    public TextWriter Out { get; private set; }

    public ShellScope CreateScope() => new(Interpreter.CreateScope().WithRuntime(true).WithExceptionHandler(LoggingExceptionHandler), false);

    public Parsed<T> Parse<T>(ShellScope scope, string data, Func<string, Maybe<T>> onParseFail = null)
    {
        onParseFail ??= (str =>
        {
            scope.Throw($"Could not parse '{data}' as {typeof(T).Name}");
            return default;
        });
        return Interpreter.Parse(scope.InterpreterScope, data, onParseFail);
    }

    // TODO: Extensions
    public IEnumerable<Predicate> GetInterpreterPredicates(ShellScope scope) => SolverBuilder.Build(Interpreter, ref scope).KnowledgeBase.AsEnumerable();
    public IEnumerable<Predicate> GetUserPredicates(ShellScope scope) => scope.InterpreterScope.Modules[WellKnown.Modules.User].Program.KnowledgeBase.AsEnumerable();

    public ErgoShell(
        Action<ErgoInterpreter> configureInterpreter = null,
        Action<ErgoSolver> configureSolver = null,
        Action<ErgoParser> configureParser = null,
        Func<LogLine, string> formatter = null
    )
    {
        Interpreter = new(configureParser: configureParser);
        configureInterpreter?.Invoke(Interpreter);
        ConfigureSolver = configureSolver;
        Dispatcher = new CommandDispatcher(s => WriteLine($"Unknown command: {s}", LogLevel.Err));
        LineFormatter = formatter ?? DefaultLineFormatter;
        LoggingExceptionHandler = new ExceptionHandler((ex) =>
        {
            WriteLine(ex.Message, LogLevel.Err);
        });
        ThrowingExceptionHandler = new ExceptionHandler((ex) => ExceptionDispatchInfo.Capture(ex).Throw());
        AddCommandsByReflection();
        Console.InputEncoding = Encoding.Unicode;
        Console.OutputEncoding = Encoding.Unicode;
        SetConsoleOutputCP(1200);
        SetConsoleCP(1200);
        Clear();
    }

    public ErgoSolver CreateSolver(ref ShellScope scope)
    {
        var solver = SolverBuilder.Build(Interpreter, ref scope);
        ConfigureSolver?.Invoke(solver);
        return solver;
    }

    public bool TryAddCommand(ShellCommand s) => Dispatcher.TryAdd(s);

    protected void AddCommandsByReflection()
    {
        var assembly = typeof(Save).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsAssignableTo(typeof(ShellCommand))) continue;
            if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
            var inst = (ShellCommand)Activator.CreateInstance(type);
            Dispatcher.TryAdd(inst);
        }
    }

    public virtual void Clear()
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Clear();
    }

    public virtual void Save(ShellScope scope, string fileName, bool force = true)
    {
        var preds = GetUserPredicates(scope);
        if (File.Exists(fileName) && !force)
        {
            WriteLine($"File already exists: {fileName}", LogLevel.Err);
            return;
        }

        var module = scope.InterpreterScope.Modules[scope.InterpreterScope.Module];
        // TODO: make it easier to save directives
        var dirs = module.Imports.Contents
            .Select(m => new Directive(new Complex(new("use_module"), m), string.Empty))
            .ToArray();
        var text = new ErgoProgram(dirs, preds.ToArray()).Explain(canonical: false);
        File.WriteAllText(fileName, text);
        WriteLine($"Saved: '{fileName}'.", LogLevel.Inf);
    }

    public virtual void Load(ref ShellScope scope, string fileName)
    {
        var copy = scope;
        var preds = GetInterpreterPredicates(copy);
        var oldPredicates = preds.Count();
        var interpreterScope = copy.InterpreterScope;
        var loaded = Interpreter.TryLoad(ref interpreterScope, fileName);
        loaded.Do(some =>
        {
            var newPredicates = preds.Count();
            var delta = newPredicates - oldPredicates;
            WriteLine($"Loaded: '{fileName}'.\r\n\t{Math.Abs(delta)} {(delta >= 0 ? "new" : "")} predicates have been {(delta >= 0 ? "added" : "removed")}.", LogLevel.Inf);
            copy = copy.WithInterpreterScope(interpreterScope);
        });
        scope = copy;
    }

    public virtual async IAsyncEnumerable<ShellScope> Repl(ShellScope scope, Func<string, bool> exit = null)
    {
        var encoding = new UnicodeEncoding(false, false);
        Out = new StreamWriter(Console.OpenStandardOutput(), encoding);
        In = new StreamReader(Console.OpenStandardInput(), encoding);

        Console.SetOut(Out);
        Console.SetIn(In);

        while (true)
        {
            Write($"{scope.InterpreterScope.Module.Explain()}> ");
            var prompt = Prompt();
            if (exit != null && exit(prompt)) break;
            await foreach (var result in DoAsync(scope, prompt))
            {
                yield return result;
                scope = result;
            }
        }

        Out.Dispose();
        In.Dispose();
    }

    public async IAsyncEnumerable<ShellScope> DoAsync(ShellScope scope, string command)
    {
        await foreach (var result in Dispatcher.Dispatch(this, scope, command))
        {
            yield return result;
        }
    }
}
