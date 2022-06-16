using System.Reflection;

namespace Ergo.Lang;

/// <summary>
/// Transforms C# objects to and from Ergo Dicts with named, optional arguments.
/// </summary>
internal class NamedPropertyTypeResolver<T> : ErgoPropertyResolver<T>
{
    public override TermMarshalling Marshalling => TermMarshalling.Named;
    public override IEnumerable<string> GetMembers() => Properties.Select(p => p.Name);
    public override ITerm TransformMember(string name, ITerm value) =>
        new Complex(WellKnown.Functors.NamedArgument.First(), new Atom(ToErgoCase(name)), value)
            .AsOperator(OperatorAffix.Infix);
    public override ITerm GetArgument(string name, ITerm value)
    {
        if (!value.IsAbstractTerm<Dict>(out var dict))
            throw new NotSupportedException();
        if (!dict.Dictionary.TryGetValue(new Atom(ToErgoCase(name)), out var arg))
            return WellKnown.Literals.Discard;
        return arg;
    }
    public override Type GetMemberType(string name) => PropertiesByName[name].PropertyType;
    public override object GetMemberValue(string name, object instance) => PropertiesByName[name].GetValue(instance);
    public override void SetMemberValue(string name, object instance, object value) => PropertiesByName[name].SetValue(instance, value);
    public override TermAttribute GetMemberAttribute(string name) => PropertiesByName[name].GetCustomAttribute<TermAttribute>();
    public override Type GetParameterType(string name, ConstructorInfo info) => info.GetParameters().Single(p => p.Name.Equals(name)).ParameterType;
    public override ITerm TransformTerm(Atom functor, ITerm[] args) => new Dict(functor, args
        .Select((a) => new KeyValuePair<Atom, ITerm>((Atom)((Complex)a).Arguments[0], ((Complex)a).Arguments[1])))
        .CanonicalForm;

    public NamedPropertyTypeResolver() : base() { }
}
