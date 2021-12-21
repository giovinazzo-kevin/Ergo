namespace Ergo.Lang
{
    public enum InterpreterError
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
