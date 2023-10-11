using Ergo.Lang.Ast.Terms.Interfaces;
using PeterO.Numbers;
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
        else if (Type.IsEnum)
        {
            IsAtomic = new(() => true);
            GetFunctor = o => new Atom(o?.ToString().ToErgoCase() ?? Enum.GetNames(Type).First().ToErgoCase());
            DefaultValue = new(() => ToTerm(Enum.GetNames(Type).First().ToErgoCase()));
        }
        else if (Type.IsNumericType())
        {
            IsAtomic = new(() => true);
            GetFunctor = o => new Atom(Convert.ToDecimal(o ?? 0M));
            DefaultValue = new(() => ToTerm(0d));
        }
        else if (Type == typeof(EDecimal))
        {
            IsAtomic = new(() => true);
            GetFunctor = o => new Atom(o);
            DefaultValue = new(() => ToTerm(EDecimal.Zero));
        }
        else if (Type.IsArray)
        {
            IsAtomic = new(() => false);
            GetFunctor = o => WellKnown.Functors.HeadTail.First();
            DefaultValue = new(() => ToTerm(Array.CreateInstance(Type.GetElementType(), 0)));
        }
        else if (Type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)) is { } iList)
        {
            IsAtomic = new(() => false);
            GetFunctor = o => WellKnown.Functors.HeadTail.First();
            DefaultValue = new(() => ToTerm(Array.CreateInstance(iList.GetGenericArguments()[0], 0)));
        }
        else
        {
            IsAtomic = new(() => GetMembers().Count() == 0);
            GetFunctor = o => new Atom(Type.Name.ToErgoCase());
            DefaultValue = new(() => ToTerm(Activator.CreateInstance(Type)));
        }
    }

    public abstract Type GetMemberType(string name);
    public abstract TermAttribute GetMemberAttribute(string name);
    public abstract object GetMemberValue(string name, object instance);
    public abstract void SetMemberValue(string name, object instance, object value);
    public abstract IEnumerable<string> GetMembers();
    public abstract ITerm GetArgument(string name, ITerm value);
    public abstract ITerm TransformMember(string name, Maybe<string> key, ITerm value);
    public abstract ITerm TransformTerm(Atom functor, ITerm[] args);
    public abstract Type GetParameterType(string name, ConstructorInfo info);
    public abstract ITerm CycleDetectedLiteral(Atom functor);

    public virtual ITerm ToTerm(object o, Maybe<Atom> overrideFunctor = default, Maybe<TermMarshalling> overrideMarshalling = default, TermMarshallingContext ctx = null)
    {
        if (o is null)
            return WellKnown.Literals.Discard;
        if (o.GetType().GetInterface(nameof(ITerm)) is not null)
            return (ITerm)o;
        if (o.GetType().GetInterface(nameof(IAbstractTerm)) is not null)
            return ((IAbstractTerm)o).CanonicalForm;
        if (!o.GetType().IsAssignableTo(Type))
            throw new ArgumentException(null, o.ToString());
        // Check if the [Term] attribute is applied at the type level,
        // If so, assume that's what we want unless overrideFunctor is not None.
        var attr = Type.GetCustomAttribute<TermAttribute>();
        var functor = overrideFunctor.Map(
            some => !IsCollectionType(Type) ? Maybe.Some(some) : default,
            () => !IsCollectionType(Type) && attr?.Functor is { } f ? Maybe.Some(new Atom(f)) : default
        );
        var marshalling = overrideMarshalling.GetOr(Marshalling);

        if (TermMarshallingContext.MayCauseCycles(o.GetType()))
        {
            if (ctx.ReferenceStack.Contains(o))
                return CycleDetectedLiteral(GetFunctor(o));

            ctx.ReferenceStack.Push(o);
        }
        if (IsAtomic.Value)
            return functor.GetOr(GetFunctor(o));

        ITerm[] args;
        if (Type.IsArray)
        {
            // Collections are handled recursively
            args = o == null ? Array.Empty<ITerm>() : ((IEnumerable)o).Cast<object>()
                .Select(x => TermMarshall.ToTerm(x, Type.GetElementType(), overrideFunctor, marshalling, ctx))
                .ToArray();
        }
        else if (Type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)) is { } iList)
        {
            // Collections are handled recursively
            args = o == null ? Array.Empty<ITerm>() : ((IEnumerable)o).Cast<object>()
                .Select(x => TermMarshall.ToTerm(x, iList.GetGenericArguments()[0], overrideFunctor, marshalling, ctx))
                .ToArray();
        }
        else
        {
            args = GetMembers().Select(
                m =>
                {
                    var attr = GetMemberAttribute(m) ?? GetMemberType(m).GetCustomAttribute<TermAttribute>();
                    var overrideMemberFunctor = attr?.Functor is null ? Maybe<Atom>.None : new Atom(attr.Functor);
                    var overrideMemberMarshalling = attr is null ? marshalling : attr.Marshalling;
                    var ovrrideMemberKey = attr is null ? Maybe<string>.None : attr.Key ?? m;
                    var memberValue = o == null ? null : GetMemberValue(m, o);
                    var term = TermMarshall.ToTerm(memberValue, GetMemberType(m), overrideMemberFunctor, overrideMemberMarshalling, ctx);
                    var member = TransformMember(m, ovrrideMemberKey, term);
                    if (member.IsAbstract<List>().TryGetValue(out var list))
                    {
                        member = new List(list.Contents.Select(x =>
                        {
                            if (x is Complex cplx)
                                return cplx.WithFunctor(new(attr?.Functor ?? cplx.Functor.Value));
                            return x;
                        })).CanonicalForm;
                    }
                    return member;

                }
            ).ToArray();
        }
        if (TermMarshallingContext.MayCauseCycles(o.GetType()))
        {
            ctx.ReferenceStack.Pop();
        }
        return TransformTermInternal(functor.GetOr(GetFunctor(o)), args);

        ITerm TransformTermInternal(Atom functor, ITerm[] args)
        {
            if (args.Length == 0)
                return new Atom(functor);
            if (WellKnown.Functors.HeadTail.Contains(functor))
                return new List(args).CanonicalForm;
            return TransformTerm(functor, args);
        }

    }

    static bool IsCollectionType(Type t) => t.IsArray
        || t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyList<>));

    public virtual object FromTerm(ITerm t)
    {
        if (t is Variable)
            return null;
        if (IsAtomic.Value)
        {
            if (Type.IsEnum)
            {
                return Enum.Parse(Type, ((Atom)t).Value.ToString().ToCSharpCase(), ignoreCase: true);
            }

            return t switch
            {
                Ast.Complex cplx when cplx.Arguments.Length == 1 => ((Atom)cplx.Arguments[0]).Value,
                Atom a => a.Value switch
                {
                    EDecimal d when Type.IsNumericType() => Convert.ChangeType(d.ToDecimal(), Type),
                    var v => v
                },
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
                var value = TermMarshall.FromTerm(arg, type, Marshalling);
                if (value is EDecimal dec)
                    value = Convert.ChangeType(dec.ToDecimal(), paramType);
                else
                    value = Convert.ChangeType(value, paramType);
                return value;
            }).ToArray());
            return instance;
        }
        else
        {
            if (Type.IsArray && t.IsAbstract<List>().TryGetValue(out var list))
            {
                var instance = Array.CreateInstance(Type.GetElementType(), list.Contents.Length);
                for (var i = 0; i < list.Contents.Length; i++)
                {
                    var obj = TermMarshall.FromTerm(list.Contents[i], Type.GetElementType(), Marshalling);
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
                    var arg = GetArgument(name, (Ast.Complex)t);
                    var value = TermMarshall.FromTerm(arg, type);
                    if (value is null)
                        continue;
                    if (value is EDecimal dec)
                        value = Convert.ChangeType(dec.ToDecimal(), type);
                    else
                        value = Convert.ChangeType(value, type);
                    SetMemberValue(name, instance, value);
                }

                return instance;
            }
        }
    }
}
