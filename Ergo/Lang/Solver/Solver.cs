using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Ergo.Lang
{

    public partial class Solver
    {
        protected readonly AsyncLocal<bool> Cut = new();

        public readonly KnowledgeBase KnowledgeBase;
        public readonly Atom EntryModule;

        protected readonly IReadOnlyDictionary<Atom, Module> Modules;
        protected readonly IReadOnlyDictionary<string, BuiltIn> BuiltIns;
        protected readonly SolverFlags Flags;

        public event Action<TraceType, string> Trace;

        public Solver(Atom entryModule, IReadOnlyDictionary<Atom, Module> modules, IReadOnlyDictionary<string, BuiltIn> builtins, SolverFlags flags = SolverFlags.Default)
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
                foreach (var subModule in module.Imports.Head.Contents.Select(c => (Atom)c))
                {
                    if (added.Contains(subModule))
                        continue;
                    LoadModule(Modules[subModule], added);
                }
                foreach (var pred in module.KnowledgeBase)
                {
                    var head = pred.Head.Reduce(a => a, v => throw new ArgumentException(), c => c.Functor);
                    var predicateSlashArity = new Expression(Operators.BinaryDivision, head, Maybe<Term>.Some(new Atom((double)Predicate.Arity(pred.Head)))).Complex;
                    if(module.Name == Interpreter.UserModule 
                    || module.Exports.Head.Contents.Any(t => Substitution.TryUnify(new(t, predicateSlashArity), out _)))
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

        private Term ResolveBuiltin(Term term, Atom module, List<Substitution> subs, out string sig)
        {
            sig = Predicate.Signature(term);
            term = term.Map(t => t, v => v, c => c.WithArguments(c.Arguments.Select(a => ResolveBuiltin(a, module, subs, out _)).ToArray()));
            while (BuiltIns.TryGetValue(sig, out var builtIn)) {
                var eval = builtIn.Apply(term, module);
                term = eval.Result;
                subs.AddRange(eval.Substitutions);
                var newSig = Predicate.Signature(term);
                if (sig == newSig) {
                    break;
                }
                sig = newSig;
            }
            return term;
        }

        private void LogTrace(TraceType type, Term term, int depth = 0)
        {
            LogTrace(type, Term.Explain(term), depth);
        }

        private void LogTrace(TraceType type, string s, int depth = 0)
        {
            Trace?.Invoke(type, $"{type}: ({depth:00}) {s}");
        }

        protected IEnumerable<Solution> Solve(Scope scope, Term goal, List<Substitution> subs = null, int depth = 0)
        {
            subs ??= new List<Substitution>();
            // Treat comma-expression complex terms as proper expressions
            if (CommaExpression.TryUnfold(goal, out var expr)) {
                foreach (var s in Solve(scope, expr.Sequence, subs, depth)) {
                    yield return s;
                }
                yield break;
            }
            // Transform builtins into the literal they evaluate to
            goal = ResolveBuiltin(goal, scope.Module, subs, out var signature);
            if (goal.Equals(Literals.False)) {
                LogTrace(TraceType.Retn, "false", depth);
                yield break;
            }
            if (goal.Equals(Literals.True)) {
                LogTrace(TraceType.Retn, "true", depth);
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

            (Term Qualified, IEnumerable<KnowledgeBase.Match> Matches) QualifyGoal(Module scope, Term goal)
            {
                if (KnowledgeBase.TryGetMatches(goal, out var matches))
                {
                    return (goal, matches);
                }
                var qualifiedGoal = goal.Reduce<Term>(
                    a => new Atom($"{Atom.Explain(scope.Name)}:{Atom.Explain(a)}"),
                    v => new Variable($"{Atom.Explain(scope.Name)}:{Variable.Explain(v)}"),
                    c => new Complex(new Atom($"{Atom.Explain(scope.Name)}:{Atom.Explain(c.Functor)}"), c.Arguments)
                );
                if (KnowledgeBase.TryGetMatches(qualifiedGoal, out matches))
                {
                    return (qualifiedGoal, matches);
                }
                if (Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
                {
                    throw new InterpreterException(Interpreter.ErrorType.UnknownPredicate, signature);
                }
                return (goal, Enumerable.Empty<KnowledgeBase.Match>());
            }
        }

        protected IEnumerable<Solution> Solve(Scope scope, Sequence goal, List<Substitution> subs = null, int depth = 0)
        {
            subs ??= new List<Substitution>();
            if (!CommaExpression.IsCommaExpression(goal)) {
                throw new InvalidOperationException("Only CommaExpression sequences can be solved.");
            }
            if (goal.IsEmpty) {
                yield return new Solution(subs.ToArray());
                yield break;
            }
            var goals = goal.Contents;
            var subGoal = goals.First();
            goals = goals[1..];
            // Get first solution for the current subgoal
            foreach (var s in Solve(scope, subGoal, subs, depth)) {
                if(Cut.Value) {
                    yield break;
                }
                var rest = Sequence.Substitute(new Sequence(goal.Functor, goal.EmptyElement, goals), s.Substitutions);
                foreach (var ss in Solve(scope, rest, subs, depth)) {
                    yield return new Solution(s.Substitutions.Concat(ss.Substitutions).Distinct().ToArray());
                }
                if(subGoal.Equals(Literals.Cut)) {
                    Cut.Value = true;
                    yield break;
                }
            }
        }

        public IEnumerable<Solution> Solve(Sequence goal)
        {
            Cut.Value = false;
            return Solve(new Scope(EntryModule, default, default), goal);
        }
    }
}
