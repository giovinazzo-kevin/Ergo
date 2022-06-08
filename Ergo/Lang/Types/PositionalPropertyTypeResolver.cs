using Ergo.Lang.Ast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{

    internal class PositionalPropertyTypeResolver<T> : ErgoPropertyResolver<T>
    {
        public override TermMarshalling Marshalling => TermMarshalling.Positional;
        public override IEnumerable<string> GetMembers() => Properties.Select((p, i) => i.ToString());
        public override ITerm TransformMember(string name, ITerm value) => value;
        public override IEnumerable<string> GetArguments(Complex value) => value.Arguments.Select((a, i) => i.ToString());
        public override ITerm GetArgument(string name, Complex value) => value.Arguments[int.Parse(name)];
        public override Type GetMemberType(string name) => Properties[int.Parse(name)].PropertyType;
        public override object GetMemberValue(string name, object instance) =>  Properties[int.Parse(name)].GetValue(instance);
        public override void SetMemberValue(string name, object instance, object value) => Properties[int.Parse(name)].SetValue(instance, value);
        public override TermAttribute GetMemberAttribute(string name) => Properties[int.Parse(name)].GetCustomAttribute<TermAttribute>();
        public override Type GetParameterType(string name, ConstructorInfo info) => info.GetParameters()[int.Parse(name)].ParameterType;
        public PositionalPropertyTypeResolver() : base() { }
    }
}
