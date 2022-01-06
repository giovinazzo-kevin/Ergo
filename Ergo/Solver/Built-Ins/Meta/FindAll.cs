using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class FindAll : BuiltIn
    {
        public FindAll()
            : base("", new("findall"), Maybe.Some(3), Modules.Meta)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            scope = scope.WithDepth(scope.Depth + 1)
                .WithCaller(scope.Callee)
                .WithCallee(Maybe.Some(GetStub(args)));
            if (!CommaSequence.TryUnfold(args[1], out var comma))
            {
                comma = new(ImmutableArray<ITerm>.Empty.Add(args[1]));
            }
            var solutions = solver.Solve(new(comma), Maybe.Some(scope))
                .Select(s => s.Simplify())
                .ToArray();
            if (solutions.Length == 0)
            {
                if(args[2].IsGround && args[2].Equals(WellKnown.Literals.EmptyList))
                {
                    yield return new Evaluation(WellKnown.Literals.True);
                }
                else if(!args[2].IsGround)
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
