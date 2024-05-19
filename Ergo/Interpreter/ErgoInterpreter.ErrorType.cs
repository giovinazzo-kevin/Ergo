namespace Ergo.Interpreter;

public partial class ErgoInterpreter
{
    public enum ErrorType
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
        , ModuleNameDoesNotMatchFileName
        , TransformationFailed
        , CantInlineCyclicalGoal
        , CantInlineForeignGoal
    }
}
