using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    internal class TypeResolver<T> : ITypeResolver
    {
        public Type Type => typeof(T);

        protected readonly ConcurrentDictionary<string, PropertyInfo> Properties;
        protected readonly Func<object, Atom> GetFunctor;
        protected readonly bool IsAtomic;


        public TypeResolver()
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
            var instance = Activator.CreateInstance(Type);
            var args = ((Complex)t).Arguments.ToDictionary(a => (string)((Complex)a).Functor.Value, a => ((Complex)a).Arguments[0]);
            foreach (var prop in Properties.Values)
            {
                prop.SetValue(instance, FromTerm(args[prop.Name]));
            }
            return instance;
        }

        public Term ToTerm(object o)
        {
            if (o.GetType() != Type) throw new ArgumentException(null, nameof(o));

            var functor = GetFunctor(o);
            if (IsAtomic) return functor;

            return new Complex(functor, Properties.Values.Select(
                v => (Term)new Complex(new(v.Name), TypeMarshall.ToTerm(v.GetValue(o)))
            ).ToArray());
        }
    }
}
