namespace Ergo.Lang;

public partial class ErgoParser
{
    public enum ErrorType
    {
        UnexpectedEndOfFile
        , PredicateHasSingletonVariables
        , ExpectedArgumentDelimiterOrClosedParens
        , ExpectedPredicateDelimiterOrTerminator
        , ExpectedClauseList
        , KeyExpected
        , UnterminatedClauseList
        , ComplexHasNoArguments
        , OperatorDoesNotExist
        , TermHasIllegalName
        , MismatchedParentheses
    }
}
