namespace Ergo.Interpreter
{
    public enum InterpreterError
    {
        CouldNotLoadFile
        , ExpectedTermOfTypeAt
        , ModuleAlreadyImported
        , ModuleNameClash
        , OperatorClash
        , ExpansionClashWithLiteral
        , ExpansionClash
        , LiteralCyclicDefinition
        , ModuleRedefinition
        , UndefinedDirective
    }
}
