namespace Ergo.Lang;

public partial class LegacyErgoParser
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
