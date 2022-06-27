namespace Ergo.Lang.Exceptions;

public enum InterpreterError
{
    CouldNotLoadFile
    , ExpectedTermOfTypeAt
    , ModuleAlreadyImported
    , ModuleNameClash
    , CouldNotParseTerm
    , OperatorClash
    , ExpansionClashWithLiteral
    , ExpansionLambdaShouldHaveOneVariable
    , ExpansionIsNotUsingLambdaVariable
    , ExpansionClash
    , LiteralCyclicDefinition
    , ModuleRedefinition
    , UndefinedDirective
}
