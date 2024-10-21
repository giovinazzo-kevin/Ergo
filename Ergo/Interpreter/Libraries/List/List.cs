using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.List;

public class List(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<Nth0>
    , IExportsBuiltIn<Nth1>
    , IExportsBuiltIn<Sort>
    , IExportsBuiltIn<ListSet>
    ;