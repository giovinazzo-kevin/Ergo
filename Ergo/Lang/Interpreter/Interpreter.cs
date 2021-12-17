﻿using Ergo.Lang;
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
        public static readonly Atom PrologueModule = new("prologue");
        public static readonly Atom UserModule = new("user");

        public readonly Dictionary<Atom, Module> Modules;
        public readonly Dictionary<string, BuiltIn> BuiltInsDict;
        public readonly List<string> SearchDirectories;

        public IEnumerable<BuiltIn> BuiltIns => BuiltInsDict.Values;

        public event Action<string> Trace;



        public Interpreter()
        {
            BuiltInsDict = new();
            Modules = new();
            SearchDirectories = new() { "", "stdlib/" };
            Modules[UserModule] = new Module(UserModule, List.Build(), List.Build());
            Load(Atom.Explain(PrologueModule));
            AddBuiltins();
        }

        public bool TryGetMatches(Term head, out IEnumerable<KnowledgeBase.Match> matches) => Modules[UserModule].KnowledgeBase.TryGetMatches(head, out matches);
        public bool TryGetBuiltIn(Term match, out BuiltIn builtin) => BuiltInsDict.TryGetValue(Predicate.Signature(match), out builtin);
        public void AssertA(Atom module, Predicate p)
        {
            if (TryGetBuiltIn(p.Head, out _)) {
                throw new InterpreterException(ErrorType.UserPredicateConflictsWithBuiltIn, Predicate.Signature(p.Head));
            }
            Modules[module].KnowledgeBase.AssertA(p);
        }
        public void AssertZ(Atom module, Predicate p)
        {
            if (TryGetBuiltIn(p.Head, out _)) {
                throw new InterpreterException(ErrorType.UserPredicateConflictsWithBuiltIn, Predicate.Signature(p.Head));
            }
            Modules[module].KnowledgeBase.AssertZ(p);
        }

        public bool RetractOne(Atom module, Term head)
        {
            if (TryGetBuiltIn(head, out _)) {
                throw new InterpreterException(ErrorType.UnknownPredicate, head);
            }
            return Modules[module].KnowledgeBase.RetractOne(head);
        }

        public int RetractAll(Atom module, Term head)
        {
            if (TryGetBuiltIn(head, out _)) {
                throw new InterpreterException(ErrorType.UnknownPredicate, head);
            }
            return Modules[module].KnowledgeBase.RetractAll(head);
        }

        public Solver GetSolver(Solver.SolverFlags flags = Solver.SolverFlags.Default) => new Solver(Modules, BuiltInsDict, flags);

        public IEnumerable<Solver.Solution> Solve(Sequence goal, Solver.SolverFlags flags = Solver.SolverFlags.Default)
        {
            var solver = GetSolver(flags);
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

        public virtual Module Load(string fileName)
        {
            var dir = SearchDirectories.FirstOrDefault(
                d => File.Exists(Path.ChangeExtension(Path.Combine(d, fileName), "ergo"))
            );
            if(dir == null)
            {
                throw new FileNotFoundException(fileName);
            }
            fileName = Path.ChangeExtension(Path.Combine(dir, fileName), "ergo");
            var fs = FileStreamUtils.EncodedFileStream(File.OpenRead(fileName), closeStream: true);
            return Load(fileName, fs, closeStream: true);
        }

        public virtual bool RunDirective(Directive d, ref Module currentModule)
        {
            if (Substitution.TryUnify(new(d.Body, Directives.Module.Body), out _))
            {
                return Module(ref currentModule);
            }
            if (Substitution.TryUnify(new(d.Body, Directives.UseModule.Body), out _))
            {
                return UseModule(ref currentModule);
            }
            return false;

            bool Module(ref Module currentModule)
            {
                var body = ((Complex)d.Body);
                // first arg: module name; second arg: export list
                var moduleName = body.Arguments[0]
                    .Reduce(a => a, v => throw new ArgumentException(), c => throw new ArgumentException());
                if (currentModule.Name != UserModule)
                {
                    throw new InterpreterException(ErrorType.ModuleRedefinition, moduleName, currentModule.Name);
                }
                if (Modules.TryGetValue(moduleName, out var module))
                {
                    throw new InterpreterException(ErrorType.ModuleNameClash, Atom.Explain(moduleName));
                }
                var exports = body.Arguments[1].Reduce(
                    a => a.Equals(List.EmptyLiteral) ? List.Build() : throw new ArgumentException(),
                    v => throw new ArgumentException(),
                    c => List.TryUnfold(c, out var l) ? l : List.Build()
                );
                var P = new Variable("P");
                var A = new Variable("A");
                var predicateSlashArity = new Expression(Operators.BinaryDivision, P, Maybe<Term>.Some(A)).Complex;
                foreach (var item in exports.Head.Contents)
                {
                    // make sure that 'item' is in the form 'predicate/arity'
                    if(!Substitution.TryUnify(new(predicateSlashArity, item), out var subs))
                    {
                        throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.PredicateIndicator, Term.Explain(item));
                    }
                    var predicate = subs.Single(x => x.Lhs == P).Rhs;
                    var arity = subs.Single(x => x.Lhs == A).Rhs;
                    if(predicate.Type != TermType.Atom || arity.Type != TermType.Atom || ((Atom)arity).Value is not double d)
                    {
                        throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, BuiltIn.Types.PredicateIndicator, Term.Explain(item));
                    }
                }
                Modules[moduleName] = new(moduleName, List.Build(), exports);
                currentModule = Modules[moduleName];
                return true;
            }
            bool UseModule(ref Module currentModule)
            {
                var body = ((Complex)d.Body);
                // first arg: module name
                var moduleName = body.Arguments[0]
                    .Reduce(a => a, v => throw new ArgumentException(), c => throw new ArgumentException());
                if(moduleName == currentModule.Name)
                {
                    return false;
                }
                if(!Modules.ContainsKey(moduleName))
                {
                    Load(Atom.Explain(moduleName));
                }
                currentModule = Modules[currentModule.Name] = currentModule.WithImport(moduleName);
                return true;
            }
        }

        public virtual Module Load(string name, Stream file, bool closeStream = true)
        {
            var lexer = new Lexer(file, name);
            var parser = new Parser(lexer);
            if (!parser.TryParseProgram(out var program)) {
                MaybeClose();
                throw new InterpreterException(ErrorType.CouldNotLoadFile);
            }
            var currentModule = Modules[UserModule];
            foreach (var d in program.Directives)
            {
                RunDirective(d, ref currentModule);
            }
            foreach (var item in currentModule.Imports.Head.Contents)
            {
                var import = item.Reduce(a => a, v => throw new ArgumentException(), c => throw new ArgumentException());
                if(!Modules.TryGetValue(import, out var module))
                {
                    module = Load(Atom.Explain(import));
                    if (module.Name != import)
                    {
                        throw new ArgumentException(Atom.Explain(module.Name));
                    }
                }
            }
            foreach (var k in program.KnowledgeBank) {
                RetractAll(currentModule.Name, k.Head);
            }
            foreach (var k in program.KnowledgeBank)
            {
                AssertZ(currentModule.Name, k);
            }
            MaybeClose();
            return currentModule;
            void MaybeClose()
            {
                if (closeStream) {
                    file.Dispose();
                }
            }
        }
    }
}
