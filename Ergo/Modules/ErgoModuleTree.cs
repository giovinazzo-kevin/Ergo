using Ergo.Modules.Directives;
using Ergo.Modules.Libraries;
using Ergo.Runtime.BuiltIns;
using Microsoft.Extensions.DependencyInjection;
namespace Ergo.Modules;

public sealed class ErgoModuleTree(IServiceProvider serviceProvider)
{
    private readonly Dictionary<Atom, ErgoModule> _modules = [];
    private readonly Dictionary<Signature, ErgoDirective> _directives = [];
    private readonly Dictionary<Signature, ErgoBuiltIn> _builtins = [];

    public IReadOnlyDictionary<Signature, ErgoDirective> Directives => _directives;
    public IReadOnlyDictionary<Signature, ErgoBuiltIn> BuiltIns => _builtins;
    public IReadOnlyDictionary<Atom, ErgoModule> Modules => _modules;
    public IEnumerable<Operator> Operators => _modules.Values.SelectMany(x => x.Operators);

    public T GetLibrary<T>()
        where T : IErgoLibrary
    {
        var key = new Atom(typeof(T).Name.ToErgoCase());
        return this[key]
            .Map(x => x.Library)
            .Select(x => (T)x)
            .GetOrThrow();
    }

    public Maybe<ErgoModule> this[Atom key]
    {
        get => Maybe.FromTryGet(() => (_modules.TryGetValue(key, out var val), val));
    }

    public ErgoModule Declare(Atom moduleName)
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
        return _modules[moduleName] = new(moduleName, library);
    }
}
