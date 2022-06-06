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
        protected override Type GetMemberType(string name) => PropertiesByName[name].PropertyType;
        protected override object GetMemberValue(string name, object instance) => PropertiesByName[name].GetValue(instance);
        protected override void SetMemberValue(string name, object instance, object value) => PropertiesByName[name].SetValue(instance, value);
        protected override TermAttribute GetMemberAttribute(string name) => PropertiesByName[name].GetCustomAttribute<TermAttribute>();
        protected override ITerm TransformTerm(Atom functor, ITerm[] args) => new Complex(functor, args);
    }
}
