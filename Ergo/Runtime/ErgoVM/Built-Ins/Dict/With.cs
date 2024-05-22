namespace Ergo.Runtime.BuiltIns;

public sealed class With : BuiltIn
{
    const int Arity = 3;

    public With()
        : base("", new($"with"), Maybe<int>.Some(Arity), WellKnown.Modules.Dict)
    {
    }

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args[0] is Dict a)
        {
            if (args[1] is Set b
                && GetPairs(b).TryGetValue(out var kvps))
            {
                var merged = Update(a, kvps);
                vm.SetArg(0, args[2]);
                vm.SetArg(1, merged);
                ErgoVM.Goals.Unify2(vm);
            }
            else if (args[2] is Dict d)
            {
                if (a.Dictionary.Keys.Any(k => d.Dictionary.ContainsKey(k) && !LanguageExtensions.Unify(d.Dictionary[k], a.Dictionary[k]).TryGetValue(out _)))
                {
                    vm.Fail();
                }
                var diff = d.Dictionary.Keys.Except(a.Dictionary.Keys).ToHashSet();
                var merged = new Set(d.Dictionary.Where(kvp => diff.Contains(kvp.Key))
                    .Select(x => (ITerm)WellKnown.Operators.NamedArgument.ToComplex(x.Key, Maybe.Some(x.Value))), default);
                vm.SetArg(0, args[1]);
                vm.SetArg(1, merged);
                ErgoVM.Goals.Unify2(vm);
            }
        }
        else if (args[0] is Variable
            && args[1] is Set b
            && GetPairs(b).TryGetValue(out var kvps)
            && args[2] is Dict d)
        {
            if (kvps.Select(k => k.Key).Any(k => d.Dictionary.ContainsKey(k) && !LanguageExtensions.Unify(d.Dictionary[k], kvps[k]).TryGetValue(out _)))
            {
                vm.Fail();
            }
            var diff = d.Dictionary.Keys.Except(kvps.Select(k => k.Key)).ToHashSet();
            var merged = new Set(d.Dictionary.Where(kvp => diff.Contains(kvp.Key))
                .Select(x => (ITerm)WellKnown.Operators.NamedArgument.ToComplex(x.Key, Maybe.Some(x.Value))), default);
            vm.SetArg(1, merged);
            ErgoVM.Goals.Unify2(vm);
        }
        else vm.Fail();

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
    };
}
