namespace Ergo.Shell;

public readonly struct LogLine(string msg, LogLevel level, DateTime timeStamp)
{
    public readonly string Message = msg;
    public readonly LogLevel Level = level;
    public readonly DateTime TimeStamp = timeStamp;
}