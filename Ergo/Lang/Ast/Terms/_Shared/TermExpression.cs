using Newtonsoft.Json.Linq;

namespace Ergo.Lang.Ast;

// TODO: Make this architecture efficient in the case where multiple instances of the same variable are unified
// Lay down on immutability if necessary
public abstract class TermExpression(List<TermExpression> children)
{
    public List<TermExpression> Children { get; } = children;
    public abstract Maybe<TermExpression> Unify(TermExpression other);
}

public class AtomNode(object value) : TermExpression([])
{
    public object Value { get; private set; } = value;

    public override Maybe<TermExpression> Unify(TermExpression other)
    {
        if (ReferenceEquals(this, other))
            return other;
        if (other is VariableNode var)
            return var.Unify(this);
        if (other is not AtomNode atom || !Equals(atom.Value, Value))
            return default;
        return other;
    }
}

public class VariableNode(string name) : TermExpression([])
{
    public string Name { get; private set; } = name;

    public override Maybe<TermExpression> Unify(TermExpression other)
    {
        return other;
    }
}

public class ComplexNode(AtomNode functor, List<TermExpression> children) : TermExpression(children)
{
    public AtomNode Functor { get; private set; } = functor;
    public int Arity => Children.Count;
    public override Maybe<TermExpression> Unify(TermExpression other)
    {
        if (ReferenceEquals(this, other))
            return Maybe.Some(other);
        if (other is VariableNode var)
            return var.Unify(this);
        if (other is not ComplexNode cplx || cplx.Arity != Arity)
            return default;
        if(!Functor.Unify(cplx.Functor).TryGetValue(out var functorUnif))
            return default;
        var args = new List<TermExpression>(Children.Count);
        for (int i = 0; i < Children.Count; i++) {
            if (!Children[i].Unify(other.Children[i]).TryGetValue(out var childUnif))
                return default;
            args.Add(childUnif);
        }
        return new ComplexNode((AtomNode)functorUnif, args);
    }
}

public static class TermExtensions
{
    public static TermExpression ToTermExpression(this ITerm term)
    {
        if (term is Atom a)
            return new AtomNode(a.Value);
        if (term is Variable v)
            return new VariableNode(v.Name);
        if (term is Complex c)
            return new ComplexNode((AtomNode)c.Functor.ToTermExpression(), c.Arguments.Select(ToTermExpression).ToList());
        throw new NotSupportedException();
    }
    public static ITerm ToTerm(this TermExpression expr)
    {
        if (expr is AtomNode a)
            return new Atom(a.Value);
        if (expr is VariableNode v)
            return new Variable(v.Name);
        if (expr is ComplexNode c)
            return new Complex((Atom)c.Functor.ToTerm(), c.Children.Select(ToTerm).ToArray());
        throw new NotSupportedException();
    }
}