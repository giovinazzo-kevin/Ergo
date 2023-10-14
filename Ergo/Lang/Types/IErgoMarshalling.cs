namespace Ergo.Lang;

public interface IErgoMarshalling<T>
{
    ITerm ToTerm();
    T FromTerm(ITerm term);
}
