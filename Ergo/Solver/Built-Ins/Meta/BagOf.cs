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

            var goalClauses = new CommaSequence(
                goal,
                new Complex(WellKnown.Functors.Unification.First(), 
                    variable, 
                    new CommaSequence(freeVars.Root, template).Root)
            );

            var solutions = solver.Solve(new(goalClauses), Maybe.Some(scope))
                .Select(s => s.Simplify())
                .ToArray();

            if(!solutions.Any())
            {
                yield return new(WellKnown.Literals.False);
                yield break;
            }

            foreach (var sol in solutions)
            {
                var instance = template.Substitute(sol.Substitutions);

            }
        }
    }
}
