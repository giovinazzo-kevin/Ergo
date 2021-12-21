using System;

namespace Ergo.Lang
{

    internal interface ITypeResolver
    {
        Type Type { get; }
        ITerm ToITerm(object o);
        object FromITerm(ITerm t);
    }
}
