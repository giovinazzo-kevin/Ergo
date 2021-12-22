using Ergo.Lang.Ast;
using System.Diagnostics;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct BuiltInSignature
    {
        public readonly Atom Functor;
        public readonly Maybe<int> Arity;

        public BuiltInSignature(Atom a, Maybe<int> arity) => (Functor, Arity) = (a, arity);
        public BuiltInSignature WithArity(Maybe<int> arity) => new(Functor, arity);

        public string Explain() => $"{Functor.Explain()}/{Arity.Reduce(some => some.ToString(), () => "*")}";
    }
}
