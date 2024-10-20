using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Meta;

public class Meta(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<BagOf>
    , IExportsBuiltIn<For>
    , IExportsBuiltIn<Call>
    , IExportsBuiltIn<FindAll>
    , IExportsBuiltIn<SetOf>
    , IExportsBuiltIn<SetupCallCleanup>
    , IExportsBuiltIn<Choose>
    ;