using Ergo.Lang.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{

    internal class PositionalPropertyTypeResolver<T> : ErgoPropertyResolver<T>
    {
        public override TermMarshalling Marshalling => TermMarshalling.Positional;
        protected override IEnumerable<string> GetMembers() => Properties.Select(p => p.Name);
        protected override ITerm TransformMember(string name, ITerm value) => value;
        protected override IEnumerable<string> GetArguments(Complex value) => value.Arguments.Select((a, i) => i.ToString());
        protected override ITerm GetArgument(string name, Complex value) => value.Arguments[int.Parse(name)];
        public PositionalPropertyTypeResolver() : base() { }
    }
}
