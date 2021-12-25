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

        private volatile bool _repl;
        private volatile bool _trace;
        public bool TraceMode
        {
            get => _trace;
            set => _trace = value;
        }
        private volatile bool _throw;
        public bool ThrowUnhandledExceptions {
            get => _throw;
            set => _throw = value;
        }

        public ShellScope CreateScope() => new(Interpreter.CreateScope().WithRuntime(true), DefaultExceptionHandler);

        public Parsed<T> Parse<T>(ShellScope scope, string data, Func<string, T> onParseFail = null)
        {
            onParseFail ??= (str => throw new ShellException($"Could not parse '{data}' as {typeof(T).Name}"));
            var userDefinedOps = scope.InterpreterScope.GetUserDefinedOperators().ToArray();
            return new Parsed<T>(data, scope.ExceptionHandler, onParseFail, userDefinedOps);
        }

        // TODO: Extensions
        public IEnumerable<Predicate> GetInterpreterPredicates(ShellScope scope) => new ErgoSolver(Interpreter, scope.InterpreterScope).KnowledgeBase.AsEnumerable();
        public IEnumerable<Predicate> GetUserPredicates(ShellScope scope) => scope.InterpreterScope.Modules[Modules.User].Program.KnowledgeBase.AsEnumerable();

        public ErgoShell(ErgoInterpreter interpreter = null, Func<LogLine, string> formatter = null)
        {
            Interpreter = interpreter ?? new();
            Dispatcher = new CommandDispatcher(s => WriteLine($"Unknown command: {s}", LogLevel.Err));
            LineFormatter = formatter ?? DefaultLineFormatter;
            DefaultExceptionHandler = new ExceptionHandler(ex => {
                WriteLine(ex.Message, LogLevel.Err);
                if (_throw && !(ex is ShellException || ex is InterpreterException || ex is ParserException || ex is LexerException)) {
                    throw ex;
                }
            });

            AddCommandsByReflection();
            SetConsoleOutputCP(65001);
            SetConsoleCP(65001);
            Clear();
#if DEBUG
            ThrowUnhandledExceptions = true;
#endif
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
            var module = scope.InterpreterScope.Modules[scope.InterpreterScope.CurrentModule];
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
            if (scope.ExceptionHandler.Try(() => Interpreter.Load(ref interpreterScope, fileName)))
            {
                var newPredicates = preds.Count();
                var delta = newPredicates - oldPredicates;
                WriteLine($"Loaded: '{fileName}'.\r\n\t{Math.Abs(delta)} {(delta >= 0 ? "new" : "")} predicates have been {(delta >= 0 ? "added" : "removed")}.", LogLevel.Inf);
            }
        }

        public virtual void EnterRepl(ref ShellScope scope)
        {
            _repl = true;
            do {
                Write($"{scope.InterpreterScope.CurrentModule.Explain()}> ");
                Do(ref scope, Prompt());
            }
            while (_repl);
        }

        public bool Do(ref ShellScope scope, string command)
        {
            var scope_ = scope;
            if(scope.ExceptionHandler.TryGet(() => Dispatcher.Dispatch(this, ref scope_, command), out var success) && success)
            {
                scope = scope_;
                return true;
            }
            return false;
        }

        public virtual void ExitRepl()
        {
            _repl = false;
        }
    }
}
