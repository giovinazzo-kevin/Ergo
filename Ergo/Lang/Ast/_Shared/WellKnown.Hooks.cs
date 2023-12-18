namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Hooks
    {
        public static class IO
        {
            public static readonly Signature Portray_1 = new(new("portray"), Maybe.Some(1), Modules.IO, default);
        }

    }

}
