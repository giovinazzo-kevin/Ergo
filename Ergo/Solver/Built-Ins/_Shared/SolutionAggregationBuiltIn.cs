using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public abstract class SolutionAggregationBuiltIn : BuiltIn
    {
        protected SolutionAggregationBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module) 
            : base(documentation, functor, arity, module)
        {
        }

        protected async IAsyncEnumerable<(ITerm ArgVars, List ListTemplate, List ListVars)> AggregateSolutions(ErgoSolver solver, SolverScope scope, ITerm[] args)
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
            while (goal is Complex c && WellKnown.Functors.ExistentialQualifier.Contains(c.Functor))
            {
                templateVars = templateVars.Concat(c.Arguments[0].Variables);
                goal = c.Arguments[1];
            }
            templateVars = templateVars.Concat(template.Variables)
                .ToHashSet();

            var variable = new Variable("TMP_BAGOF__"); // TODO: something akin to thread.next_free_variable() from TauProlog
            var freeVars = goal.Variables.Where(v => !templateVars.Contains(v))
                .ToHashSet();
            var listVars = new List(freeVars.Cast<ITerm>());

            var goalClauses = new CommaSequence(
                goal,
                new Complex(WellKnown.Functors.Unification.First(),
                    variable,
                    new CommaSequence(listVars.Root, template).AsParenthesized(true).Root)
                .AsOperator(OperatorAffix.Infix)
            );
            var solutions = (await solver.Solve(new(goalClauses), Maybe.Some(scope)).CollectAsync())
                .Select(s => s.Simplify());
            foreach(var sol in solutions
                .Select(sol =>
                {
                    var arg = (Complex)sol.Links.Value[variable];

                    var argVars = arg.Arguments[0];
                    var argTmpl = arg.Arguments[1];
                    var varTmpl = argTmpl.Variables
                        .ToHashSet();

                    var subTmpl = sol.Links.Value
                        .Where(kv => kv.Key is Variable && kv.Value is Variable)
                        .Select(kv => (Key: (Variable)kv.Key, Value: (Variable)kv.Value))
                        .Where(kv => varTmpl.Contains(kv.Value) && !templateVars.Contains(kv.Key) && !freeVars.Contains(kv.Key))
                        .Select(kv => new Substitution(kv.Key, kv.Value));

                    argVars = argVars.Substitute(subTmpl);
                    argTmpl = argTmpl.Substitute(subTmpl);
                    return (argVars, argTmpl);
                })
                .ToLookup(sol => sol.argVars, sol => sol.argTmpl)
                .Select(kv => (kv.Key, new List(kv), listVars)))
            {
                yield return sol;
            }
        }
    }
}
