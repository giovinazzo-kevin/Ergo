using System.Diagnostics;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Goals.Explain() }")]
    public readonly struct Query
    {
        public readonly CommaSequence Goals;

        public Query(CommaSequence goals)
        {
            Goals = goals;
        }
    }
}
