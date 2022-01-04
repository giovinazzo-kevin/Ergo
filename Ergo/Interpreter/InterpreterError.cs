namespace Ergo.Interpreter
{
    public enum InterpreterError
    {
        CouldNotLoadFile
        , ExpectedTermOfTypeAt
        , ModuleNameClash
        , OperatorClash
        , LiteralClashWithBuiltIn
        , LiteralClash
        , LiteralCyclicDefinition
        , ModuleRedefinition
        , UndefinedDirective
    }
}
