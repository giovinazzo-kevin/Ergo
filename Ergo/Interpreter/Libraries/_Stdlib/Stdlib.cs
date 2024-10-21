using Ergo.Modules.Directives;

namespace Ergo.Modules.Libraries._Stdlib;

public class Stdlib(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsDirective<DeclareInlinedPredicate>
    , IExportsDirective<DeclareModule>
    , IExportsDirective<DeclareOperator>
    , IExportsDirective<SetModule>
    , IExportsDirective<UseModule>
{
    public override int LoadOrder => 0;
}
