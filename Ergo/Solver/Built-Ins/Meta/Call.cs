using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{

    public sealed class Call : BuiltIn
    {
        public Call()
            : base("", new("call"), Maybe<int>.None, Modules.Meta)
        {
        }

        public override IEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            scope = scope.WithDepth(scope.Depth + 1)
                .WithCaller(scope.Callee)
                .WithCallee(Maybe.Some(GetStub(args)));
            if (args.Length == 0)
            {
                throw new SolverException(SolverError.UndefinedPredicate, scope, Signature.WithArity(Maybe<int>.Some(0)).Explain());
            }
            var goal = args.Aggregate((a, b) => a.Concat(b));
            if(goal is Variable)
            {
                throw new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, goal.Explain());
            }
            if (!CommaSequence.TryUnfold(goal, out var comma))
            {
                comma = new(ImmutableArray<ITerm>.Empty.Add(goal));
            }
            var any = false;
            foreach (var solution in solver.Solve(new(comma), Maybe.Some(scope)))
            {
                yield return new Evaluation(Literals.True, solution.Substitutions);
                any = true;
            }
            if(!any)
            {
                yield return new Evaluation(Literals.False);
            }
        }
    }
}
