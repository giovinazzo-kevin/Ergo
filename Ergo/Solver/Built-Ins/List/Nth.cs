using Ergo.Interpreter;

namespace Ergo.Solver.BuiltIns;

public abstract class Nth : BuiltIn
{
    public readonly int Offset;

    public Nth(int offset)
        : base("", new($"nth{offset}"), Maybe<int>.Some(3), Modules.List) => Offset = offset;

    public override async IAsyncEnumerable<Evaluation> Apply(ErgoSolver solver, SolverScope scope, ITerm[] args)
    {
        if (args[0].Matches<int>(out var index))
        {
            index -= Offset;
            if (List.TryUnfold(args[1], out var list) && index >= 0 && index < list.Contents.Length)
            {
                var elem = list.Contents[index];
                if (args[2].Unify(elem).TryGetValue(out var subs))
                {
                    yield return new Evaluation(WellKnown.Literals.True, subs.ToArray());
                    yield break;
                }
            }
            else if (!args[1].IsGround)
            {
                var contents = Enumerable.Range(0, index)
                    .Select(x => (ITerm)new Variable("_"))
                    .Append(args[2]);
                yield return new Evaluation(WellKnown.Literals.True, new Substitution(args[1], new List(contents.ToArray()).Root));
                yield break;
            }
        }
        else if (!args[0].IsGround)
        {
            if (List.TryUnfold(args[1], out var list))
            {
                var any = false;
                for (var i = 0; i < list.Contents.Length; ++i)
                {
                    var elem = list.Contents[i];
                    if (args[2].Unify(elem).TryGetValue(out var subs))
                    {
                        any = true;
                        yield return new Evaluation(WellKnown.Literals.True, subs.Prepend(new(args[0], new Atom(i + Offset))).ToArray());
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

        yield return new Evaluation(WellKnown.Literals.False);
    }
}
public sealed class Nth0 : Nth { public Nth0() : base(0) { } }
public sealed class Nth1 : Nth { public Nth1() : base(1) { } }

