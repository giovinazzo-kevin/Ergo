using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Solver.BuiltIns
{
    public sealed class DictKeyValue : BuiltIn
    {
        public DictKeyValue()
            : base("", new($"dict_key_value"), Maybe<int>.Some(3), Modules.Dict)
        {
        }

        public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
        {
            if(args[0] is Variable)
            {
                solver.Throw(new SolverException(SolverError.TermNotSufficientlyInstantiated, scope, args[0].Explain()));
                yield return new Evaluation(WellKnown.Literals.False);
                yield break;
            }
            if (args[0] is Dict dict)
            {
                if (!dict.Dictionary.Keys.Any())
                {
                    yield return new Evaluation(WellKnown.Literals.False);
                    yield break;
                }
                foreach (var key in dict.Dictionary.Keys)
                {
                    if (new Substitution(args[1], key).TryUnify(out var subs) && new Substitution(args[2], dict.Dictionary[key]).TryUnify(out var vSubs))
                    {
                        yield return new Evaluation(WellKnown.Literals.True, subs.Concat(vSubs).ToArray());
                    }
                    else
                    {
                        yield return new Evaluation(WellKnown.Literals.False);
                        yield break;
                    }
                }
                yield break;
            }
            yield return new Evaluation(WellKnown.Literals.False);
        }
    }

}
