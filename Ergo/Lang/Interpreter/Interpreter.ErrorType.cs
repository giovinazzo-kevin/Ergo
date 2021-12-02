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
            , UninstantiatedTermAt
            , ExpectedTermWithArity
            , ExpectedAtomWithDomain
        }
    }
}
