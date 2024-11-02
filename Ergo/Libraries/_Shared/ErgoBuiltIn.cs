using Ergo.Lang.Compiler;
using System.Collections;
using System.Diagnostics;

namespace Ergo.Runtime.BuiltIns;

public abstract class ErgoBuiltIn(string documentation, Atom functor, Maybe<int> arity, Atom module)
{
    public readonly Signature Signature = new(functor, arity, module, Maybe<Atom>.None);
    public readonly string Documentation = documentation;
    public abstract Op Compile();
}
