namespace Ergo.Lang
{
    public readonly partial struct BuiltIn
    {
        public readonly struct Evaluation
        {
            public readonly Term Result;
            public readonly Substitution[] Substitutions{ get; }

            public Evaluation(Term result, params Substitution[] subs)
            {
                Result = result;
                Substitutions = subs;
            }

        }

    }

}
