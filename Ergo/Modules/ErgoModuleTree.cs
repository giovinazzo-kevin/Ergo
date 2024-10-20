
using Ergo.Events.Interpreter;
using Ergo.Events.Modules;
using Ergo.Modules.Directives;
using Ergo.Modules.Libraries;
using Ergo.Runtime.BuiltIns;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
namespace Ergo.Modules;

public sealed class ErgoModuleTree(IServiceProvider serviceProvider)
{
    private readonly Dictionary<Atom, ErgoModule> _modules = [];
    private readonly Dictionary<Signature, ErgoDirective> _directives;
    private readonly Dictionary<Signature, ErgoBuiltIn> _builtins;

    public IReadOnlyDictionary<Signature, ErgoDirective> Directives => _directives;
    public IReadOnlyDictionary<Signature, ErgoBuiltIn> BuiltIns => _builtins;

    public Maybe<ErgoModule> this[Atom key]
    {
        get => Maybe.FromTryGet(() => (_modules.TryGetValue(key, out var val), val));
    }

    public ErgoModule Define(Atom moduleName)
    {
        if (_modules.ContainsKey(moduleName))
            throw new InterpreterException(ErgoInterpreter.ErrorType.ModuleRedefinition, default);
        var library = Maybe.FromNullable(serviceProvider.GetKeyedService<IErgoLibrary>(moduleName.Explain()));
        library.Do(some =>
        {
            foreach (var directive in some.ExportedDirectives)
                _directives.Add(directive.Signature, directive);
            foreach (var builtin in some.ExportedBuiltins)
                _builtins.Add(builtin.Signature, builtin);
        });
        return _modules[moduleName] = new(this, moduleName, library);
    }
}
