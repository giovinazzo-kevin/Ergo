using System.Reflection;

namespace Ergo.Lang;

internal class PositionalPropertyTypeResolver<T> : ErgoPropertyResolver<T>
{
    public override TermMarshalling Marshalling => TermMarshalling.Positional;
    public override IEnumerable<string> GetMembers() => Properties.Select((p, i) => i.ToString());
    public override ITerm TransformMember(string name, Maybe<string> key, ITerm value) => value;
    public override ITerm GetArgument(string name, ITerm value) => ((Complex)value).Arguments[int.Parse(name)];
    public override ITerm TransformTerm(Atom functor, ITerm[] args) => new Complex(functor, args)
        .AsParenthesized(WellKnown.Functors.Conjunction.Contains(functor));
    public override bool IsMemberWriteable(string name) => Properties[int.Parse(name)].CanWrite;
    public override Type GetMemberType(string name) => Properties[int.Parse(name)].PropertyType;
    public override object GetMemberValue(string name, object instance) => Properties[int.Parse(name)].GetValue(instance);
    public override void SetMemberValue(string name, object instance, object value) => Properties[int.Parse(name)].SetValue(instance, value);
    public override TermAttribute GetMemberAttribute(string name) => Attributes[int.Parse(name)];
    public override Type GetParameterType(string name, ConstructorInfo info) => info.GetParameters()[int.Parse(name)].ParameterType;
    public override ITerm CycleDetectedLiteral(Atom functor)
        => new Dict(functor, [
            new(new Atom("_error").AsQuoted(false), new Atom("<cycle detected>")) ], functor.Scope);
    public PositionalPropertyTypeResolver() : base() { }
}
