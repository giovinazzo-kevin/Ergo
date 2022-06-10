using Ergo.Lang.Ast;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    /*  C#                                                          |   Named Resolution
        Person() {                                                  |   person(
            FirstName = "First",                                    |
            LastName = "Last", 
            Email = "test@gmail.com", 
            Gender = "male"
        }   
     */

    internal class NamedPropertyTypeResolver<T> : ErgoPropertyResolver<T>
    {
        public override TermMarshalling Marshalling => TermMarshalling.Named;
        public override IEnumerable<string> GetMembers() => Properties.Select(p => p.Name);
        public override ITerm TransformMember(string name, ITerm value) => new Complex(new(name.ToLower()), value);
        public override ITerm GetArgument(string name, Complex value)
        {
            var index = Array.IndexOf(GetMembers().ToArray(), name);
            if (index == -1)
                throw new InvalidOperationException();
            if(index >= value.Arguments.Length)
                return WellKnown.Literals.Discard;
            return value.Arguments[index];
        }
        public override Type GetMemberType(string name) => PropertiesByName[name].PropertyType;
        public override object GetMemberValue(string name, object instance) => PropertiesByName[name].GetValue(instance);
        public override void SetMemberValue(string name, object instance, object value) => PropertiesByName[name].SetValue(instance, value);
        public override TermAttribute GetMemberAttribute(string name) => PropertiesByName[name].GetCustomAttribute<TermAttribute>();
        public override Type GetParameterType(string name, ConstructorInfo info) => info.GetParameters().Single(p => p.Name.Equals(name)).ParameterType;

        public NamedPropertyTypeResolver() : base() { }
    }
}
