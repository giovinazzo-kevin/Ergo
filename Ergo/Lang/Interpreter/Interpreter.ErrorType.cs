namespace Ergo.Lang
{

    public partial class Interpreter
    {
        public enum ErrorType
        {
            CouldNotLoadFile
            , UnknownPredicate
            , UserPredicateConflictsWithBuiltIn
            , ExpectedTermOfTypeAt
            , UninstantiatedITermAt
            , ExpectedITermWithArity
            , ModuleNameClash
            , ModuleRedefinition
            , UndefinedPredicate
        }
    }
}
