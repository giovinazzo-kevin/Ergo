
namespace Ergo.Solver.BuiltIns;

public sealed class With : SolverBuiltIn
{
    public With()
        : base("", new($"with"), Maybe<int>.Some(3), WellKnown.Modules.Dict)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] args)
    {
        if (args[0].IsAbstract<Dict>().TryGetValue(out var a))
        {
            if (args[1].IsAbstract<Set>().TryGetValue(out var b)
                && GetPairs(b).TryGetValue(out var kvps))
            {
                var merged = Update(a, kvps);
                if (args[2].Unify(merged).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
            }
            else if (args[2].IsAbstract<Dict>().TryGetValue(out var d))
            {
                if (a.Dictionary.Keys.Any(k => d.Dictionary.ContainsKey(k) && !d.Dictionary[k].Unify(a.Dictionary[k]).TryGetValue(out _)))
                {
                    yield return False();
                    yield break;
                }
                var diff = d.Dictionary.Keys.Except(a.Dictionary.Keys).ToHashSet();
                var merged = new Set(d.Dictionary.Where(kvp => diff.Contains(kvp.Key))
                    .Select(x => (ITerm)WellKnown.Operators.NamedArgument.ToComplex(x.Key, Maybe.Some(x.Value))), default);
                if (args[1].Unify(merged).TryGetValue(out var subs))
                {
                    yield return True(subs);
                    yield break;
                }
            }
        }
        else if (args[0] is Variable
            && args[1].IsAbstract<Set>().TryGetValue(out var b)
            && GetPairs(b).TryGetValue(out var kvps)
            && args[2].IsAbstract<Dict>().TryGetValue(out var d))
        {
            if (kvps.Select(k => k.Key).Any(k => d.Dictionary.ContainsKey(k) && !d.Dictionary[k].Unify(kvps[k]).TryGetValue(out _)))
            {
                yield return False();
                yield break;
            }
            var diff = d.Dictionary.Keys.Except(kvps.Select(k => k.Key)).ToHashSet();
            var merged = new Set(d.Dictionary.Where(kvp => diff.Contains(kvp.Key))
                .Select(x => (ITerm)WellKnown.Operators.NamedArgument.ToComplex(x.Key, Maybe.Some(x.Value))), default);
            if (args[0].Unify(merged).TryGetValue(out var subs))
            {
                yield return True(subs);
                yield break;
            }
        }

        yield return False();

        static Dict Update(Dict d, IEnumerable<KeyValuePair<Atom, ITerm>> kvps)
        {
            var builder = d.Dictionary.ToBuilder();
            foreach (var (key, value) in kvps)
            {
                builder.Remove(key);
                builder.Add(key, value);
            }
            return new(d.Functor, builder, d.Scope);
        }

        static Maybe<Dictionary<Atom, ITerm>> GetPairs(Set set)
        {
            var ret = new List<KeyValuePair<Atom, ITerm>>();
            foreach (var item in set.Contents)
            {
                if (item is Complex { Arity: 2 } c && WellKnown.Operators.NamedArgument.Synonyms.Contains(c.Functor)
                    && c.Arguments[0] is Atom a)
                    ret.Add(new(a, c.Arguments[1]));
                else return default;
            }
            return ret.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
