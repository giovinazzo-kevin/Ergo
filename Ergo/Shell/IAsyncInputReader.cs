namespace Ergo.Shell;

public interface IAsyncInputReader
{
    bool Blocking { get; }
    char ReadChar(bool intercept = false);
}
