using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.List;

public class Set(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<Union>
    , IExportsBuiltIn<IsSet>
    ;