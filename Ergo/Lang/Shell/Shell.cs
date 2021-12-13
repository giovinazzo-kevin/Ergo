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
        public string PromptTag { get; set; }

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
                void HandleTrace(string trace)
                {
                    WriteLine(trace, LogLevel.Trc);
                }
            }
        }
        private volatile bool _throw;
        public bool ThrowUnhandledExceptions {
            get => _throw;
            set => _throw = value;
        }

        protected IEnumerable<Predicate> GetInterpreterPredicates() => Interpreter.GetSolver().KnowledgeBase.AsEnumerable();

        public Shell(Interpreter interpreter = null, string prompt = "ergo> ", Func<LogLine, string> formatter = null)
        {
            PromptTag = prompt;
            Interpreter = interpreter ?? new();
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
            var preds = GetInterpreterPredicates();
            if (File.Exists(fileName) && !force)
            {
                WriteLine($"File already exists: {fileName}", LogLevel.Err);
                return;
            }
            var text = Program.Explain(new Program(Array.Empty<Directive>(), preds.ToArray()));
            File.WriteAllText(fileName, text);
            WriteLine($"Saved: '{fileName}'.", LogLevel.Inf);
        }

        public virtual void Parse(string code, string fileName = "")
        {
            var preds = GetInterpreterPredicates();
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
            var preds = GetInterpreterPredicates();
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
            return Dispatcher.Dispatch(command);
        }

        public virtual void ExitRepl()
        {
            _repl = false;
        }
    }
}
