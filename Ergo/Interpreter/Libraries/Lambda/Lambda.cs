using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Lambda;

public class Lambda(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<LambdaCall>
    ;