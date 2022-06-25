namespace Ergo.Solver;

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
