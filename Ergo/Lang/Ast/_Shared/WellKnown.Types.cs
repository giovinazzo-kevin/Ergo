namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Types
    {
        public const string Number = nameof(Number);
        public const string Integer = nameof(Integer);
        public const string Boolean = nameof(Boolean);
        public const string FreeVariable = nameof(FreeVariable);
        public const string String = nameof(String);
        public const string Atom = nameof(Atom);
        public const string Functor = nameof(Functor);
        public const string Complex = nameof(Complex);
        public const string Sequence = nameof(Sequence);
        public const string Comparison = nameof(Comparison);
        public const string CommaList = nameof(CommaList);
        public const string List = nameof(List);
        public const string Error = nameof(Error);
        public const string ModuleName = nameof(ModuleName);
        public const string Lambda = nameof(Lambda);
        public const string LambdaParameters = nameof(LambdaParameters);
        public const string Signature = nameof(Signature);
        public const string Predicate = nameof(Predicate);
        public static class Managed
        {
            public const string Stream = nameof(Managed) + "." + nameof(Stream);
        }
    }

}
