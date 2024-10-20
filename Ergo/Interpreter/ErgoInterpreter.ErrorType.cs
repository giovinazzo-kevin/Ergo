﻿namespace Ergo.Modules;

public partial class ErgoInterpreter
{
    public enum ErrorType
    {
        CouldNotLoadFile
        , ExpectedTermOfTypeAt
        , UndefinedModule
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
