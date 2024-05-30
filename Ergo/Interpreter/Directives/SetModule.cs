namespace Ergo.Interpreter.Directives;

public class SetModule : InterpreterDirective
{
    public SetModule()
        : base("", "module", 1, 0)
    {
    }

    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (args[0] is not Atom moduleName)
        {
            throw new InterpreterException(ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt, scope, WellKnown.Types.String, args[0].Explain());
        }

        if (!scope.IsRuntime)
        {
            return new DeclareModule().Execute(interpreter, ref scope, args[0], WellKnown.Literals.EmptyList);
        }

        var module = interpreter
            .EnsureModule(ref scope, moduleName);
        scope = scope
            .WithoutModule(module.Name)
            .WithModule(module)
            .WithCurrentModule(module.Name);
        return true;
    }
}
