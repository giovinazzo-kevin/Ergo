namespace Ergo.Lang.Ast
{
    public readonly struct Expansion
    {
        public readonly ITerm Head;
        public readonly ITerm Value;

        public Expansion(ITerm key, ITerm value)
        {
            Head = key;
            Value = value;
        }
    }
}
