namespace Ergo.Lang.Exceptions;

public abstract class ErgoException : Exception
{
    protected ErgoException(string message) : base(message)
    {
    }
}
