namespace Ergo.Lang
{
    public enum ErrorType
    {
        CouldNotLoadFile
        , UnknownPredicate
        , UserPredicateConflictsWithBuiltIn
        , ExpectedTermOfTypeAt
        , UninstantiatedTermAt
        , ExpectedTermWithArity
        , ModuleNameClash
        , ModuleRedefinition
        , UndefinedPredicate
    }
}
