namespace Ergo.Lang.Exceptions;

public enum SolverError
{
    TermNotSufficientlyInstantiated
    , KeyNotFound
    , ExpectedTermOfTypeAt
    , CannotRetractImportedPredicate
    , CannotRetractStaticPredicate
    , UndefinedPredicate
    , ExpectedNArgumentsGotM
    , StackOverflow
}
