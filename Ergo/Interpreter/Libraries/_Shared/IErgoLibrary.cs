using Ergo.Events;
using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries;

// see https://github.com/G3Kappa/Ergo/issues/10
public interface IErgoLibrary
{
    int LoadOrder => 0;
    Atom Module { get; }
    IEnumerable<ErgoDirective> ExportedDirectives { get; }
    IEnumerable<ErgoBuiltIn> ExportedBuiltins { get; }
    void OnErgoEvent(ErgoEvent evt) { }

}
