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
        , LiteralCircularDefinition
        , ModuleRedefinition
        , UndefinedDirective
    }
}
