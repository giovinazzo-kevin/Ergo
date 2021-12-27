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
        , LiteralClashWithBuiltIn
        , LiteralClash
        , ModuleRedefinition
        , UndefinedPredicate
    }
}
