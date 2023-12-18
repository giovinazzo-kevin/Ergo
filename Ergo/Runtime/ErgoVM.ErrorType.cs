namespace Ergo.Runtime;

public partial class ErgoVM
{
    public enum ErrorType
    {
        MatchFailed,
        StackEmpty,
        StackOverflow,
        ExpectedTermOfTypeAt,
        TermNotSufficientlyInstantiated,
        KeyNotFound,
        CannotRetractImportedPredicate,
        CannotRetractStaticPredicate,
        UndefinedPredicate,
        ExpectedNArgumentsGotM,
    }
}
