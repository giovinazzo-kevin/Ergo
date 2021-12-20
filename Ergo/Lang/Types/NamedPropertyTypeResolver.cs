using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    internal class NamedPropertyTypeResolver<T> : ITypeResolver
    {
        public Type Type => typeof(T);

        protected readonly ConcurrentDictionary<string, PropertyInfo> Properties;
        protected readonly Func<object, Atom> GetFunctor;
        protected readonly bool IsAtomic;


        public NamedPropertyTypeResolver()
        {
            Properties = new(Type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(p => p.Name));
            if(Type == typeof(char) || Type == typeof(string))
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
                GetFunctor = o => new Atom((double)o);
            }
            else
            {
                IsAtomic = false;
                GetFunctor = o => new Atom(Type.Name.ToLower());
            }
        }

        public object FromTerm(Term t)
        {
            if (IsAtomic)
            {
                return ((Atom)t).Value;
            }
            if(Type.Namespace == null)
            {
                // anonymous type
                var args = ((Complex)t).Arguments.ToDictionary(a => (string)((Complex)a).Functor.Value, a => ((Complex)a).Arguments[0]);
                var constructor = Type.GetConstructors().Single();
                var constructorParameters = constructor.GetParameters().ToDictionary(p => p.Name);
                var instance = constructor.Invoke(args.Select(a =>
                {
                    var value = TermMarshall.FromTerm(args[a.Key], Properties[a.Key].PropertyType, TermMarshall.MarshallingMode.Named);
                    value = Convert.ChangeType(value, constructorParameters[a.Key].ParameterType);
                    return value;
                }).ToArray());
                return instance;
            }
            else
            {
                var instance = Activator.CreateInstance(Type);
                var args = ((Complex)t).Arguments.ToDictionary(a => (string)((Complex)a).Functor.Value, a => ((Complex)a).Arguments[0]);
                foreach (var prop in Properties.Values)
                {
                    var value = TermMarshall.FromTerm(args[prop.Name], prop.PropertyType, TermMarshall.MarshallingMode.Named);
                    value = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(instance, value);
                }
                return instance;
            }
        }

        public Term ToTerm(object o)
        {
            if (o.GetType() != Type) throw new ArgumentException(null, nameof(o));

            var functor = GetFunctor(o);
            if (IsAtomic) return functor;

            return new Complex(functor, Properties.Values.Select(
                v => (Term)new Complex(new(v.Name), TermMarshall.ToTerm(v.GetValue(o), TermMarshall.MarshallingMode.Named))
            ).ToArray());
        }
    }
}
