﻿using System.Text;

namespace Ergo.Runtime.BuiltIns;

public sealed class Read : ErgoBuiltIn
{
    public Read()
        : base("", new("read"), 1, WellKnown.Modules.IO)
    {
    }

    public override Op Compile() => vm =>
    {
        var sb = new StringBuilder();
        int ch;
        Maybe<ITerm> maybeTerm = default;
        while ((ch = vm.In.Read()) != -1)
        {
            sb.Append((char)ch);
            if (ch == '\n')
            {
                maybeTerm = vm.KB.Scope.Parse<ITerm>(sb.ToString());
                if (maybeTerm.TryGetValue(out _))
                    break;
            }
        }
        if (!maybeTerm.TryGetValue(out ITerm term))
        {
            vm.Fail();
            return;
        }
        while ((ch = vm.In.Peek()) != -1 && ch != '\n')
            vm.In.Read();
        vm.SetArg(1, term);
        ErgoVM.Goals.Unify2(vm);
    };
}