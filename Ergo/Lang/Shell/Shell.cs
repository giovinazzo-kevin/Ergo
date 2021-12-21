using Ergo.Lang.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ergo.Lang
{
    public partial class Shell
    {
        protected readonly ExceptionHandler Handler;
        public readonly Interpreter Interpreter;
        public readonly CommandDispatcher Dispatcher;
        public Func<LogLine, string> LineFormatter { get; set; }
        public Atom CurrentModule { get; private set; }

        private volatile bool _repl;

        private volatile bool _trace;
        public bool TraceMode {
            get => _trace;
            set {
                if (_trace = value) {
                    Interpreter.Trace += HandleTrace;
                }
                else {
                    Interpreter.Trace -= HandleTrace;
                }
                void HandleTrace(Solver.TraceType type, string trace)
                {
                    WriteLine(trace, LogLevel.Trc, type);
                }
            }
        }
        private volatile bool _throw;
        public bool ThrowUnhandledExceptions {
            get => _throw;
            set => _throw = value;
        }

        protected IEnumerable<Predicate> GetInterpreterPredicates(Maybe<Atom> entryModule = default) => Interpreter
            .GetSolver(entryModule.Reduce(some => some, () => Interpreter.UserModule))
            .KnowledgeBase.AsEnumerable();
        protected IEnumerable<Predicate> GetUserPredicates() => Interpreter.Modules[Interpreter.UserModule].KnowledgeBase.AsEnumerable();

        public Shell(Interpreter interpreter = null, Func<LogLine, string> formatter = null)
        {
            Interpreter = interpreter ?? new();
            CurrentModule = Interpreter.UserModule;
            Dispatcher = new CommandDispatcher(s => WriteLine($"Unknown command: {s}", LogLevel.Err));
            LineFormatter = formatter ?? DefaultLineFormatter;
            Handler = new ExceptionHandler(ex => {
                WriteLine(ex.Message, LogLevel.Err);
                if (_throw && !(ex is ShellException || ex is InterpreterException || ex is ParserException || ex is LexerException)) {
                    throw ex;
                }
            });
            InitializeCommandDispatcher();
            SetConsoleOutputCP(65001);
            SetConsoleCP(65001);
            Clear();
#if DEBUG
            ThrowUnhandledExceptions = true;
#endif
        }

        public virtual void Clear()
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Clear();
        }

        public virtual void Save(string fileName, bool force = true)
        {
            var preds = GetUserPredicates();
            if (File.Exists(fileName) && !force)
            {
                WriteLine($"File already exists: {fileName}", LogLevel.Err);
                return;
            }
            var module = Interpreter.Modules[CurrentModule];
            // TODO: make it easier to save directives
            var dirs = module.Imports.Contents
                .Select(m => new Directive(string.Empty, new Complex(new("use_module"), m)))
                .ToArray();
            var text = new Program(dirs, preds.ToArray()).Explain();
            File.WriteAllText(fileName, text);
            WriteLine($"Saved: '{fileName}'.", LogLevel.Inf);
        }

        public virtual void Parse(string code, string fileName = "")
        {
            var preds = GetInterpreterPredicates(Maybe.Some(CurrentModule));
            var oldPredicates = preds.Count();
            if (Handler.Try(() => Interpreter.Parse(code, fileName)))
            {
                var newPredicates = preds.Count();
                var delta = newPredicates - oldPredicates;
                WriteLine($"Loaded: '{fileName}'.\r\n\t{Math.Abs(delta)} {(delta >= 0 ? "new" : "")} predicates have been {(delta >= 0 ? "added" : "removed")}.", LogLevel.Inf);
            }
        }

        public virtual void Load(string fileName)
        {
            var preds = GetInterpreterPredicates(Maybe.Some(CurrentModule));
            var oldPredicates = preds.Count();
            if (Handler.Try(() => Interpreter.Load(fileName)))
            {
                var newPredicates = preds.Count();
                var delta = newPredicates - oldPredicates;
                WriteLine($"Loaded: '{fileName}'.\r\n\t{Math.Abs(delta)} {(delta >= 0 ? "new" : "")} predicates have been {(delta >= 0 ? "added" : "removed")}.", LogLevel.Inf);
            }
        }

        public virtual void EnterRepl()
        {
            _repl = true;
            do {
                Do(Prompt());
            }
            while (_repl);
        }

        public bool Do(string command)
        {
            return Handler.TryGet(() => Dispatcher.Dispatch(command), out var success) && success;
        }

        public virtual void ExitRepl()
        {
            _repl = false;
        }
    }
}
