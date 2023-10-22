namespace Ergo.Solver.BuiltIns;

public abstract class NthBase : SolverBuiltIn
{
    public readonly int Offset;

    public NthBase(int offset)
        : base("", new($"nth{offset}"), Maybe<int>.Some(3), WellKnown.Modules.List) => Offset = offset;

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> args)
    {
        if (args[0].Matches<int>(out var index))
        {
            index -= Offset;
            if (args[1] is List list && index >= 0 && index < list.Contents.Length)
            {
                var elem = list.Contents[index];
                if (LanguageExtensions.Unify(args[2], elem).TryGetValue(out var subs))
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
                yield return True(new Substitution(args[1], new List(contents, default, args[1].Scope)));
                yield break;
            }
        }
        else if (!args[0].IsGround)
        {
            if (args[1] is List list)
            {
                var any = false;
                for (var i = 0; i < list.Contents.Length; ++i)
                {
                    var elem = list.Contents[i];
                    if (LanguageExtensions.Unify(args[2], elem).TryGetValue(out var subs))
                    {
                        any = true;
                        subs.Add(new(args[0], new Atom(i + Offset)));
                        yield return True(subs);
                    }
                }

                if (any)
                {
                    yield break;
                }
            }
            else if (!args[1].IsGround)
            {
                yield return True();
                yield break;
            }
        }

        yield return False();
    }
}

