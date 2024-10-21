using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.CSharp;

public class CSharp(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<InvokeOp>
    ;