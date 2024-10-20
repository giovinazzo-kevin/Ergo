using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Prologue;

public class Prologue(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<AssertA>
    , IExportsBuiltIn<AssertZ>
    , IExportsBuiltIn<Cut>
    , IExportsBuiltIn<Not>
    , IExportsBuiltIn<Retract>
    , IExportsBuiltIn<RetractAll>
    , IExportsBuiltIn<Unifiable>
    , IExportsBuiltIn<Unify>
    , IExportsBuiltIn<Compare>

    , IExportsDirective<DeclareMetaPredicate>
    ;