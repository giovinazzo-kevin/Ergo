
using Ergo.Modules.Libraries;
namespace Ergo.Modules;

public sealed class ErgoModule(Atom name, Maybe<IErgoLibrary> lib)
{
    public readonly record struct PredicateInfo(Maybe<ImmutableArray<char>> Arguments, bool IsDynamic, bool IsExported);

    public readonly Atom Name = name;
    public  Maybe<IErgoLibrary> Library = lib;
    public int LoadOrder { get; internal set; }

    public readonly List<ErgoModule> Imports = [];
    public readonly HashSet<Operator> Operators = [];
    public readonly List<Clause> Clauses = [];
    public readonly Dictionary<Signature, PredicateInfo> MetaTable = [];

    public PredicateInfo GetMetaTableEntry(Signature sig)
    {
        if (MetaTable.TryGetValue(sig, out var info))
            return info;
        return new();
    }
}
