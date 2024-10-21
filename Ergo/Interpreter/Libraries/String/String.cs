using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.String;

public class String(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<FormatString>
    ;
