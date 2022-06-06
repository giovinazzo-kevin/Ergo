using Ergo.Lang.Ast;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    internal class NamedPropertyTypeResolver<T> : ErgoPropertyResolver<T>
    {
        public override TermMarshalling Marshalling => TermMarshalling.Named;
        protected override IEnumerable<string> GetMembers() => PropertiesByName.Keys;
        protected override ITerm TransformMember(string name, ITerm value) => new Complex(new(name.ToLower()), value);
        protected override IEnumerable<string> GetArguments(Complex value) => value.Arguments.Select(a => (string)((Complex)a).Functor.Value);
        protected override ITerm GetArgument(string name, Complex value) => value.Arguments.Single(a => name.Equals((string)((Complex)a).Functor.Value));
        public NamedPropertyTypeResolver() : base() { }
    }
}
