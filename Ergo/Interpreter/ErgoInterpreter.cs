using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Solver.BuiltIns;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using Ergo.Lang.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Ergo.Solver;
using System.Collections.Immutable;
using Ergo.Interpreter.Directives;

namespace Ergo.Interpreter
{
    public partial class ErgoInterpreter
    {
        public readonly InterpreterFlags Flags;
        public readonly Dictionary<Signature, InterpreterDirective> Directives;

        public ErgoInterpreter(InterpreterFlags flags = InterpreterFlags.Default)
        {
            Flags = flags;
            Directives = new();
            AddDirectivesByReflection();
        }

        public InterpreterScope CreateScope()
        {
            var prologueScope = new InterpreterScope(new Module(Modules.Prologue, List.Empty, List.Empty, ImmutableArray<Operator>.Empty, ImmutableDictionary<Atom, Literal>.Empty, ErgoProgram.Empty(Modules.Prologue), runtime: true));
            var prologue = Load(ref prologueScope, Modules.Prologue.Explain());
            return new InterpreterScope(new Module(Modules.User, List.Empty, List.Empty, ImmutableArray<Operator>.Empty, ImmutableDictionary<Atom, Literal>.Empty, ErgoProgram.Empty(Modules.User), runtime: true)
                    .WithImport(Modules.Prologue))
                .WithModule(prologue);
        }

        public bool TryAddDirective(InterpreterDirective d) => Directives.TryAdd(d.Signature, d);

        protected void AddDirectivesByReflection()
        {
            var assembly = typeof(UseModule).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAssignableTo(typeof(InterpreterDirective))) continue;
                if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
                var inst = (InterpreterDirective)Activator.CreateInstance(type);
                Directives[inst.Signature] = inst;
            }
        }

        public virtual Module Load(ref InterpreterScope scope, string fileName)
        {
            var dir = scope.SearchDirectories.FirstOrDefault(
                d => File.Exists(Path.ChangeExtension(Path.Combine(d, fileName), "ergo"))
            );
            if (dir == null)
            {
                throw new FileNotFoundException(fileName);
            }
            fileName = Path.ChangeExtension(Path.Combine(dir, fileName), "ergo");
            var fs = FileStreamUtils.EncodedFileStream(File.OpenRead(fileName), closeStream: true);
            return Load(ref scope, fileName, fs, closeStream: true);
        }

        public virtual Module Load(ref InterpreterScope scope, string name, Stream file, bool closeStream = true)
        {
            var operators = scope.GetUserDefinedOperators();
            var lexer = new Lexer(file, operators, name);
            var parser = new Parser(lexer, operators);
            if (!parser.TryParseProgramDirectives(out var program))
            {
                MaybeClose();
                throw new InterpreterException(InterpreterError.CouldNotLoadFile);
            }
            foreach (var d in program.Directives)
            {
                RunDirective(ref scope, d);
            }
            foreach (var import in scope.Modules[scope.CurrentModule].Imports.Contents)
            {
                if (!scope.Modules.ContainsKey((Atom)import))
                {
                    var importScope = scope;
                    scope = scope.WithModule(Load(ref importScope, import.Explain()));
                }
            }
            var newOperators = scope.GetUserDefinedOperators()
                .Except(operators)
                .ToArray();
            if (newOperators.Length > 0)
            {
                operators = operators.Concat(newOperators).ToArray();
                file.Seek(0, SeekOrigin.Begin);
                lexer = new Lexer(file, operators, name);
                parser = new Parser(lexer, operators);
            }
            if (!parser.TryParseProgram(out program))
            {
                MaybeClose();
                throw new InterpreterException(InterpreterError.CouldNotLoadFile);
            }
            var currentModule = scope.Modules[scope.CurrentModule].WithProgram(program);
            foreach (Atom import in currentModule.Imports.Contents)
            {
                var importScope = scope.WithCurrentModule(import);
                if (!scope.Modules.ContainsKey(import))
                {
                    var module = Load(ref importScope, import.Explain());
                    if (module.Name != import)
                    {
                        throw new ArgumentException(module.Name.Explain());
                    }
                    scope = scope.WithModule(module);
                }
            }
            MaybeClose();
            scope = scope.WithModule(currentModule);
            return currentModule;
            void MaybeClose()
            {
                if (closeStream)
                {
                    file.Dispose();
                }
            }
        }

        public Module EnsureModule(ref InterpreterScope scope, Atom name)
        {
            if(!scope.Modules.TryGetValue(name, out var module))
            {
                try
                {
                    scope = scope.WithModule(module = Load(ref scope, name.Explain())
                        .WithImport(Modules.Prologue));
                }
                catch(FileNotFoundException)
                {
                    scope = scope.WithModule(module = new Module(name, List.Empty, List.Empty, ImmutableArray<Operator>.Empty, ImmutableDictionary<Atom, Literal>.Empty, ErgoProgram.Empty(name), runtime: true)
                        .WithImport(Modules.Prologue));
                }
            }
            return module;
        }

        public bool TryGetMatches(InterpreterScope scope, ITerm head, out IEnumerable<KnowledgeBase.Match> matches)
        {
            // if head is in the form predicate/arity (or its built-in equivalent),
            // do some syntactic de-sugaring and convert it into an actual anonymous complex
            if (head is Complex c
                && (new Atom[] { new("/"), new("@anon") }).Contains(c.Functor)
                && c.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
            {
                head = new Complex(new(match.Predicate), Enumerable.Range(0, match.Arity)
                    .Select(i => (ITerm)new Variable($"{i}"))
                    .ToArray());
            }
            return new ErgoSolver(this, scope).KnowledgeBase.TryGetMatches(head, out matches);
        }

        public virtual bool RunDirective(ref InterpreterScope scope, Directive d)
        {
            if(Directives.TryGetValue(d.Body.GetSignature(), out var directive))
            {
                return directive.Execute(this, ref scope, ((Complex)d.Body).Arguments);
            }
            // TODO: throw if flag says so
            return false;
        }

    }
}
