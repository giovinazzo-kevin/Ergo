namespace Ergo.Lang;

public interface ITermConverter
{
    Type Type { get; }
    TermMarshalling Marshalling { get; }
    ITerm ToTerm(object o, Maybe<Atom> overrideFunctor = default, Maybe<TermMarshalling> overrideMarshalling = default, TermMarshallingContext ctx = null);
    object FromTerm(ITerm t);
}
