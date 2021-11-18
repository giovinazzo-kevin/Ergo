using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{

    public partial class Solver
    {
        protected int GlobalVarCounter;

        protected readonly KnowledgeBase Kb;
        protected readonly IReadOnlyDictionary<string, BuiltIn> BuiltIns;
        protected readonly SolverFlags Flags;

        public event Action<string> Trace;

        public Solver(KnowledgeBase kb, IReadOnlyDictionary<string, BuiltIn> builtins, SolverFlags flags = SolverFlags.Default)
        {
            Kb = kb;
            Flags = flags;
            BuiltIns = builtins;
            GlobalVarCounter = 0;
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

        private void LogTrace(Term term, int indent = 0)
        {
            Trace?.Invoke(new string(' ', indent) + Term.Explain(term));
        }

        public IEnumerable<Solution> Solve(Term term, List<Substitution> subs = null, int indent = 0)
        {
            LogTrace(term, indent: indent);
            subs ??= new List<Substitution>();
            // Treat comma-expression complex terms as proper expressions
            if (CommaExpression.TryUnfold(term, out var expr)) {
                foreach (var s in Solve(expr.Sequence, subs, indent)) {
                    yield return s;
                }
                yield break;
            }
            term = ResolveBuiltin(term, subs, out var signature);
            if (term.Equals(Literals.False))
                yield break;
            if (term.Equals(Literals.True)) {
                yield return new Solution(subs.ToArray());
                yield break;
            }
            if (!Kb.TryGetMatches(term, out var matches) && Flags.HasFlag(SolverFlags.ThrowOnPredicateNotFound)) {
                throw new InterpreterException(Interpreter.ErrorType.UnknownPredicate, signature);
            }
            if (!matches.Any()) {
                yield break;
            }
            foreach (var m in matches) {
                LogTrace(m.Rhs.Head, indent: indent + 1);
                foreach (var s in Solve(m.Rhs.Body, new List<Substitution>(m.Substitutions), indent + 1)) {
                    yield return s;
                }
            }
        }

        public IEnumerable<Solution> Solve(Sequence goal, List<Substitution> subs = null, int indent = 0)
        {
            subs ??= new List<Substitution>();
            if(!CommaExpression.IsCommaExpression(goal)) {
                throw new InvalidOperationException("Only CommaExpression sequences can be solved.");
            }
            if (goal.IsEmpty) {
                yield return new Solution(subs.ToArray());
                yield break;
            }
            var goals = goal.GetContents().ToArray();
            var subGoal = goals.First();
            goals = goals[1..];
            // Get first solution for the current subgoal
            foreach (var s in Solve(subGoal, subs, indent)) {
                var rest = Sequence.Substitute(new Sequence(goal.Functor, goal.EmptyElement, goals), s.Substitutions);
                foreach (var ss in Solve(rest, subs, indent)) {
                    yield return new Solution(s.Substitutions.Concat(ss.Substitutions).Distinct().ToArray());
                }
            }
        }
    }
}
