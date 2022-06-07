using Ergo.Lang.Ast;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    public abstract class ErgoPropertyResolver<T> : ErgoTypeResolver<T>
    {
        protected readonly ConcurrentDictionary<string, PropertyInfo> PropertiesByName;
        protected readonly PropertyInfo[] Properties;
        public ErgoPropertyResolver()
        {
            Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToArray();
            PropertiesByName = new(Properties.ToDictionary(p => p.Name));
        }
    }
}
