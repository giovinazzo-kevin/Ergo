using Ergo.Lang.Ast;
using System;

namespace Ergo.Lang
{

    internal interface ITypeResolver
    {
        Type Type { get; }
        TermMarshalling Marshalling { get; }
        ITerm ToTerm(object o, Maybe<Atom> overrideFunctor = default, Maybe<TermMarshalling> overrideMarshalling = default);
        object FromTerm(ITerm t);
    }
}
