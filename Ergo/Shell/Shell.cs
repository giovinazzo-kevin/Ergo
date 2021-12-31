using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Utils;
using Ergo.Shell.Commands;
using Ergo.Solver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace Ergo.Shell
{
    public partial class ErgoShell
    {
        public readonly ErgoInterpreter Interpreter;
        public readonly CommandDispatcher Dispatcher;
        public readonly Func<LogLine, string> LineFormatter;
        protected readonly ExceptionHandler DefaultExceptionHandler;
        public readonly Action<ErgoSolver> ConfigureSolver;

        public ShellScope CreateScope() => new(Interpreter.CreateUserScope().WithRuntime(true), DefaultExceptionHandler, false, false);

        public Parsed<T> Parse<T>(ShellScope scope, string data, Func<string, Maybe<T>> onParseFail = null)
        {
            onParseFail ??= (str =>
            {
                if (scope.ExceptionHandler.TryGet<T>(scope, () => throw new ShellException($"Could not parse '{data}' as {typeof(T).Name}"), out var ret))
                    return Maybe.Some(ret);
                return Maybe<T>.None;
            });
            var userDefinedOps = scope.InterpreterScope.GetUserDefinedOperators().ToArray();
            return new Parsed<T>(data, onParseFail, userDefinedOps);
        }

        // TODO: Extensions
        public IEnumerable<Predicate> GetInterpreterPredicates(ShellScope scope) => new ErgoSolver(Interpreter, scope.InterpreterScope).KnowledgeBase.AsEnumerable();
        public IEnumerable<Predicate> GetUserPredicates(ShellScope scope) => scope.InterpreterScope.Modules[Modules.User].Program.KnowledgeBase.AsEnumerable();

        public ErgoShell(Action<ErgoInterpreter> configureInterpreter = null, Action<ErgoSolver> configureSolver = null, Func<LogLine, string> formatter = null)
        {
            Interpreter = new();
            configureInterpreter(Interpreter);
            ConfigureSolver = configureSolver;
            Dispatcher = new CommandDispatcher(s => WriteLine($"Unknown command: {s}", LogLevel.Err));
            LineFormatter = formatter ?? DefaultLineFormatter;
            DefaultExceptionHandler = new ExceptionHandler((scope, ex) => {
                WriteLine(ex.Message, LogLevel.Err);
                if (scope.ExceptionThrowingEnabled && !(ex is ShellException || ex is InterpreterException || ex is ParserException || ex is LexerException)) {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            });
            AddCommandsByReflection();
            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.Unicode;
            SetConsoleOutputCP(1200);
            SetConsoleCP(1200);
            Clear();
        }

        public ErgoSolver CreateSolver(ShellScope scope)
        {
            var solver = new ErgoSolver(Interpreter, scope.InterpreterScope);
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
                .Select(m => new Directive(new Complex(new("use_module"), m)))
                .ToArray();
            var text = new ErgoProgram(dirs, preds.ToArray()).Explain();
            File.WriteAllText(fileName, text);
            WriteLine($"Saved: '{fileName}'.", LogLevel.Inf);
        }

        public virtual void Load(ShellScope scope, string fileName)
        {
            var preds = GetInterpreterPredicates(scope);
            var oldPredicates = preds.Count();
            var interpreterScope = scope.InterpreterScope;
            if (scope.ExceptionHandler.Try(scope, () => Interpreter.Load(ref interpreterScope, fileName)))
            {
                var newPredicates = preds.Count();
                var delta = newPredicates - oldPredicates;
                WriteLine($"Loaded: '{fileName}'.\r\n\t{Math.Abs(delta)} {(delta >= 0 ? "new" : "")} predicates have been {(delta >= 0 ? "added" : "removed")}.", LogLevel.Inf);
            }
        }

        public virtual void EnterRepl(ref ShellScope scope, Func<string, bool> exit = null)
        {
            while(true) {
                Write($"{scope.InterpreterScope.Module.Explain()}> ");
                var prompt = Prompt();
                if (exit != null && exit(prompt)) break;
                Do(ref scope, prompt);
            }
        }

        public bool Do(ref ShellScope scope, string command)
        {
            var scope_ = scope;
            if(scope.ExceptionHandler.TryGet(scope, () => Dispatcher.Dispatch(this, ref scope_, command), out var success) && success)
            {
                scope = scope_;
                return true;
            }
            return false;
        }
    }
}
