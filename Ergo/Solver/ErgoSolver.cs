using Ergo.Lang;
using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ergo.Lang.Extensions;
using Ergo.Lang.Exceptions;
using Ergo.Solver.BuiltIns;
using Ergo.Interpreter;
using System.IO;
using Ergo.Lang.Utils;

namespace Ergo.Solver
{

    public partial class ErgoSolver
    {
        protected readonly AsyncLocal<bool> Cut = new();

        public readonly SolverFlags Flags;
        public readonly KnowledgeBase KnowledgeBase;
        public readonly InterpreterScope InterpreterScope;
        public readonly Dictionary<Signature, BuiltIn> BuiltIns;

        public event Action<SolverTraceType, string> Trace;

        public ErgoSolver(ErgoInterpreter i, InterpreterScope scope, SolverFlags flags = SolverFlags.Default)
        {
            Flags = flags;
            KnowledgeBase = new();
            BuiltIns = new();
            AddBuiltInsByReflection();
            var added = new HashSet<Atom>();
            LoadModule(scope.Modules[scope.CurrentModule], added);
            foreach (var module in scope.Modules.Values)
            {
                LoadModule(module, added);
            }
            InterpreterScope = scope;
            void LoadModule(Module module, HashSet<Atom> added)
            {
                if (added.Contains(module.Name))
                    return;
                added.Add(module.Name);
                foreach (var subModule in module.Imports.Contents.Select(c => (Atom)c))
                {
                    if (added.Contains(subModule))
                        continue;
                    if(!scope.Modules.TryGetValue(subModule, out var import))
                    {
                        var importScope = scope;
                        scope = scope.WithModule(import = i.Load(ref importScope, subModule.Explain()));
                    }
                    LoadModule(import, added);
                }
                foreach (var pred in module.Program.KnowledgeBase)
                {
                    var sig = pred.Head.GetSignature();
                    if (module.Name == scope.CurrentModule || module.ContainsExport(sig))
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

        public bool TryAddBuiltIn(BuiltIn b) => BuiltIns.TryAdd(b.Signature, b);

        protected void AddBuiltInsByReflection()
        {
            var assembly = typeof(Print).Assembly;
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAssignableTo(typeof(BuiltIn))) continue;
                if (!type.GetConstructors().Any(c => c.GetParameters().Length == 0)) continue;
                var inst = (BuiltIn)Activator.CreateInstance(type);
                BuiltIns[inst.Signature] = inst;
            }
        }


        private ITerm ResolveBuiltin(SolverScope scope, ITerm term, List<Substitution> subs, int depth, out Signature sig)
        {
            sig = term.GetSignature();
            if (term is Complex c)
            {
                term = c.WithArguments(c.Arguments.Select(a => ResolveBuiltin(scope, a, subs, depth, out _)).ToArray());
            }
            while (BuiltIns.TryGetValue(sig, out var builtIn)
            || BuiltIns.TryGetValue(sig = sig.WithArity(Maybe<int>.None), out builtIn)) {
                var eval = builtIn.Apply(this, scope, term.Reduce(a => Array.Empty<ITerm>(), v => Array.Empty<ITerm>(), c => c.Arguments));
                LogTrace(SolverTraceType.Resv, $"{term.Explain()} -> {eval.Result.Explain()} {{{string.Join("; ", eval.Substitutions.Select(s => s.Explain()))}}}", depth);
                term = eval.Result;
                subs.AddRange(eval.Substitutions);
                var newSig = term.GetSignature();
                if (sig.Equals(newSig)) {
                    break;
                }
                sig = newSig;
            }
            return term;
        }

        private void LogTrace(SolverTraceType type, ITerm term, int depth = 0)
        {
            LogTrace(type, term.Explain(), depth);
        }

        private void LogTrace(SolverTraceType type, string s, int depth = 0)
        {
            Trace?.Invoke(type, $"{type}: ({depth:00}) {s}");
        }

        protected IEnumerable<Solution> Solve(SolverScope scope, ITerm goal, List<Substitution> subs = null, int depth = 0)
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
            goal = ResolveBuiltin(scope, goal, subs, depth, out var signature);
            if (goal.Equals(Literals.False) || goal is Variable) {
                LogTrace(SolverTraceType.Retn, "false", depth);
                yield break;
            }
            if (goal.Equals(Literals.True)) {
                LogTrace(SolverTraceType.Retn, $"true {{{string.Join("; ", subs.Select(s => s.Explain()))}}}", depth);
                yield return new Solution(subs.ToArray());
                yield break;
            }
            var (qualifiedGoal, matches) = QualifyGoal(InterpreterScope.Modules[scope.Module], goal);
            LogTrace(SolverTraceType.Call, qualifiedGoal, depth);
            foreach (var m in matches) {
                var innerScope = new SolverScope(m.Rhs.DeclaringModule, Maybe.Some(m.Rhs), scope.Callee);
                var recursiveCall = innerScope.Callee.Reduce(a => innerScope.Caller.Reduce(b =>
                        Predicate.Signature(a.Head) == Predicate.Signature(b.Head), () => false), () => false);
                var solve = Solve(innerScope, m.Rhs.Body, new List<Substitution>(m.Substitutions), depth + 1);
                foreach (var s in solve) {
                    LogTrace(SolverTraceType.Exit, m.Rhs.Head, depth);
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
                    throw new InterpreterException(InterpreterError.UnknownPredicate, qualifiedGoal.GetSignature().Explain());
                }
                return (goal, Enumerable.Empty<KnowledgeBase.Match>());
            }
        }

        protected IEnumerable<Solution> Solve(SolverScope scope, CommaSequence query, List<Substitution> subs = null, int depth = 0)
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

        public IEnumerable<Solution> Solve(Query goal, Maybe<SolverScope> scope = default)
        {
            Cut.Value = false;
            return Solve(scope.Reduce(some => some, () => new SolverScope(InterpreterScope.CurrentModule, default, default)), goal.Goals);
        }
    }
}
