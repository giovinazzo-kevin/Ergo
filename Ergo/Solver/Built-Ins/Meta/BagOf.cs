﻿using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ergo.Solver.BuiltIns
{

    public sealed class BagOf : SolutionAggregationBuiltIn
    {
        public BagOf()
            : base("", new("bagof"), Maybe.Some(3), Modules.Meta)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            var any = false;
            await foreach (var (ArgVars, ListTemplate, ListVars) in AggregateSolutions(solver, scope, args))
            {
                if(!new Substitution(ListVars.Root, ArgVars).TryUnify(out var listSubs)
                || !new Substitution(args[2], ListTemplate.Root).TryUnify(out var instSubs))
                {
                    yield return new(WellKnown.Literals.False);
                    yield break;
                }
                yield return new(WellKnown.Literals.True, listSubs.Concat(instSubs).ToArray());
                any = true;
            }
            if(!any)
            {
                yield return new(WellKnown.Literals.False);
            }
        }
    }
}
