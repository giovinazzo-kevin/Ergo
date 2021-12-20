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
        public static readonly Atom PrologueModule = new("prologue");
        public static readonly Atom UserModule = new("user");

        public readonly InterpreterFlags Flags;
        public readonly Dictionary<Atom, Module> Modules;
        public readonly Dictionary<string, BuiltIn> BuiltInsDict;
        public readonly List<string> SearchDirectories;

        public IEnumerable<BuiltIn> BuiltIns => BuiltInsDict.Values;

        public event Action<Solver.TraceType, string> Trace;

        protected void InitializeModules()
        {
            Modules.Clear();
            Modules[UserModule] = new Module(UserModule, List.Build(), List.Build(), Array.Empty<Operator>(), runtime: true);
            Load(Atom.Explain(PrologueModule));
        }

        public Interpreter(InterpreterFlags flags = InterpreterFlags.Default)
        {
            BuiltInsDict = new();
            Modules = new();
            SearchDirectories = new() { "", "stdlib/" };
            InitializeModules();
            AddBuiltins();
        }

        public bool TryGetMatches(Term head, Atom module, out IEnumerable<KnowledgeBase.Match> matches)
        {
            var operators = GetUserDefinedOperators(module).ToArray();
            // if head is in the form predicate/arity (or its built-in equivalent),
            // do some syntactic de-sugaring and convert it into an actual anonymous complex
            if(Term.TryUnify(head, "/(Predicate, Arity)", out _, out var subs, operators)
            || Term.TryUnify(head, "@anon(Predicate, Arity)", out _, out subs, operators))
            {
                var anon = Term.Substitute("@anon(Predicate, Arity)", subs, out _, operators);
                try { head = BuiltIn_AnonymousComplex(anon, module).Result; } catch(Exception) { }
            }
            return new Solver(module, Modules, BuiltInsDict).KnowledgeBase.TryGetMatches(head, out matches);
        }
        public bool TryGetBuiltIn(Term match, out BuiltIn builtin) => BuiltInsDict.TryGetValue(Predicate.Signature(match), out builtin);
        protected Module EnsureModule(Atom name)
        {
            if(!Modules.TryGetValue(name, out var module))
            {
                try
                {
                    Load(Atom.Explain(name));
                    module = Modules[name];
                }
                catch(FileNotFoundException)
                {
                    Modules[name] = module = new(name, List.Build(), List.Build(), Array.Empty<Operator>(), runtime: true);
                }
            }
            return module;
        }

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

        public Solver GetSolver(Atom entryModule, Solver.SolverFlags flags = Solver.SolverFlags.Default) => new Solver(entryModule, Modules, BuiltInsDict, flags);

        public IEnumerable<Solver.Solution> Solve(Sequence goal, Maybe<Atom> entryModule = default, Solver.SolverFlags flags = Solver.SolverFlags.Default)
        {
            var module = entryModule.Reduce(some => some, () => UserModule);
            var solver = GetSolver(module, flags);
            solver.Trace += HandleTrace;
            var solutions = solver.Solve(goal);
            return solutions;

            void HandleTrace(Solver.TraceType type, string msg) => Trace?.Invoke(type, msg);
        }

        public virtual void Parse(string code, string fileName = "")
        {
            var fs = FileStreamUtils.MemoryStream(code);
            Load(fileName, fs, closeStream: true);
        }

        public virtual Module Load(string fileName, Maybe<Atom> entryModule = default)
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
            return Load(fileName, fs, closeStream: true, entryModule);
        }

        public virtual bool RunDirective(Directive d, ref Module currentModule, bool fromCli = false)
        {
            if (Term.TryUnify(d.Body, Directives.ChooseModule.Body, out _))
            {
                return ChooseModule(ref currentModule);
            }
            if (Term.TryUnify(d.Body, Directives.DefineModule.Body, out _))
            {
                return DefineModule(ref currentModule);
            }
            if (Term.TryUnify(d.Body, Directives.UseModule.Body, out _))
            {
                return UseModule(ref currentModule);
            }
            if (Term.TryUnify(d.Body, Directives.DefineOperator.Body, out _))
            {
                return DefineOperator(ref currentModule);
            }
            return false;

            bool DefineOperator(ref Module currentModule)
            {
                // first arg: precedence; second arg: type; third arg: name
                var op = TermMarshall.FromTerm(d.Body, new
                    { Precedence = default(int), Type = default(string), Name = default(string) },
                    TermMarshall.MarshallingMode.Positional
                );

                var (affix, assoc) = op.Type switch
                {
                    "fx" => (Operator.AffixType.Prefix, Operator.AssociativityType.Right),
                    "xf" => (Operator.AffixType.Postfix, Operator.AssociativityType.Left),
                    "xfx" => (Operator.AffixType.Infix, Operator.AssociativityType.None),
                    "xfy" => (Operator.AffixType.Infix, Operator.AssociativityType.Right),
                    "yfx" => (Operator.AffixType.Infix, Operator.AssociativityType.Left),
                    _ => throw new NotSupportedException()
                };

                currentModule = Modules[currentModule.Name] = currentModule.WithOperators(currentModule.Operators
                    .Append(new Operator(affix, assoc, op.Precedence, op.Name))
                    .ToArray());

                return true;
            }

            bool ChooseModule(ref Module currentModule)
            {
                var body = ((Complex)d.Body);
                // first arg: module name; second arg: export list
                var moduleName = body.Arguments[0]
                    .Reduce(a => a, v => throw new ArgumentException(), c => throw new ArgumentException());
                InitializeModules(); // Clear modules and re-scope into the current module
                currentModule = EnsureModule(moduleName);
                return true;
            }
 
            bool DefineModule(ref Module currentModule)
            {
                var body = ((Complex)d.Body);
                // first arg: module name; second arg: export list
                var moduleName = body.Arguments[0]
                    .Reduce(a => a, v => throw new ArgumentException(), c => throw new ArgumentException());
                if (!fromCli && currentModule.Name != UserModule)
                {
                    throw new InterpreterException(ErrorType.ModuleRedefinition, Atom.Explain(currentModule.Name), Atom.Explain(moduleName));
                }
                var exports = body.Arguments[1].Reduce(
                    a => a.Equals(List.EmptyLiteral) ? List.Build() : throw new ArgumentException(),
                    v => throw new ArgumentException(),
                    c => List.TryUnfold(c, out var l) ? l : List.Build()
                );
                if (Modules.TryGetValue(moduleName, out var module))
                {
                    if (!module.Runtime && !Flags.HasFlag(InterpreterFlags.AllowStaticModuleRedefinition))
                    {
                        throw new InterpreterException(ErrorType.ModuleNameClash, Atom.Explain(moduleName));
                    }
                    module = module.WithExports(exports.Head.Contents);
                }
                else
                {
                    module = new Module(moduleName, List.Build(), exports, Array.Empty<Operator>());
                }
                currentModule = Modules[moduleName] = module;
                var P = new Variable("P");
                var A = new Variable("A");
                var predicateSlashArity = new Expression(Operators.BinaryDivision, P, Maybe<Term>.Some(A)).Complex;
                foreach (var item in exports.Head.Contents)
                {
                    // make sure that 'item' is in the form 'predicate/arity', and that it is asserted
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

        public IEnumerable<Operator> GetUserDefinedOperators(Atom entryModule)
        {
            var module = EnsureModule(entryModule);
            foreach (var import in module.Imports.Head.Contents)
            {
                foreach (var importedOp in GetUserDefinedOperators((Atom)import))
                {
                    if (!Modules[(Atom)import].Exports.Head.Contents.Any(t =>
                     {
                         var x = TermMarshall.FromTerm(t, new
                         { Predicate = default(string), Arity = default(int) },
                             TermMarshall.MarshallingMode.Positional
                         );
                         return importedOp.Synonyms.Any(s => Equals(s.Value, x.Predicate))
                         && (x.Arity == 1 && importedOp.Affix != Operator.AffixType.Infix
                         || x.Arity == 2);
                     }))
                    {
                        continue;
                    }
                    yield return importedOp;
                }
            }
            foreach (var op in module.Operators)
            {
                yield return op;
            }
        }

        public virtual Module Load(string name, Stream file, bool closeStream = true, Maybe<Atom> entryModule = default)
        {
            var currentModule = Modules[entryModule.Reduce(some => some, () => UserModule)];
            var operators = GetUserDefinedOperators(currentModule.Name).ToArray();
            var lexer = new Lexer(file, operators, name);
            var parser = new Parser(lexer, operators);
            if (!parser.TryParseProgramDirectives(out var program))
            {
                MaybeClose();
                throw new InterpreterException(ErrorType.CouldNotLoadFile);
            }
            foreach (var d in program.Directives)
            {
                RunDirective(d, ref currentModule);
            }
            var newOperators = GetUserDefinedOperators(currentModule.Name)
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
                throw new InterpreterException(ErrorType.CouldNotLoadFile);
            }
            foreach (var item in currentModule.Imports.Head.Contents)
            {
                var import = item.Reduce(a => a, v => throw new ArgumentException(), c => throw new ArgumentException());
                if(!Modules.TryGetValue(import, out var module))
                {
                    module = Load(Atom.Explain(import), entryModule: Maybe.Some(currentModule.Name));
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
