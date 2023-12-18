namespace Ergo.Runtime.BuiltIns;

public abstract class NthBase : BuiltIn
{
    public readonly int Offset;

    public NthBase(int offset)
        : base("", new($"nth{offset}"), Maybe<int>.Some(3), WellKnown.Modules.List) => Offset = offset;

    public override ErgoVM.Op Compile() => vm =>
    {
        var args = vm.Args;
        if (args[0].Matches<int>(out var index))
        {
            index -= Offset;
            if (args[1] is List list && index >= 0 && index < list.Contents.Length)
            {
                var elem = list.Contents[index];
                vm.SetArg(0, args[2]);
                vm.SetArg(1, elem);
                ErgoVM.Goals.Unify2(vm);
            }
            else if (!args[1].IsGround)
            {
                var contents = Enumerable.Range(0, index)
                    .Select(x => (ITerm)new Variable("_"))
                    .Append(args[2]);
                vm.SetArg(0, args[1]);
                vm.SetArg(1, new List(contents, default, args[1].Scope));
                ErgoVM.Goals.Unify2(vm);
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
                        vm.Solution(subs);
                    }
                }
                if (!any)
                    vm.Fail();
            }
            else if (!args[1].IsGround)
            {
                vm.Solution();
            }
        }
        else vm.Fail();
    };
}

