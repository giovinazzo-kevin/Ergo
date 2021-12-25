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
        public event Action<SolverTraceType, string> Trace;
        public readonly Dictionary<Signature, InterpreterDirective> Directives;

        public ErgoInterpreter(InterpreterFlags flags = InterpreterFlags.Default)
        {
            Flags = flags;
            Directives = new();
            AddDirectivesByReflection();
        }

        public InterpreterScope CreateScope() => new(new(Modules.User, List.Empty, List.Empty, Array.Empty<Operator>(), ErgoProgram.Empty(Modules.User), runtime: true));

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
            var currentModule = scope.Modules[scope.CurrentModule];
            var operators = scope.GetUserDefinedOperators().ToArray();
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
            currentModule = currentModule.WithProgram(program);
            foreach (Atom import in currentModule.Imports.Contents)
            {
                var importScope = scope.WithCurrentModule(import);
                if (!scope.Modules.TryGetValue(import, out var module))
                {
                    module = Load(ref importScope, import.Explain());
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

        public bool TryGetMatches(InterpreterScope scope, ITerm head, out IEnumerable<KnowledgeBase.Match> matches)
        {
            //var operators = GetUserDefinedOperators(module).ToArray();
            //// if head is in the form predicate/arity (or its built-in equivalent),
            //// do some syntactic de-sugaring and convert it into an actual anonymous complex
            //if(ITerm.TryUnify(head, "/(Predicate, Arity)", out _, out var subs, operators)
            //|| ITerm.TryUnify(head, "@anon(Predicate, Arity)", out _, out subs, operators))
            //{
            //    var anon = ITerm.Substitute("@anon(Predicate, Arity)", subs, out _, operators);
            //    try { head = BuiltIn_AnonymousComplex(anon, module).Result; } catch(Exception) { }
            //}
            return new ErgoSolver(scope).KnowledgeBase.TryGetMatches(head, out matches);
        }

        public Module EnsureModule(ref InterpreterScope scope, Atom name)
        {
            if(!scope.Modules.TryGetValue(name, out var module))
            {
                try
                {
                    scope = scope.WithModule(module = Load(ref scope, name.Explain()));
                }
                catch(FileNotFoundException)
                {
                    scope = scope.WithModule(module = new(name, List.Empty, List.Empty, Array.Empty<Operator>(), ErgoProgram.Empty(name), runtime: true));
                }
            }
            return module;
        }

        public IEnumerable<Solution> Solve(ref InterpreterScope scope, Query goal, SolverFlags flags = SolverFlags.Default)
        {
            var solver = new ErgoSolver(scope, flags);
            solver.Trace += HandleTrace;
            var solutions = solver.Solve(goal);
            return solutions;

            void HandleTrace(SolverTraceType type, string msg) => Trace?.Invoke(type, msg);
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
