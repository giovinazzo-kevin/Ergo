using System.Collections;
using System.Reflection;

namespace Ergo.Lang;

public abstract class ErgoTypeResolver<T> : ITypeResolver
{
    public Type Type => typeof(T);
    public abstract TermMarshalling Marshalling { get; }

    protected readonly Func<object, Atom> GetFunctor;
    protected readonly Lazy<bool> IsAtomic;
    protected readonly Lazy<ITerm> DefaultValue;

    public ErgoTypeResolver()
    {
        if (Type == typeof(char) || Type == typeof(string))
        {
            IsAtomic = new(() => true);
            GetFunctor = o => new Atom(o?.ToString() ?? string.Empty);
            DefaultValue = new(() => ToTerm(string.Empty));
        }
        else if (Type == typeof(bool))
        {
            IsAtomic = new(() => true);
            GetFunctor = o => new Atom(o ?? false);
            DefaultValue = new(() => ToTerm(false));
        }
        else if (Type.IsPrimitive)
        {
            IsAtomic = new(() => true);
            GetFunctor = o => new Atom(Convert.ChangeType(o ?? 0, typeof(double)));
            DefaultValue = new(() => ToTerm(0d));
        }
        else if (Type.IsEnum)
        {
            IsAtomic = new(() => true);
            GetFunctor = o => new Atom(o?.ToString() ?? Enum.GetNames(Type).First());
            DefaultValue = new(() => ToTerm(Enum.GetNames(Type).First()));
        }
        else if (Type.IsArray)
        {
            IsAtomic = new(() => false);
            GetFunctor = o => WellKnown.Functors.List.First();
            DefaultValue = new(() => ToTerm(Array.CreateInstance(Type.GetElementType(), 0)));
        }
        else
        {
            IsAtomic = new(() => GetMembers().Count() == 0);
            GetFunctor = o => new Atom(Type.Name.ToLower());
            DefaultValue = new(() => ToTerm(Activator.CreateInstance(Type)));
        }
    }

    public abstract Type GetMemberType(string name);
    public abstract TermAttribute GetMemberAttribute(string name);
    public abstract object GetMemberValue(string name, object instance);
    public abstract void SetMemberValue(string name, object instance, object value);
    public abstract IEnumerable<string> GetMembers();
    public abstract ITerm GetArgument(string name, ITerm value);
    public abstract ITerm TransformMember(string name, ITerm value);
    public abstract ITerm TransformTerm(Atom functor, ITerm[] args);
    public abstract Type GetParameterType(string name, ConstructorInfo info);

    public virtual ITerm ToTerm(object o, Maybe<Atom> overrideFunctor = default, Maybe<TermMarshalling> overrideMarshalling = default)
    {
        if (o is null) return WellKnown.Literals.Discard;
        if (o != null && o.GetType() != Type) throw new ArgumentException(null, o.ToString());
        // Check if the [Term] attribute is applied at the type level,
        // If so, assume that's what we want unless overrideFunctor is not None.
        var attr = Type.GetCustomAttribute<TermAttribute>();
        var functor = overrideFunctor.Reduce(
            some => !Type.IsArray ? some : GetFunctor(o),
            () => !Type.IsArray && attr?.Functor is { } f ? new Atom(f) : GetFunctor(o)
        );
        var marshalling = overrideMarshalling.Reduce(some => some, () => Marshalling);

        if (IsAtomic.Value) return functor;

        ITerm[] args;
        if (Type.IsArray)
        {
            // Collections are handled recursively
            args = o == null ? Array.Empty<ITerm>() : ((IEnumerable)o).Cast<object>()
                .Select(x => TermMarshall.ToTerm(x, Type.GetElementType(), overrideFunctor, Maybe.Some(marshalling)))
                .ToArray();
        }
        else
        {
            args = GetMembers().Select(
                m =>
                {
                    var attr = GetMemberAttribute(m) ?? GetMemberType(m).GetCustomAttribute<TermAttribute>();
                    var overrideMemberFunctor = attr is null ? Maybe<Atom>.None : Maybe.Some(new Atom(attr.Functor));
                    var overrideMemberMarshalling = attr is null ? Maybe.Some(marshalling) : Maybe.Some(attr.Marshalling);
                    var memberValue = o == null ? null : GetMemberValue(m, o);
                    var term = TermMarshall.ToTerm(memberValue, GetMemberType(m), overrideMemberFunctor, overrideMemberMarshalling);
                    var member = TransformMember(m, term);
                    if (List.TryUnfold(member, out var list))
                    {
                        member = new List(list.Contents.Select(x =>
                        {
                            if (x is Complex cplx)
                                return cplx.WithFunctor(new(attr?.Functor ?? cplx.Functor.Value));
                            return x;
                        }).ToArray()).Root;
                    }

                    return member;
                }
            ).ToArray();
        }

        return TransformTermInternal(functor, args);

        ITerm TransformTermInternal(Atom functor, ITerm[] args)
        {
            if (args.Length == 0)
                return new Atom(functor);

            if (WellKnown.Functors.List.Contains(functor))
                return new List(args).Root;

            return TransformTerm(functor, args);
        }

    }

    public virtual object FromTerm(ITerm t)
    {
        if (IsAtomic.Value)
        {
            if (Type.IsEnum)
            {
                return Enum.Parse(Type, ((Atom)t).Value.ToString());
            }

            return t switch
            {
                Complex cplx when cplx.Arguments.Length == 1 => ((Atom)cplx.Arguments[0]).Value,
                Atom a => a.Value,
                _ => null
            };
        }

        if (Type.Namespace == null)
        {
            // anonymous/record type
            var args = GetMembers();
            var constructor = Type.GetConstructors().Single();
            var instance = constructor.Invoke(args.Select(name =>
            {
                var type = GetMemberType(name);
                var paramType = GetParameterType(name, constructor);
                var arg = GetArgument(name, t);
                var value = TermMarshall.FromTerm(arg, type, Maybe.Some(Marshalling));
                value = Convert.ChangeType(value, paramType);
                return value;
            }).ToArray());
            return instance;
        }
        else
        {
            if (Type.IsArray && List.TryUnfold(t, out var list))
            {
                var instance = Array.CreateInstance(Type.GetElementType(), list.Contents.Length);
                for (var i = 0; i < list.Contents.Length; i++)
                {
                    var obj = TermMarshall.FromTerm(list.Contents[i], Type.GetElementType(), Maybe.Some(Marshalling));
                    instance.SetValue(obj, i);
                }

                return instance;
            }
            else
            {
                var instance = Activator.CreateInstance(Type);
                var args = GetMembers();
                foreach (var name in args)
                {
                    var type = GetMemberType(name);
                    var arg = GetArgument(name, (Complex)t);
                    var value = TermMarshall.FromTerm(arg, type, Maybe.Some(Marshalling));
                    value = Convert.ChangeType(value, type);
                    SetMemberValue(name, instance, value);
                }

                return instance;
            }
        }
    }
}
