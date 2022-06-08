namespace Ergo.Interpreter
{
    public enum InterpreterError
    {
        CouldNotLoadFile
        , ExpectedTermOfTypeAt
        , ModuleAlreadyImported
        , ModuleNameClash
        , OperatorClash
        , LiteralClashWithBuiltIn
        , LiteralClash
        , LiteralCyclicDefinition
        , ModuleRedefinition
        , UndefinedDirective
    }
}
