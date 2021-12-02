using Ergo.Lang;
using Ergo.Lang.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ergo.Lang
{
    public partial class Interpreter
    {
        protected readonly KnowledgeBase KnowledgeBase;
        protected readonly Dictionary<string, BuiltIn> BuiltInsDict;
        public IEnumerable<Predicate> Predicates => KnowledgeBase;
        public IEnumerable<BuiltIn> BuiltIns => BuiltInsDict.Values;

        public event Action<string> Trace;

        public Interpreter()
        {
            KnowledgeBase = new KnowledgeBase();
            BuiltInsDict = new Dictionary<string, BuiltIn>();
            AddBuiltins();
        }

        public bool TryGetMatches(Term head, out IEnumerable<KnowledgeBase.Match> matches) => KnowledgeBase.TryGetMatches(head, out matches);
        public bool TryGetBuiltIn(Term match, out BuiltIn builtin) => BuiltInsDict.TryGetValue(Predicate.Signature(match), out builtin);
        public void AssertA(Predicate p)
        {
            if (TryGetBuiltIn(p.Head, out _)) {
                throw new InterpreterException(ErrorType.UserPredicateConflictsWithBuiltIn, Predicate.Signature(p.Head));
            }
            KnowledgeBase.AssertA(p);
        }
        public void AssertZ(Predicate p)
        {
            if (TryGetBuiltIn(p.Head, out _)) {
                throw new InterpreterException(ErrorType.UserPredicateConflictsWithBuiltIn, Predicate.Signature(p.Head));
            }
            KnowledgeBase.AssertZ(p);
        }

        public bool RetractOne(Term head)
        {
            if (TryGetBuiltIn(head, out _)) {
                throw new InterpreterException(ErrorType.UnknownPredicate, head);
            }
            return KnowledgeBase.RetractOne(head);
        }

        public int RetractAll(Term head)
        {
            if (TryGetBuiltIn(head, out _)) {
                throw new InterpreterException(ErrorType.UnknownPredicate, head);
            }
            return KnowledgeBase.RetractAll(head);
        }

        public IEnumerable<Solver.Solution> Solve(Sequence goal, Solver.SolverFlags flags = Solver.SolverFlags.Default)
        {
            var solver = new Solver(KnowledgeBase, BuiltInsDict, flags);
            solver.Trace += HandleTrace;
            var solutions = solver.Solve(goal);
            return solutions;

            void HandleTrace(string msg) => Trace?.Invoke(msg);
        }

        public virtual void Parse(string code, string fileName = "")
        {
            var fs = FileStreamUtils.MemoryStream(code);
            Load(fileName, fs, closeStream: true);
        }

        public virtual void Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }
            var fs = FileStreamUtils.EncodedFileStream(File.OpenRead(fileName), closeStream: true);
            Load(fileName, fs, closeStream: true);
        }

        public virtual void Load(string name, Stream file, bool closeStream = true)
        {
            var lexer = new Lexer(file, name);
            var parser = new Parser(lexer);

            if (!parser.TryParseProgram(out var program)) {
                MaybeClose();
                throw new InterpreterException(ErrorType.CouldNotLoadFile);
            }

            foreach (var k in program.KnowledgeBank) {
                RetractAll(k.Head);
            }
            foreach (var k in program.KnowledgeBank)
            {
                AssertZ(k);
            }

            MaybeClose();

            void MaybeClose()
            {
                if (closeStream) {
                    file.Dispose();
                }
            }
        }
    }
}
