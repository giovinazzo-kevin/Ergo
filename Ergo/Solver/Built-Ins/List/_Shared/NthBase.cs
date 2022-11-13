﻿namespace Ergo.Solver.BuiltIns;

public abstract class NthBase : SolverBuiltIn
{
    public readonly int Offset;

    public NthBase(int offset)
        : base("", new($"nth{offset}"), Maybe<int>.Some(3), WellKnown.Modules.List) => Offset = offset;

    public override async IAsyncEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (args[0].Matches<int>(out var index))
        {
            index -= Offset;
            if (args[1].IsAbstract<List>().TryGetValue(out var list) && index >= 0 && index < list.Contents.Length)
            {
                var elem = list.Contents[index];
                if (args[2].Unify(elem).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
            }
            else if (!args[1].IsGround)
            {
                var contents = Enumerable.Range(0, index)
                    .Select(x => (ITerm)new Variable("_"))
                    .Append(args[2]);
                yield return True(new Substitution(args[1], new List(contents).CanonicalForm));
                yield break;
            }
        }
        else if (!args[0].IsGround)
        {
            if (args[1].IsAbstract<List>().TryGetValue(out var list))
            {
                var any = false;
                for (var i = 0; i < list.Contents.Length; ++i)
                {
                    var elem = list.Contents[i];
                    if (args[2].Unify(elem).TryGetValue(out var subs))
                    {
                        any = true;
                        yield return True(subs.Prepend(new(args[0], new Atom(i + Offset))).ToArray());
                    }
                }

                if (any)
                {
                    yield break;
                }
            }
            else if (!args[1].IsGround)
            {
                yield return new Evaluation(WellKnown.Literals.True);
                yield break;
            }
        }

        yield return False();
    }
}
