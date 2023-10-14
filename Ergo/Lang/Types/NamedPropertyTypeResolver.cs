using System.Reflection;

namespace Ergo.Lang;

/// <summary>
/// Transforms C# objects to and from Ergo Dicts with named, optional arguments.
/// </summary>
internal class NamedPropertyTypeResolver<T> : ErgoPropertyResolver<T>
{
    public override TermMarshalling Marshalling => TermMarshalling.Named;
    public override IEnumerable<string> GetMembers() => Properties.Select(p => p.Name);
    public override ITerm TransformMember(string name, Maybe<string> key, ITerm value) =>
        new Complex(WellKnown.Operators.NamedArgument.CanonicalFunctor, new Atom(key.GetOr(name).ToErgoCase()), value)
            .AsOperator(WellKnown.Operators.NamedArgument);
    public override ITerm GetArgument(string name, ITerm value)
    {
        if (value is not Dict dict)
            throw new NotSupportedException();
        if (!dict.Dictionary.TryGetValue(new Atom(name.ToErgoCase()), out var arg))
            return WellKnown.Literals.Discard;
        return arg;
    }
    public override Type GetMemberType(string name) => PropertiesByName[name].PropertyType;
    public override object GetMemberValue(string name, object instance) => PropertiesByName[name].GetValue(instance);
    public override void SetMemberValue(string name, object instance, object value)
    {
        PropertiesByName[name].SetValue(instance, value);
    }
    public override TermAttribute GetMemberAttribute(string name) => PropertiesByName[name].GetCustomAttribute<TermAttribute>();
    public override Type GetParameterType(string name, ConstructorInfo info) => info.GetParameters().Single(p => p.Name.Equals(name)).ParameterType;
    public override ITerm TransformTerm(Atom functor, ITerm[] args) => new Dict(functor, args
        .Select((a) => new KeyValuePair<Atom, ITerm>((Atom)((Complex)a).Arguments[0], ((Complex)a).Arguments[1])), functor.Scope);
    public override ITerm CycleDetectedLiteral(Atom functor)
        => new Dict(functor, new KeyValuePair<Atom, ITerm>[] {
            new(new Atom("_error").AsQuoted(false), new Atom("<cycle detected>")) }, functor.Scope);
    public NamedPropertyTypeResolver() : base() { }
}
