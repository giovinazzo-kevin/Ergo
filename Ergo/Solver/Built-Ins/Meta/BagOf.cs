using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class BagOf : BuiltIn
    {
        private readonly Atom ExistentialQualifier = new("^");

        public BagOf()
            : base("", new("bagof"), Maybe.Some(3), Modules.Meta)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            var (template, goal, instances) = (args[0], args[1], args[2]);
            scope = scope.WithDepth(scope.Depth + 1)
                .WithCaller(scope.Callee)
                .WithCallee(Maybe.Some(GetStub(args)));
            if (goal is Variable)
            {
                throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, goal.Explain());
            }
            if (instances is not Variable && !List.TryUnfold(instances, out _))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, solver.InterpreterScope, Types.List, instances.Explain());
            }

            var templateVars = Enumerable.Empty<Variable>();
            while(goal is Complex c && c.Functor == ExistentialQualifier)
            {
                templateVars = templateVars.Concat(c.Arguments[0].Variables);
                goal = c.Arguments[1];
            }
            templateVars = templateVars.Concat(template.Variables)
                .ToHashSet();

            var variable = new Variable("__B");
            var freeVars = new List(ImmutableArray.CreateRange(
                goal.Variables.Where(v => !templateVars.Contains(v)).Cast<ITerm>())
            );

            var goalClauses = new CommaSequence(ImmutableArray<ITerm>.Empty
                .Add(goal));

            var solutions = solver.Solve(new(goalClauses), Maybe.Some(scope))
                .Select(s => s.Simplify())
                .ToArray();
            if (solutions.Length == 0)
            {
                if (instances.IsGround && args[2] == WellKnown.Literals.EmptyList)
                {
                    yield return new Evaluation(WellKnown.Literals.True);
                }
                else if (!args[2].IsGround)
                {
                    yield return new Evaluation(WellKnown.Literals.True, new Substitution(args[2], WellKnown.Literals.EmptyList));
                }
                else
                {
                    yield return new Evaluation(WellKnown.Literals.False);
                }
            }
            else
            {
                var list = new List(ImmutableArray.CreateRange(solutions.Select(s => args[0].Substitute(s.Substitutions))));
                if (args[2].IsGround && args[2] == list.Root)
                {
                    yield return new Evaluation(WellKnown.Literals.True);
                }
                else if (!args[2].IsGround)
                {
                    yield return new Evaluation(WellKnown.Literals.True, new Substitution(args[2], list.Root));
                }
                else
                {
                    yield return new Evaluation(WellKnown.Literals.False);
                }
            }
        }
    }
}
