using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Reflection;

public class Reflection(IServiceProvider sp) : ErgoLibrary(sp)
    , IExportsBuiltIn<AnonymousComplex>
    , IExportsBuiltIn<CommaToList>
    , IExportsBuiltIn<CopyTerm>
    , IExportsBuiltIn<Ground>
    , IExportsBuiltIn<CurrentModule>
    , IExportsBuiltIn<Nonvar>
    , IExportsBuiltIn<Number>
    , IExportsBuiltIn<NumberVars>
    , IExportsBuiltIn<SequenceType>
    , IExportsBuiltIn<Term>
    , IExportsBuiltIn<TermType>
    , IExportsBuiltIn<Variant>
    , IExportsBuiltIn<Explain>
    ;