namespace Ergo.Lang.Exceptions;

public enum SolverError
{
    TermNotSufficientlyInstantiated
    , ExpansionLacksEvalVariable
    , KeyNotFound
    , ExpectedTermOfTypeAt
    , CannotRetractImportedPredicate
    , CannotRetractStaticPredicate
    , UndefinedPredicate
}
