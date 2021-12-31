using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Lang.Ast
{
    public interface ISequence : IExplainable
    {
        ITerm Root { get; }
        Atom Functor { get; }
        ImmutableArray<ITerm> Contents { get; }
        ITerm EmptyElement { get; }
        bool IsEmpty { get; }
        public bool IsParenthesized { get; }

        IEnumerable<ITerm> GetContents(ITerm root, ITerm emptyElem)
        {
            while (TryUnwrap(root, emptyElem, out var arg, out root))
            {
                yield return arg;
            }

            static bool TryUnwrap(ITerm root, ITerm emptyElement, out ITerm arg, out ITerm next)
            {
                arg = next = default;
                if (root.Equals(emptyElement))
                    return false;
                if (root is not Complex cplx)
                    return false;
                arg = cplx.Arguments[0];
                next = cplx.Arguments[1];
                return true;
            }
        }

        ISequence Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
        ISequence Substitute(IEnumerable<Substitution> subs);

        static ITerm Fold(Atom functor, ITerm emptyElement, ImmutableArray<ITerm> args)
        {
            if (args.Length == 0)
                return emptyElement;
            if (args.Length == 1)
                return new Complex(functor, args[0], emptyElement);
            return args
                .Append(emptyElement)
                .Reverse()
                .Aggregate((a, b) => new Complex(functor, b, a)
                    .AsOperator(OperatorAffix.Infix));
        }
    }

}
