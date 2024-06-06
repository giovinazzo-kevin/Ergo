namespace Ergo.Runtime.BuiltIns;

public abstract class NthBase(int offset) : BuiltIn("", new($"nth{offset}"), Maybe<int>.Some(3), WellKnown.Modules.List)
{
    public readonly int Offset = offset;

    public override ErgoVM.Op Compile() => vm =>
    {
        if (vm.Arg(0).Match<int>(out var index))
        {
            index -= Offset;
            if (vm.Arg(1) is List list && index >= 0 && index < list.Contents.Length)
            {
                var elem = list.Contents[index];
                vm.SetArg(0, vm.Arg(2));
                vm.SetArg(1, elem);
                ErgoVM.Goals.Unify2(vm);
            }
            else if (!vm.Arg(1).IsGround)
            {
                var contents = Enumerable.Range(0, index)
                    .Select(x => (ITerm)(Variable)"_")
                    .Append(vm.Arg(2));
                vm.SetArg(0, vm.Arg(1));
                vm.SetArg(1, new List(contents, default, vm.Arg(1).Scope));
                ErgoVM.Goals.Unify2(vm);
            }
        }
        else if (!vm.Arg(0).IsGround)
        {
            if (vm.Arg(1) is List list)
            {
                var any = false;
                for (var i = 0; i < list.Contents.Length; ++i)
                {
                    var elem = list.Contents[i];
                    if (LanguageExtensions.Unify(vm.Arg(2), elem).TryGetValue(out var subs))
                    {
                        any = true;
                        subs.Add(new(vm.Arg(0), (Atom)(i + Offset)));
                        vm.Solution(subs);
                    }
                }
                if (!any)
                    vm.Fail();
            }
            else if (!vm.Arg(1).IsGround)
            {
                vm.Solution();
            }
        }
        else vm.Fail();
    };
}

