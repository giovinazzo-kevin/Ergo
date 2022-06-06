using Ergo.Lang.Ast;
using System;
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
        protected readonly bool IsAtomic;

        public ErgoTypeResolver()
        {
            if (Type == typeof(char) || Type == typeof(string))
            {
                IsAtomic = true;
                GetFunctor = o => new Atom(o.ToString());
            }
            else if (Type == typeof(bool))
            {
                IsAtomic = true;
                GetFunctor = o => new Atom(o);
            }
            else if (Type.IsPrimitive)
            {
                IsAtomic = true;
                GetFunctor = o => new Atom(Convert.ChangeType(o, typeof(double)));
            }
            else if (Type.IsEnum)
            {
                IsAtomic = true;
                GetFunctor = o => new Atom(o.ToString());
            }
            else
            {
                var attr = Type.GetCustomAttribute<TermAttribute>();
                IsAtomic = false;
                GetFunctor = o => new Atom(attr?.Functor ?? Type.Name.ToLower());
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
        protected abstract ITerm TransformTerm(Atom functor, ITerm[] args);

        public virtual ITerm ToTerm(object o)
        {
            if (o.GetType() != Type) throw new ArgumentException(null, nameof(o));

            var functor = GetFunctor(o);
            if (IsAtomic) return functor;

            var args = GetMembers().Select(
                m =>
                {
                    var attr = GetMemberAttribute(m) ?? GetMemberType(m).GetCustomAttribute<TermAttribute>();
                    return TransformMember(m, TermMarshall.ToTerm(GetMemberValue(m, o), GetMemberType(m), attr?.Marshalling ?? Marshalling));
                }
            ).ToArray();

            return TransformTerm(functor, args);
        }


        public virtual object FromTerm(ITerm t)
        {
            if (IsAtomic)
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
                var constructorParameters = constructor.GetParameters().ToDictionary(p => p.Name);
                var instance = constructor.Invoke(args.Select(name =>
                {
                    var arg = GetArgument(name, (Complex)t);
                    var value = TermMarshall.FromTerm(arg, GetMemberType(name), Marshalling);
                    value = Convert.ChangeType(value, constructorParameters[name].ParameterType);
                    return value;
                }).ToArray());
                return instance;
            }
            else
            {
                if (Type.IsArray && List.TryUnfold(t, out var list))
                {
                    var instance = Array.CreateInstance(Type, list.Contents.Length);
                    for (int i = 0; i < list.Contents.Length; i++)
                    {
                        instance.SetValue(list.Contents[i], i);
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
                        var value = TermMarshall.FromTerm(arg, GetMemberType(name), Marshalling);
                        value = Convert.ChangeType(value, GetMemberType(name));
                        SetMemberValue(name, instance, value);
                    }
                    return instance;
                }
            }
        }
    }
}
