using System;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    public partial class Shell
    {

        public partial class CommandDispatcher
        {
            public readonly struct Dispatcher
            {
                public readonly string Name;
                public readonly string Description;
                public readonly Regex Expression;
                public readonly Action<Match> Callback;

                public Dispatcher(string name, string desc, Regex exp, Action<Match> callback)
                {
                    Name = name;
                    Description = desc;
                    Expression = exp;
                    Callback = callback;
                }
            }
        }
    }
}
