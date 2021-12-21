using System;

namespace Ergo.Lang
{
    public readonly partial struct BuiltIn
    {
        public readonly Atom Functor;
        public readonly int Arity;
        public readonly string Signature;
        public readonly string Documentation;

        public readonly Func<ITerm, Atom, Evaluation> Apply { get; }

        public BuiltIn(string documentation, Atom functor, int arity, Func<ITerm, Atom, Evaluation> apply)
        {
            Functor = functor;
            Arity = arity;
            Apply = apply;
            Signature = $"{functor.Explain()}/{arity}";
            Documentation = documentation;
        }

        public BuiltIn WithArity(int newArity) => new(Documentation, Functor, newArity, Apply);
    }

}
