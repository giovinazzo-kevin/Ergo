namespace Ergo.Shell;

public partial class ErgoShell
{
    public readonly struct LogLine
    {
        public readonly string Message;
        public readonly LogLevel Level;
        public readonly DateTime TimeStamp;

        public LogLine(string msg, LogLevel level, DateTime timeStamp)
        {
            Message = msg;
            TimeStamp = timeStamp;
            Level = level;
        }
    }
}
