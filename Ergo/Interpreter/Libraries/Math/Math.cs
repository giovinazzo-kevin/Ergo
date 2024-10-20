using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Math;

public class Math(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<Eval>
    , IExportsBuiltIn<NumberString>
    ;