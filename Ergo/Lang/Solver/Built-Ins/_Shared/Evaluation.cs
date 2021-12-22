using Ergo.Lang.Ast;

namespace Ergo.Lang
{
    public readonly struct Evaluation
    {
        public readonly ITerm Result;
        public readonly Substitution[] Substitutions { get; }

        public Evaluation(ITerm result, params Substitution[] subs)
        {
            Result = result;
            Substitutions = subs;
        }

    }
}
