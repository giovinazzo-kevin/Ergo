namespace Ergo.Lang
{
    public readonly struct Query
    {
        public readonly Sequence Goals;

        public Query(Sequence goals)
        {
            Goals = goals;
        }
    }
}
