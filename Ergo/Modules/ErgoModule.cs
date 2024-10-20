
using Ergo.Modules.Libraries;
namespace Ergo.Modules;

public sealed class ErgoModule(ErgoModuleTree tree, Atom name, Maybe<IErgoLibrary> lib)
{
    public readonly Atom Name = name;
    public  Maybe<IErgoLibrary> Library = lib;

    private readonly List<ErgoModule> _imports = [];
    public IReadOnlyList<ErgoModule> ImportedModules => _imports;

    private readonly HashSet<Signature> _exports = [];
    public IReadOnlySet<Signature> ExportedPredicates => _exports;

    public void Import(Atom moduleName)
    {
        if (!tree[moduleName].TryGetValue(out var importedModule))
            throw new InvalidOperationException($"Module {moduleName.Explain()} is not defined");
        _imports.Add(importedModule);
    }

    public void Export(params IEnumerable<Signature> sigs)
    {
        _exports.UnionWith(sigs);
    }
}
