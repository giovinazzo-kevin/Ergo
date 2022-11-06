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
    , ExpansionBodyMustReferenceLambdaVariable
    , ExpansionBodyMustReferenceHeadVariables
    , ExpansionHeadCantReferenceLambdaVariable
    , ExpansionClash
    , CyclicLiteralDefinition
    , ModuleRedefinition
    , UndefinedDirective
}
