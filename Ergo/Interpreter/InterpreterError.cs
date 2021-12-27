namespace Ergo.Interpreter
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
        , OperatorClash
        , AmbiguousLiteral
        , ModuleRedefinition
        , UndefinedPredicate
    }
}
