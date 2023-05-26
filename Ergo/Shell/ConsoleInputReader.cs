namespace Ergo.Shell;

public sealed class ConsoleInputReader : IAsyncInputReader
{
    public bool Blocking { get; private set; }

    public char ReadChar(bool intercept = false)
    {
        Blocking = true;
        var ch = Console.ReadKey(intercept).KeyChar;
        Blocking = false;
        return ch;
    }
}