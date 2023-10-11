using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Shell.Commands;
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
    public readonly ErgoFacade Facade;
    public readonly Encoding Encoding = new UnicodeEncoding(false, false);

    public TextReader In { get; private set; }
    public IAsyncInputReader InputReader { get; private set; }
    public TextWriter Out { get; private set; }
    public TextWriter Err { get; private set; }

    public bool UseColors { get; set; } = true;
    public bool UseANSIEscapeSequences { get; set; } = true;
    public bool UseUnicodeSymbols { get; set; } = true;

    public ShellScope CreateScope(Maybe<InterpreterScope> interpreterScope, Func<ShellScope, ShellScope> transformShell = null)
    {
        transformShell ??= s => s;
        var scope = interpreterScope.Or(() => Interpreter.CreateScope())
            .GetOrThrow(new InvalidOperationException())
            .WithExceptionHandler(LoggingExceptionHandler)
            .WithRuntime(true);
        var kb = scope.BuildKnowledgeBase();
        return transformShell(new(scope, false, kb));
    }

    internal ErgoShell(
        ErgoFacade facade,
        Func<LogLine, string> formatter = null,
        Encoding encoding = null
    )
    {
        Facade = facade;
        Interpreter = facade.BuildInterpreter();
        Dispatcher = new CommandDispatcher(s => WriteLine($"Unknown command: {s}", LogLevel.Err));
        LineFormatter = formatter ?? DefaultLineFormatter;
        Encoding = encoding ?? Encoding;
        InputReader = new ConsoleInputReader();
        LoggingExceptionHandler = new ExceptionHandler((ex) =>
        {
            WriteLine(ex.Message, LogLevel.Err);
        });
        ThrowingExceptionHandler = new ExceptionHandler((ex) => ExceptionDispatchInfo.Capture(ex).Throw());
        if (Encoding.IsSingleByte) // Assume ASCII
        {
            SetConsoleOutputCP(437);
            SetConsoleCP(437);
        }
        else // Assume UTF-16
        {
            SetConsoleOutputCP(1200);
            SetConsoleCP(1200);
        }
        Console.InputEncoding = Encoding;
        Console.OutputEncoding = Encoding;
        Clear();
    }

    public void SetIn(TextReader input, IAsyncInputReader inputReader)
    {
        In = input;
        InputReader = inputReader;
        Console.SetIn(In);
    }

    public void SetOut(TextWriter output)
    {
        Out = output;
        Console.SetOut(Out);
    }

    public void SetErr(TextWriter err)
    {
        Err = err;
        Console.SetError(err);
    }

    public void AddCommand(ShellCommand s) => Dispatcher.Add(s);

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
        var numPredsBefore = scope.KnowledgeBase.Count;
        var interpreterScope = copy.InterpreterScope;
        var loaded = Interpreter.Load(ref interpreterScope, new Atom(fileName));
        loaded.Do(some =>
        {
            var newKb = interpreterScope.BuildKnowledgeBase();
            copy = copy
                .WithInterpreterScope(interpreterScope)
                .WithKnowledgeBase(newKb)
                ;
            var numPredsAfter = newKb.Count;
            var delta = numPredsAfter - numPredsBefore;
            WriteLine($"Loaded: '{fileName}'.\r\n\t{Math.Abs(delta)} {(delta >= 0 ? "new" : "")} predicates have been {(delta >= 0 ? "added" : "removed")}.", LogLevel.Inf);
        });
        scope = copy;
    }

    public static void CancelConsoleInput()
    {
        var handle = GetStdHandle(STD_INPUT_HANDLE);
        CancelIoEx(handle, IntPtr.Zero);
    }

    public virtual async IAsyncEnumerable<ShellScope> Repl(
        Maybe<InterpreterScope> interpreterScope = default, Func<ShellScope, ShellScope> transformScope = null, Func<string, bool> exit = null, [EnumeratorCancellation] CancellationToken ct = default)
    {

        var scope = CreateScope(interpreterScope, transformScope);

        WriteLine("Welcome to the Ergo shell. To list the available commands, enter '?' (without quotes).", LogLevel.Cmt);
        WriteLine("While evaluating solutions, press:", LogLevel.Cmt);
        WriteLine("\t- spacebar to yield the next solution;", LogLevel.Cmt);
        WriteLine("\t- 'c' to enumerate solutions automatically;", LogLevel.Cmt);
        WriteLine("\t- any other key to abort.", LogLevel.Cmt);

        while (true)
        {
            Write($"{scope.InterpreterScope.Entry.Explain()}> ");
            var prompt = await Task.Run(() => Prompt(), ct);
            if (exit != null && exit(prompt))
                break;
            await foreach (var result in DoAsync(scope, prompt, ct))
            {
                yield return result;
                scope = result;
            }
        }

        //Out.Dispose();
        //In.Dispose();
    }

    public async IAsyncEnumerable<ShellScope> DoAsync(ShellScope scope, string command, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var result in Dispatcher.Dispatch(this, scope, command))
        {
            yield return result;
            if (ct.IsCancellationRequested)
                yield break;
        }
    }
}
