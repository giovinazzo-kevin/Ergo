﻿using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang.Exceptions.Handler;
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
    public static readonly Encoding Encoding = new UnicodeEncoding(false, false);

    public TextReader In { get; private set; }
    public TextWriter Out { get; private set; }
    public TextWriter Err { get; private set; }

    public ShellScope CreateScope()
    {
        var interpreterScope = Interpreter.CreateScope(x => x.WithExceptionHandler(LoggingExceptionHandler))
            .WithRuntime(true);
        var kb = interpreterScope.BuildKnowledgeBase();
        return new(interpreterScope, false, kb);
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
        SetConsoleOutputCP(1200);
        SetConsoleCP(1200);
        Console.InputEncoding = Encoding;
        Console.OutputEncoding = Encoding;
        Clear();
    }

    public void SetIn(TextReader input)
    {
        In = input;
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

    public virtual async IAsyncEnumerable<ShellScope> Repl(Func<ShellScope, ShellScope> transformScope = null, Func<string, bool> exit = null)
    {

        var scope = CreateScope();
        if (transformScope != null)
            scope = transformScope(scope);

        WriteLine("Welcome to the Ergo shell. To list the available commands, enter '?' (without quotes)." +
            "\r\nWhile evaluating solutions, press:" +
            "\r\n\t- spacebar to yield the next solution;" +
            "\r\n\t- 'c' to enumerate solutions automatically;" +
            "\r\n\t- any other key to abort.", LogLevel.Cmt);

        while (true)
        {
            Write($"{scope.InterpreterScope.Entry.Explain()}> ");
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
