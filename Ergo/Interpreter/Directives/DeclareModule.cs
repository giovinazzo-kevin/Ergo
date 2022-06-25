namespace Ergo.Interpreter.Directives;

public class DeclareModule : InterpreterDirective
{
    public DeclareModule()
        : base("", new("module"), Maybe.Some(2), 0)
    {
    }
    public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
    {
        if (args[0] is not Atom moduleName)
        {
            throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, WellKnown.Types.String, args[0].Explain());
        }

        if (!scope.IsRuntime && scope.Entry != WellKnown.Modules.User)
        {
            throw new InterpreterException(InterpreterError.ModuleRedefinition, scope, scope.Entry.Explain(), moduleName.Explain());
        }

        if (!args[1].IsAbstract<List>(out var exports))
        {
            throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, WellKnown.Types.List, args[1].Explain());
        }

        if (scope.Modules.TryGetValue(moduleName, out var module))
        {
            if (!module.IsRuntime)
            {
                throw new InterpreterException(InterpreterError.ModuleNameClash, scope, moduleName.Explain());
            }

            module = module.WithExports(exports.Contents);
        }
        else
        {
            module = new Module(moduleName, runtime: scope.IsRuntime)
                .WithExports(exports.Contents)
                .WithImport(WellKnown.Modules.Stdlib);
        }

        scope = scope
            .WithModule(module)
            .WithCurrentModule(module.Name);
        foreach (var item in exports.Contents)
        {
            // make sure that 'item' is in the form 'predicate/arity'
            if (!item.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, WellKnown.Types.Signature, item.Explain());
            }
        }

        return true;
    }
}
