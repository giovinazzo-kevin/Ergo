using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Dict;

public class Dict(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<DictKeyValue>
    , IExportsBuiltIn<With>;