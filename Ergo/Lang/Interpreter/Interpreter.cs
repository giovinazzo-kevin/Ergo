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
        public readonly Dictionary<BuiltInSignature, BuiltIn> BuiltInsDict;
        public readonly List<string> SearchDirectories;

        public IEnumerable<BuiltIn> BuiltIns => BuiltInsDict.Values;

        public event Action<Solver.TraceType, string> Trace;

        protected void InitializeModules()
        {
            Modules.Clear();
            Modules[UserModule] = new Module(UserModule, List.Empty, List.Empty, Array.Empty<Operator>(), runtime: true);
            Load(PrologueModule.Explain());
        }

        public Interpreter(InterpreterFlags flags = InterpreterFlags.Default)
        {
            BuiltInsDict = new();
            Modules = new();
            SearchDirectories = new() { "", "stdlib/" };
            Flags = flags;
            InitializeModules();
            AddReflectedBuiltIns();
        }

        protected void AddReflectedBuiltIns()
        {
            var assembly = typeof(BuiltIns.Print).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAssignableTo(typeof(BuiltIn))) continue;
                if(!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
                var inst = (BuiltIn)Activator.CreateInstance(type);
                BuiltInsDict[inst.Signature] = inst;
            }
        }

        public bool TryGetMatches(ITerm head, Atom module, out IEnumerable<KnowledgeBase.Match> matches)
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
            return new Solver(module, Modules, BuiltInsDict).KnowledgeBase.TryGetMatches(head, out matches);
        }
        public bool TryGetBuiltIn(ITerm match, out BuiltIn builtin) => BuiltInsDict.TryGetValue(match.GetBuiltInSignature(), out builtin);
        protected Module EnsureModule(Atom name)
        {
            if(!Modules.TryGetValue(name, out var module))
            {
                try
                {
                    Load(name.Explain());
                    module = Modules[name];
                }
                catch(FileNotFoundException)
                {
                    Modules[name] = module = new(name, List.Empty, List.Empty, Array.Empty<Operator>(), runtime: true);
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

        public bool RetractOne(Atom module, ITerm head)
        {
            if (TryGetBuiltIn(head, out _)) {
                throw new InterpreterException(ErrorType.UnknownPredicate, head);
            }
            return Modules[module].KnowledgeBase.RetractOne(head);
        }

        public int RetractAll(Atom module, ITerm head)
        {
            if (TryGetBuiltIn(head, out _)) {
                throw new InterpreterException(ErrorType.UnknownPredicate, head);
            }
            return Modules[module].KnowledgeBase.RetractAll(head);
        }

        public Solver GetSolver(Atom entryModule, Solver.SolverFlags flags = Solver.SolverFlags.Default) => new Solver(entryModule, Modules, BuiltInsDict, flags);

        public IEnumerable<Solver.Solution> Solve(Query goal, Maybe<Atom> entryModule = default, Solver.SolverFlags flags = Solver.SolverFlags.Default)
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
            if (new Substitution(d.Body, Directives.ChooseModule.Body).TryUnify(out _))
            {
                return ChooseModule(ref currentModule);
            }
            if (new Substitution(d.Body, Directives.DefineModule.Body).TryUnify(out _))
            {
                return DefineModule(ref currentModule);
            }
            if (new Substitution(d.Body, Directives.UseModule.Body).TryUnify(out _))
            {
                return UseModule(ref currentModule);
            }
            if (new Substitution(d.Body, Directives.DefineOperator.Body).TryUnify(out _))
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
                    "fx" => (OperatorAffix.Prefix, OperatorAssociativity.Right),
                    "xf" => (OperatorAffix.Postfix, OperatorAssociativity.Left),
                    "xfx" => (OperatorAffix.Infix, OperatorAssociativity.None),
                    "xfy" => (OperatorAffix.Infix, OperatorAssociativity.Right),
                    "yfx" => (OperatorAffix.Infix, OperatorAssociativity.Left),
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
                var moduleName = (Atom)body.Arguments[0];
                InitializeModules(); // Clear modules and re-scope into the current module
                currentModule = EnsureModule(moduleName);
                return true;
            }
 
            bool DefineModule(ref Module currentModule)
            {
                var body = ((Complex)d.Body);
                // first arg: module name; second arg: export list
                var moduleName = (Atom)body.Arguments[0];
                if (!fromCli && currentModule.Name != UserModule)
                {
                    throw new InterpreterException(ErrorType.ModuleRedefinition, currentModule.Name.Explain(), moduleName.Explain());
                }
                var exports = List.Empty;
                if(body.Arguments[1] is Complex c)
                {
                    List.TryUnfold(c, out exports);
                }

                if (Modules.TryGetValue(moduleName, out var module))
                {
                    if (!module.Runtime && !Flags.HasFlag(InterpreterFlags.AllowStaticModuleRedefinition))
                    {
                        throw new InterpreterException(ErrorType.ModuleNameClash, moduleName.Explain());
                    }
                    module = module.WithExports(exports.Contents);
                }
                else
                {
                    module = new Module(moduleName, List.Empty, exports, Array.Empty<Operator>());
                }
                currentModule = Modules[moduleName] = module;
                var P = new Variable("P");
                var A = new Variable("A");
                var predicateSlashArity = new Expression(Operators.BinaryDivision, P, Maybe<ITerm>.Some(A)).Complex;
                foreach (var item in exports.Contents)
                {
                    // make sure that 'item' is in the form 'predicate/arity', and that it is asserted
                    if(!new Substitution(predicateSlashArity, item).TryUnify(out var subs))
                    {
                        throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, Types.PredicateIndicator, item.Explain());
                    }
                    var predicate = subs.Single(x => x.Lhs.Equals(P)).Rhs;
                    var arity = subs.Single(x => x.Lhs.Equals(A)).Rhs;
                    if(predicate is not Atom || arity is not Atom || ((Atom)arity).Value is not double d)
                    {
                        throw new InterpreterException(ErrorType.ExpectedTermOfTypeAt, Types.PredicateIndicator, item.Explain());
                    }
                }
                return true;
            }
            bool UseModule(ref Module currentModule)
            {
                var body = ((Complex)d.Body);
                // first arg: module name
                var moduleName = (Atom)body.Arguments[0];
                if(moduleName == currentModule.Name)
                {
                    return false;
                }
                if(!Modules.ContainsKey(moduleName))
                {
                    Load(moduleName.Explain());
                }
                currentModule = Modules[currentModule.Name] = currentModule.WithImport(moduleName);
                return true;
            }
        }

        public IEnumerable<Operator> GetUserDefinedOperators(Atom entryModule)
        {
            var module = EnsureModule(entryModule);
            foreach (var import in module.Imports.Contents)
            {
                foreach (var importedOp in GetUserDefinedOperators((Atom)import))
                {
                    if (!Modules[(Atom)import].Exports.Contents.Any(t =>
                     {
                         var x = TermMarshall.FromTerm(t, new
                         { Predicate = default(string), Arity = default(int) },
                             TermMarshall.MarshallingMode.Positional
                         );
                         return importedOp.Synonyms.Any(s => Equals(s.Value, x.Predicate))
                         && (x.Arity == 1 && importedOp.Affix != OperatorAffix.Infix
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
            foreach (var item in currentModule.Imports.Contents)
            {
                var import = (Atom)item;
                if(!Modules.TryGetValue(import, out var module))
                {
                    module = Load(import.Explain(), entryModule: Maybe.Some(currentModule.Name));
                    if (module.Name != import)
                    {
                        throw new ArgumentException(module.Name.Explain());
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
