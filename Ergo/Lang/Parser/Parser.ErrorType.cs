namespace Ergo.Lang
{

    public partial class Parser
    {
        public enum ErrorType
        {
            UnexpectedEndOfFile
            , PredicateHasSingletonVariables
            , ExpectedArgumentDelimiterOrClosedParens
            , ExpectedPredicateDelimiterOrTerminator
            , ExpectedClauseList
            , UnterminatedClauseList
            , ComplexHasNoArguments
            , OperatorDoesNotExist
            , TermHasIllegalName
        }
    }
}
