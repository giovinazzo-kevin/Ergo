
using Ergo.Modules.Libraries;
namespace Ergo.Modules;

public sealed class ErgoModule(Atom name, Maybe<IErgoLibrary> lib)
{
    public readonly record struct PredicateInfo(Maybe<ImmutableArray<char>> MetaArguments, bool IsDynamic);

    public readonly Atom Name = name;
    public  Maybe<IErgoLibrary> Library = lib;
    public readonly List<ErgoModule> Imports = [];
    public readonly HashSet<Signature> Exports = [];
    public readonly HashSet<Operator> Operators = [];
    public readonly List<Clause> Clauses = [];
    public readonly Dictionary<Signature, PredicateInfo> MetaPredicateTable = [];
}
