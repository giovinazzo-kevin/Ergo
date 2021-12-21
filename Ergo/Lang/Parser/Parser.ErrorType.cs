namespace Ergo.Lang
{

    public partial class Parser
    {
        public enum ErrorType
        {
            UnexpectedEndOfFile
            , PredicateHasSingletonVariables
            , ExpectedArgumentDelimiterOrClosedParens
            , ExpectedPredicateDelimiterOrITerminator
            , ExpectedClauseList
            , UnITerminatedClauseList
            , ComplexHasNoArguments
            , OperatorDoesNotExist
            , ITermHasIllegalName
            , MismatchedParentheses
        }
    }
}
