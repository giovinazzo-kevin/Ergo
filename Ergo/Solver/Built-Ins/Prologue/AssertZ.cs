﻿using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Collections.Generic;

namespace Ergo.Solver.BuiltIns
{
    public sealed class AssertZ : DynamicPredicateBuiltIn
    {
        public AssertZ()
            : base("", new("assertz"), Maybe.Some(1))
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] arguments)
        {
            if (Assert(solver, scope, arguments[0], z: true))
            {
                yield return new(WellKnown.Literals.True);
            }
            else
            {
                yield return new(WellKnown.Literals.False);
            }
        }
    }
}