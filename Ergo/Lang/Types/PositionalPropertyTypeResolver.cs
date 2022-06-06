using Ergo.Lang.Ast;
using System;
using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    internal class PositionalPropertyTypeResolver<T> : ITypeResolver
    {
        public Type Type => typeof(T);

        protected readonly PropertyInfo[] Properties;
        protected readonly Func<object, Atom> GetFunctor;
        protected readonly bool IsAtomic;


        public PositionalPropertyTypeResolver()
        {
            Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
            else if(Type.IsEnum)
            {
                IsAtomic = true;
                GetFunctor = o => new Atom(o.ToString());
            }
            else
            {
                IsAtomic = false;
                GetFunctor = o => new Atom(Type.Name.ToLower());
            }
        }

        public object FromTerm(ITerm t)
        {
            if (IsAtomic)
            {
                if(Type.IsEnum)
                {
                    return Enum.Parse(Type, ((Atom)t).Value.ToString());
                }
                return ((Atom)t).Value;
            }
            if(Type.Namespace == null)
            {
                // anonymous type
                var constructor = Type.GetConstructors().Single();
                var constructorParameters = constructor.GetParameters();
                var args = ((Complex)t).Arguments.Select((a, i) =>
                {
                    var value = TermMarshall.FromTerm(a, Properties[i].PropertyType, TermMarshall.MarshallingMode.Positional);
                    value = Convert.ChangeType(value, constructorParameters[i].ParameterType);
                    return value;
                })
                    .ToArray();
                var instance = constructor.Invoke(args);
                return instance;
            }
            else
            {
                if(Type.IsArray && List.TryUnfold(t, out var list))
                {
                    var elemType = Type.GetElementType();
                    var instance = Array.CreateInstance(elemType, list.Contents.Length);
                    for (int i = 0; i < list.Contents.Length; i++)
                    {
                        var value = TermMarshall.FromTerm(list.Contents[i], elemType, TermMarshall.MarshallingMode.Positional);
                        value = Convert.ChangeType(value, elemType);
                        instance.SetValue(value, i);
                    }
                    return instance;
                }
                else
                {
                    var instance = Activator.CreateInstance(Type);
                    var args = ((Complex)t).Arguments;
                    for (int i = 0; i < Properties.Length; i++)
                    {
                        var value = TermMarshall.FromTerm(args[i], Properties[i].PropertyType, TermMarshall.MarshallingMode.Positional);
                        value = Convert.ChangeType(value, Properties[i].PropertyType);
                        Properties[i].SetValue(instance, value);
                    }
                    return instance;
                }
            }
        }

        public ITerm ToTerm(object o)
        {
            if (o.GetType() != Type) throw new ArgumentException(null, nameof(o));

            var functor = GetFunctor(o);
            if (IsAtomic) return functor;

            return new Complex(functor, Properties.Select(
                v => TermMarshall.ToTerm(v.GetValue(o), v.PropertyType, TermMarshall.MarshallingMode.Positional)
            ).ToArray());
        }
    }
}
