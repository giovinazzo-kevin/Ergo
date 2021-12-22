using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ergo.Lang.Extensions;
using Ergo.Lang.Exceptions;

namespace Ergo.Lang
{

    public partial class Solver
    {
        protected readonly AsyncLocal<bool> Cut = new();

        public readonly KnowledgeBase KnowledgeBase;
        public readonly Atom EntryModule;

        protected readonly IReadOnlyDictionary<Atom, Module> Modules;
        protected readonly IReadOnlyDictionary<BuiltInSignature, BuiltIn> BuiltIns;
        protected readonly SolverFlags Flags;

        public event Action<TraceType, string> Trace;

        public Solver(Atom entryModule, IReadOnlyDictionary<Atom, Module> modules, IReadOnlyDictionary<BuiltInSignature, BuiltIn> builtins, SolverFlags flags = SolverFlags.Default)
        {
            EntryModule = entryModule;
            Modules = modules;
            Flags = flags;
            BuiltIns = builtins;
            KnowledgeBase = new KnowledgeBase();
            var added = new HashSet<Atom>();
            LoadModule(modules[entryModule], added);
            foreach (var module in Modules.Values)
            {
                LoadModule(module, added);
            }
            void LoadModule(Module module, HashSet<Atom> added)
            {
                if (added.Contains(module.Name))
                    return;
                added.Add(module.Name);
                foreach (var subModule in module.Imports.Contents.Select(c => (Atom)c))
                {
                    if (added.Contains(subModule))
                        continue;
                    LoadModule(Modules[subModule], added);
                }
                foreach (var pred in module.KnowledgeBase)
                {
                    var head = pred.Head;
                    if(pred.Head is Complex c)
                    {
                        head = c.Functor;
                    }
                    var predicateSlashArity = new Expression(Operators.BinaryDivision, head, Maybe<ITerm>.Some(new Atom((double)Predicate.Arity(pred.Head)))).Complex;
                    if(module.Name == Interpreter.UserModule 
                    || module.Exports.Contents.Any(t => (new Substitution(t, predicateSlashArity).TryUnify(out _))))
                    {
                        KnowledgeBase.AssertZ(pred.WithModuleName(module.Name));
                    }
                    else
                    {
                        KnowledgeBase.AssertZ(pred.WithModuleName(module.Name).Qualified());
                    }
                }
            }
        }

        private ITerm ResolveBuiltin(ITerm term, Scope scope, List<Substitution> subs, int depth, out BuiltInSignature sig)
        {
            sig = term.GetBuiltInSignature();
            if (term is Complex c)
            {
                term = c.WithArguments(c.Arguments.Select(a => ResolveBuiltin(a, scope, subs, depth, out _)).ToArray());
            }
            while (BuiltIns.TryGetValue(sig, out var builtIn)
            || BuiltIns.TryGetValue(sig = sig.WithArity(Maybe<int>.None), out builtIn)) {
                var eval = builtIn.Apply(this, scope, term.Reduce(a => Array.Empty<ITerm>(), v => Array.Empty<ITerm>(), c => c.Arguments));
                LogTrace(TraceType.Resv, $"{term.Explain()} -> {eval.Result.Explain()} {{{string.Join("; ", eval.Substitutions.Select(s => s.Explain()))}}}", depth);
                term = eval.Result;
                subs.AddRange(eval.Substitutions);
                var newSig = term.GetBuiltInSignature();
                if (sig.Equals(newSig)) {
                    break;
                }
                sig = newSig;
            }
            return term;
        }

        private void LogTrace(TraceType type, ITerm term, int depth = 0)
        {
            LogTrace(type, term.Explain(), depth);
        }

        private void LogTrace(TraceType type, string s, int depth = 0)
        {
            Trace?.Invoke(type, $"{type}: ({depth:00}) {s}");
        }

        protected IEnumerable<Solution> Solve(Scope scope, ITerm goal, List<Substitution> subs = null, int depth = 0)
        {
            subs ??= new List<Substitution>();
            // Treat comma-expression complex ITerms as proper expressions
            if (CommaSequence.TryUnfold(goal, out var expr)) {
                foreach (var s in Solve(scope, expr, subs, depth)) {
                    yield return s;
                }
                yield break;
            }
            // Transform builtins into the literal they evaluate to
            goal = ResolveBuiltin(goal, scope, subs, depth, out var signature);
            if (goal.Equals(Literals.False) || goal is Variable) {
                LogTrace(TraceType.Retn, "false", depth);
                yield break;
            }
            if (goal.Equals(Literals.True)) {
                LogTrace(TraceType.Retn, $"true {{{string.Join("; ", subs.Select(s => s.Explain()))}}}", depth);
                yield return new Solution(subs.ToArray());
                yield break;
            }
            var (qualifiedGoal, matches) = QualifyGoal(Modules[scope.Module], goal);
            LogTrace(TraceType.Call, qualifiedGoal, depth);
            foreach (var m in matches) {
                var innerScope = new Scope(m.Rhs.DeclaringModule, Maybe.Some(m.Rhs), scope.Callee);
                var recursiveCall = innerScope.Callee.Reduce(a => innerScope.Caller.Reduce(b =>
                        Predicate.Signature(a.Head) == Predicate.Signature(b.Head), () => false), () => false);
                var solve = Solve(innerScope, m.Rhs.Body, new List<Substitution>(m.Substitutions), depth + 1);
                foreach (var s in solve) {
                    LogTrace(TraceType.Exit, m.Rhs.Head, depth);
                    yield return s;
                }
                if (Cut.Value) {
                    yield break;
                }
            }

            (ITerm Qualified, IEnumerable<KnowledgeBase.Match> Matches) QualifyGoal(Module scope, ITerm goal)
            {
                if (KnowledgeBase.TryGetMatches(goal, out var matches))
                {
                    return (goal, matches);
                }
                var qualifiedGoal = goal.Qualify(scope.Name);
                if (KnowledgeBase.TryGetMatches(qualifiedGoal, out matches))
                {
                    return (qualifiedGoal, matches);
                }
                if (Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
                {
                    throw new InterpreterException(InterpreterError.UnknownPredicate, qualifiedGoal.GetBuiltInSignature().Explain());
                }
                return (goal, Enumerable.Empty<KnowledgeBase.Match>());
            }
        }

        protected IEnumerable<Solution> Solve(Scope scope, CommaSequence query, List<Substitution> subs = null, int depth = 0)
        {
            subs ??= new List<Substitution>();
            if (query.IsEmpty) {
                yield return new Solution(subs.ToArray());
                yield break;
            }
            var goals = query.Contents;
            var subGoal = goals.First();
            goals = goals[1..];
            // Get first solution for the current subgoal
            foreach (var s in Solve(scope, subGoal, subs, depth)) {
                if(Cut.Value) {
                    yield break;
                }
                var rest = (CommaSequence)new CommaSequence(goals).Substitute(s.Substitutions);
                foreach (var ss in Solve(scope, rest, subs, depth)) {
                    yield return new Solution(s.Substitutions.Concat(ss.Substitutions).Distinct().ToArray());
                }
                if(subGoal.Equals(Literals.Cut)) {
                    Cut.Value = true;
                    yield break;
                }
            }
        }

        public IEnumerable<Solution> Solve(Query goal, Maybe<Scope> scope = default)
        {
            Cut.Value = false;
            return Solve(scope.Reduce(some => some, () => new Scope(EntryModule, default, default)), goal.Goals);
        }
    }
}
