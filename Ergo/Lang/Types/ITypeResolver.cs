using System;

namespace Ergo.Lang
{
    internal interface ITypeResolver
    {
        Type Type { get; }
        Term ToTerm(object o);
        object FromTerm(Term t);
    }
}
