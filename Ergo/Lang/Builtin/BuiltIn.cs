using System;

namespace Ergo.Lang
{
    public readonly partial struct BuiltIn
    {
        public readonly Atom Functor { get; }
        public readonly int Arity { get; }
        public readonly string Signature { get; }
        public readonly string Documentation { get; }

        public readonly Func<Term, Evaluation> Apply { get; }

        public BuiltIn(string documentation, Atom functor, int arity, Func<Term, Evaluation> apply)
        {
            Functor = functor;
            Arity = arity;
            Apply = apply;
            Signature = $"{Atom.Explain(functor)}/{arity}";
            Documentation = documentation;
        }

        public BuiltIn WithArity(int newArity) => new BuiltIn(Documentation, Functor, newArity, Apply);
    }

}
