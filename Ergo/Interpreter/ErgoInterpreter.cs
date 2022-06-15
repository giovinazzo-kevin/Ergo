using Ergo.Interpreter.Directives;
using Ergo.Lang.Parser;
using Ergo.Solver;
using System.IO;

namespace Ergo.Interpreter;

public partial class ErgoInterpreter
{
    public readonly InterpreterFlags Flags;
    public readonly Dictionary<Signature, InterpreterDirective> Directives = new();
    public readonly Dictionary<Signature, HashSet<DynamicPredicate>> DynamicPredicates = new();

    public readonly InterpreterScope StdlibScope;

    public readonly Action<ErgoParser> ConfigureParser;

    public Parsed<T> Parse<T>(InterpreterScope scope, string data, Func<string, Maybe<T>> onParseFail = null)
    {
        var userDefinedOps = scope.Operators.Value;
        return new Parsed<T>(data, ConfigureParser, onParseFail, userDefinedOps);
    }

    public ErgoInterpreter(InterpreterFlags flags = InterpreterFlags.Default, Action<ErgoParser> configureParser = null)
    {
        Flags = flags;
        ConfigureParser = parser =>
        {
            parser.AbstractTermParsers.Add(new DictParser());
            configureParser?.Invoke(parser);
        };
        AddDirectivesByReflection();

        var stdlibScope = new InterpreterScope(new Module(Modules.Stdlib, runtime: true));
        Load(ref stdlibScope, Modules.Stdlib.Explain());
        StdlibScope = stdlibScope
            .WithCurrentModule(Modules.Stdlib)
            .WithRuntime(false);
    }

    public InterpreterScope CreateScope()
    {
        var scope = StdlibScope
            .WithModule(new Module(Modules.User, runtime: true)
                .WithImport(Modules.Stdlib))
            .WithCurrentModule(Modules.User);
        return scope;
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

    public bool TryAddDynamicPredicate(DynamicPredicate d)
    {
        if (!DynamicPredicates.TryGetValue(d.Signature, out var hashSet))
        {
            hashSet = DynamicPredicates[d.Signature] = new() { };
        }

        hashSet.Add(d);
        return true;
    }

    public bool TryRemoveDynamicPredicate(DynamicPredicate d)
    {
        if (!DynamicPredicates.TryGetValue(d.Signature, out var hashSet))
        {
            return false;
        }

        hashSet.Remove(d);
        return true;
    }

    public Module Load(ref InterpreterScope scope, string name) => ModuleLoader.Load(this, ref scope, name);
    public Module Load(ref InterpreterScope scope, Stream stream, string fn = "") => ModuleLoader.Load(this, ref scope, fn, stream);

    public Module EnsureModule(ref InterpreterScope scope, Atom name)
    {
        if (!scope.Modules.TryGetValue(name, out var module))
        {
            try
            {
                scope = scope
                    .WithModule(module = Load(ref scope, name.Explain()));
            }
            catch (FileNotFoundException)
            {
                scope = scope
                    .WithModule(module = new Module(name, runtime: true));
            }
        }

        return module;
    }

    public IEnumerable<KnowledgeBase.Match> GetMatches(ref InterpreterScope scope, ITerm head)
    {
        // if head is in the form predicate/arity (or its built-in equivalent),
        // do some syntactic de-sugaring and convert it into an actual anonymous complex
        if (head is Complex c
            && WellKnown.Functors.Division.Contains(c.Functor)
            && c.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
        {
            head = new Atom(match.Predicate).BuildAnonymousTerm(match.Arity);
        }
        // if head matches the signature of any data source, 
        return SolverBuilder.Build(this, ref scope).KnowledgeBase
            .GetMatches(head);
    }

    public virtual bool RunDirective(ref InterpreterScope scope, Directive d)
    {
        if (Directives.TryGetValue(d.Body.GetSignature(), out var directive))
        {
            return directive.Execute(this, ref scope, ((Complex)d.Body).Arguments);
        }

        if (Flags.HasFlag(InterpreterFlags.ThrowOnDirectiveNotFound))
        {
            throw new InterpreterException(InterpreterError.UndefinedDirective, scope, d.Explain(canonical: false));
        }

        return false;
    }

    public void LoadScope(ref InterpreterScope scope, KnowledgeBase kb)
    {
        kb.Clear();
        var added = new HashSet<Atom>();
        LoadModule(ref scope, scope.Modules[scope.Module], added);
        foreach (var module in scope.Modules.Values)
        {
            LoadModule(ref scope, module, added);
        }

        void LoadModule(ref InterpreterScope scope, Module module, HashSet<Atom> added)
        {
            if (added.Contains(module.Name))
                return;
            added.Add(module.Name);
            foreach (var subModule in module.Imports.Contents.Select(c => (Atom)c))
            {
                if (added.Contains(subModule))
                    continue;
                if (!scope.Modules.TryGetValue(subModule, out var import))
                {
                    var importScope = scope;
                    scope = scope.WithModule(import = Load(ref importScope, subModule.Explain()));
                }

                LoadModule(ref scope, import, added);
            }

            foreach (var pred in module.Program.KnowledgeBase)
            {
                var sig = pred.Head.GetSignature();
                if (module.Name == scope.Module || module.ContainsExport(sig))
                {
                    kb.AssertZ(pred.WithModuleName(module.Name));
                }
                else
                {
                    kb.AssertZ(pred.WithModuleName(module.Name).Qualified());
                }
            }

            foreach (var key in DynamicPredicates.Keys.Where(k => k.Module.Reduce(some => some, () => Modules.User) == module.Name))
            {
                foreach (var dyn in DynamicPredicates[key])
                {
                    if (!dyn.AssertZ)
                    {
                        kb.AssertA(dyn.Predicate);
                    }
                    else
                    {
                        kb.AssertZ(dyn.Predicate);
                    }
                }
            }
        }
    }

}
