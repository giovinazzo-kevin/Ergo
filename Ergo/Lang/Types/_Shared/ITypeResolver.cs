using Ergo.Lang.Ast;
using System;

namespace Ergo.Lang
{

    internal interface ITypeResolver
    {
        Type Type { get; }
        ITerm ToTerm(object o);
        object FromTerm(ITerm t);
    }
}
