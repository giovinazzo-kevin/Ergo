using Ergo.Lang.Ast;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{

    public abstract class ErgoTypeResolver<T> : ITypeResolver
    {
        public Type Type => typeof(T);
        public abstract TermMarshalling Marshalling { get; }

        protected readonly Func<object, Atom> GetFunctor;
        protected readonly Lazy<bool> IsAtomic;

        public ErgoTypeResolver()
        {
            if (Type == typeof(char) || Type == typeof(string))
            {
                IsAtomic = new(() => true);
                GetFunctor = o => new Atom(o.ToString() ?? String.Empty);
            }
            else if (Type == typeof(bool))
            {
                IsAtomic = new(() => true);
                GetFunctor = o => new Atom(o ?? false);
            }
            else if (Type.IsPrimitive)
            {
                IsAtomic = new(() => true);
                GetFunctor = o => new Atom(Convert.ChangeType(o ?? 0, typeof(double)));
            }
            else if (Type.IsEnum)
            {
                IsAtomic = new(() => true);
                GetFunctor = o => new Atom(o?.ToString() ?? Enum.GetNames(Type).First());
            }
            else if(Type.IsArray)
            {
                IsAtomic = new(() => false);
                GetFunctor = o => WellKnown.Functors.List.First();
            }
            else
            {
                IsAtomic = new(() => GetMembers().Count() == 0);
                GetFunctor = o => new Atom(Type.Name.ToLower());
            }
        }

        protected abstract Type GetMemberType(string name);
        protected abstract TermAttribute GetMemberAttribute(string name);
        protected abstract object GetMemberValue(string name, object instance);
        protected abstract void SetMemberValue(string name, object instance, object value);
        protected abstract IEnumerable<string> GetMembers();
        protected abstract IEnumerable<string> GetArguments(Complex value);
        protected abstract ITerm GetArgument(string name, Complex value);
        protected abstract ITerm TransformMember(string name, ITerm value);
        protected abstract Type GetParameterType(string name, ConstructorInfo info);

        public virtual ITerm ToTerm(object o, Maybe<Atom> overrideFunctor = default, Maybe<TermMarshalling> overrideMarshalling = default)
        {
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
            if(Type.IsArray)
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
            return TransformTerm(functor, args);

            ITerm TransformTerm(Atom functor, ITerm[] args)
            {
                if (args.Length == 0)
                    return new Atom(functor);

                if (WellKnown.Functors.List.Contains(functor))
                    return new List(args).Root;

                return new Complex(functor, args)
                    .AsParenthesized(WellKnown.Functors.Conjunction.Contains(functor));
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
                return ((Atom)t).Value;
            }
            if (Type.Namespace == null)
            {
                // anonymous type
                var args = GetArguments((Complex)t);
                var constructor = Type.GetConstructors().Single();
                var instance = constructor.Invoke(args.Select(name =>
                {
                    var arg = GetArgument(name, (Complex)t);
                    var value = TermMarshall.FromTerm(arg, GetMemberType(name), Maybe.Some(Marshalling));
                    value = Convert.ChangeType(value, GetParameterType(name, constructor));
                    return value;
                }).ToArray());
                return instance;
            }
            else
            {
                if (Type.IsArray && List.TryUnfold(t, out var list))
                {
                    var instance = Array.CreateInstance(Type.GetElementType(), list.Contents.Length);
                    for (int i = 0; i < list.Contents.Length; i++)
                    {
                        var obj = TermMarshall.FromTerm(list.Contents[i], Type.GetElementType(), Maybe.Some(Marshalling));
                        instance.SetValue(obj, i);
                    }
                    return instance;
                }
                else
                {
                    var instance = Activator.CreateInstance(Type);
                    var args = GetArguments((Complex)t);
                    foreach (var name in args)
                    {
                        var arg = GetArgument(name, (Complex)t);
                        var value = TermMarshall.FromTerm(arg, GetMemberType(name), Maybe.Some(Marshalling));
                        value = Convert.ChangeType(value, GetMemberType(name));
                        SetMemberValue(name, instance, value);
                    }
                    return instance;
                }
            }
        }
    }
}
