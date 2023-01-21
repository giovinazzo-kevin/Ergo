namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class OpCodes
    {
        public const byte Noop = 0;
        public const byte Cons = 1;
        public const byte Call = 2;
        public const byte Halt = 255;
    }

}
