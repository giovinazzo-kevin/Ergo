using System;

namespace Ergo.Lang
{

    public partial class Shell
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
}
