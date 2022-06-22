namespace Ergo.Interpreter;

public enum InterpreterError
{
    CouldNotLoadFile
    , ExpectedTermOfTypeAt
    , ModuleAlreadyImported
    , ModuleNameClash
    , OperatorClash
    , ExpansionClashWithLiteral
    , ExpansionLambdaShouldHaveOneVariable
    , ExpansionIsNotUsingLambdaVariable
    , ExpansionClash
    , LiteralCyclicDefinition
    , ModuleRedefinition
    , UndefinedDirective
}
