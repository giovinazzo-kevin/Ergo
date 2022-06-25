using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Shell.Commands;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Ergo.Shell;
/*
 ImmutableArray<string>.Empty
    .Add(string.Empty)
    .Add("./ergo/stdlib/")
    .Add("./ergo/user/")
*/

public partial class ErgoShell
{
    public readonly ErgoInterpreter Interpreter;

    public readonly CommandDispatcher Dispatcher;
    public readonly Func<LogLine, string> LineFormatter;
    public readonly ExceptionHandler LoggingExceptionHandler;
    public readonly ExceptionHandler ThrowingExceptionHandler;
    public readonly ErgoFacade Facade;

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
        var userDefinedOps = scope.InterpreterScope.GetOperators();
        return new Parsed<T>(Facade, data, onParseFail, userDefinedOps.ToArray());
    }

    internal ErgoShell(
        ErgoFacade facade,
        Func<LogLine, string> formatter = null
    )
    {
        Facade = facade;
        Interpreter = facade.BuildInterpreter();
        Dispatcher = new CommandDispatcher(s => WriteLine($"Unknown command: {s}", LogLevel.Err));
        LineFormatter = formatter ?? DefaultLineFormatter;
        LoggingExceptionHandler = new ExceptionHandler((ex) =>
        {
            WriteLine(ex.Message, LogLevel.Err);
        });
        ThrowingExceptionHandler = new ExceptionHandler((ex) => ExceptionDispatchInfo.Capture(ex).Throw());
        Console.InputEncoding = Encoding.Unicode;
        Console.OutputEncoding = Encoding.Unicode;
        SetConsoleOutputCP(1200);
        SetConsoleCP(1200);
        Clear();
    }

    public bool TryAddCommand(ShellCommand s) => Dispatcher.TryAdd(s);

    public virtual void Clear()
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Clear();
    }

    public virtual void Save(ShellScope scope, string fileName, bool force = true)
    {
        //var preds = GetUserPredicates(scope);
        //if (File.Exists(fileName) && !force)
        //{
        //    WriteLine($"File already exists: {fileName}", LogLevel.Err);
        //    return;
        //}

        //var module = scope.InterpreterScope.Modules[scope.InterpreterScope.Module];
        //// TODO: make it easier to save directives
        //var dirs = module.Imports.Contents
        //    .Select(m => new Directive(new Complex(new("use_module"), m), string.Empty))
        //    .ToArray();
        //var text = new ErgoProgram(dirs, preds.ToArray()).Explain(canonical: false);
        //File.WriteAllText(fileName, text);
        //WriteLine($"Saved: '{fileName}'.", LogLevel.Inf);
    }

    public virtual void Load(ref ShellScope scope, string fileName)
    {
        var copy = scope;
        //var preds = GetInterpreterPredicates(copy);
        //var oldPredicates = preds.Count();
        var interpreterScope = copy.InterpreterScope;
        var loaded = Interpreter.Load(ref interpreterScope, new Atom(fileName));
        loaded.Do(some =>
        {
            //var newPredicates = preds.Count();
            var delta = 0;// newPredicates - oldPredicates;
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
