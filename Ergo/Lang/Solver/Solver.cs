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

        protected readonly IReadOnlyDictionary<Atom, Module> Modules;
        protected readonly IReadOnlyDictionary<string, BuiltIn> BuiltIns;
        protected readonly SolverFlags Flags;

        public event Action<string> Trace;

        public Solver(IReadOnlyDictionary<Atom, Module> modules, IReadOnlyDictionary<string, BuiltIn> builtins, SolverFlags flags = SolverFlags.Default)
        {
            Modules = modules;
            Flags = flags;
            BuiltIns = builtins;
            KnowledgeBase = new KnowledgeBase();
            foreach (var module in Modules.Values)
            {
                LoadModule(module);
            }
            void LoadModule(Module module, HashSet<Atom> added = null)
            {
                added ??= new();
                added.Add(module.Name);
                foreach (var subModule in module.Imports.Head.Contents.Select(c => (Atom)c))
                {
                    if (added.Contains(subModule))
                        continue;
                    LoadModule(Modules[subModule]);
                }
                foreach (var pred in module.KnowledgeBase)
                {
                    KnowledgeBase.AssertZ(pred.Qualified(module));
                }
            }
        }

        private Term ResolveBuiltin(Term term, List<Substitution> subs, out string sig)
        {
            sig = Predicate.Signature(term);
            term = term.Map(t => t, v => v, c => c.WithArguments(c.Arguments.Select(a => ResolveBuiltin(a, subs, out _)).ToArray()));
            while (BuiltIns.TryGetValue(sig, out var builtIn)) {
                var eval = builtIn.Apply(term);
                term = eval.Result;
                subs.AddRange(eval.Substitutions);
                var newSig = Predicate.Signature(term);
                if (sig == newSig) {
                    break;
                }
                sig = newSig;
            }
            // Apply static literal transformations, such as () --> true
            //if (term.Equals(Literals.EmptyCommaExpression))
            //    term = Literals.True;
            return term;
        }

        private void LogTrace(TraceType type, Term term, int depth = 0)
        {
            LogTrace(type, Term.Explain(term), depth);
        }

        private void LogTrace(TraceType type, string s, int depth = 0)
        {
            Trace?.Invoke($"{type}: ({depth:00}) {s}");
        }

        protected IEnumerable<Solution> Solve(Term goal, List<Substitution> subs = null, int depth = 0)
        {
            subs ??= new List<Substitution>();
            // Treat comma-expression complex terms as proper expressions
            if (CommaExpression.TryUnfold(goal, out var expr)) {
                foreach (var s in Solve(expr.Sequence, subs, depth)) {
                    yield return s;
                }
                yield break;
            }
            // Transform builtins into the literal they evaluate to
            goal = ResolveBuiltin(goal, subs, out var signature);
            if (goal.Equals(Literals.False)) {
                yield break;
            }
            if (goal.Equals(Literals.True)) {
                yield return new Solution(subs.ToArray());
                yield break;
            }
            LogTrace(TraceType.Call, goal, depth);
            var matches = QualifyGoal(goal);
            foreach (var m in matches) {
                foreach (var s in Solve(m.Rhs.Body, new List<Substitution>(m.Substitutions), depth + 1)) {
                    LogTrace(TraceType.Exit, m.Rhs.Head, depth);
                    yield return s;
                }
                if (Cut.Value) {
                    yield break;
                }
            }

            IEnumerable<KnowledgeBase.Match> QualifyGoal(Term goal)
            {
                foreach (var module in Modules.Keys)
                {
                    var qualifiedGoal = goal.Reduce<Term>(
                        a => new Atom($"{Atom.Explain(module)}:{Atom.Explain(a)}"),
                        v => new Variable($"{Atom.Explain(module)}:{Variable.Explain(v)}"),
                        c => new Complex(new Atom($"{Atom.Explain(module)}:{Atom.Explain(c.Functor)}"), c.Arguments)
                    );
                    if (KnowledgeBase.TryGetMatches(qualifiedGoal, out var matches))
                    {
                        return matches;
                    }
                }
                if(Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound))
                {
                    throw new InterpreterException(Interpreter.ErrorType.UnknownPredicate, signature);
                }
                return Enumerable.Empty<KnowledgeBase.Match>();
            }
        }

        protected IEnumerable<Solution> Solve(Sequence goal, List<Substitution> subs = null, int depth = 0)
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
            foreach (var s in Solve(subGoal, subs, depth)) {
                if(Cut.Value) {
                    yield break;
                }
                var rest = Sequence.Substitute(new Sequence(goal.Functor, goal.EmptyElement, goals), s.Substitutions);
                foreach (var ss in Solve(rest, subs, depth)) {
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
            return Solve(goal, null, 0);
        }
    }
}
