namespace Ergo.Lang.Ast
{
    public readonly struct Literal
    {
        public readonly Atom Key;
        public readonly ITerm Value;

        public Literal(Atom key, ITerm value)
        {
            Key = key;
            Value = value;
        }
    }
}
