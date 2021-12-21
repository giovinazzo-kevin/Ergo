using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct UntypedSequence : ISequence
    {
        public ITerm Root { get; }
        public Atom Functor { get; }
        public ITerm[] Contents { get; }
        public ITerm EmptyElement { get; }
        public bool IsEmpty { get; }

        public UntypedSequence(Atom functor, ITerm empty, params ITerm[] args)
        {
            Functor = functor;
            EmptyElement = empty;
            Contents = args;
            IsEmpty = args.Length == 0;
            Root = ISequence.Fold(Functor, EmptyElement, args);
        }

        public string Explain()
        {
            if (IsEmpty)
            {
                return EmptyElement.Explain();
            }
            var joined = string.Join(", ", Contents.Select(t => t.Explain()));
            if (Contents.Length != 1)
            {
                return $"({joined})";
            }
            return joined;
        }

        public ISequence Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null) =>
            new UntypedSequence(Functor, EmptyElement, Contents.Select(arg => arg.Instantiate(ctx, vars)).ToArray());

        public ISequence Substitute(IEnumerable<Substitution> subs) =>
            new UntypedSequence(Functor, EmptyElement, Contents.Select(arg => arg.Substitute(subs)).ToArray());
    }
}
