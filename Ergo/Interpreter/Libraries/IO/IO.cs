using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.IO;

public class IO(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<Write>
    , IExportsBuiltIn<WriteCanonical>
    , IExportsBuiltIn<WriteQuoted>
    , IExportsBuiltIn<WriteDict>
    , IExportsBuiltIn<WriteRaw>
    , IExportsBuiltIn<Read>
    , IExportsBuiltIn<ReadLine>
    , IExportsBuiltIn<GetChar>
    , IExportsBuiltIn<GetSingleChar>
    , IExportsBuiltIn<PeekChar>
    ;