using Ergo.Lang.Ast.Terms.Interfaces;

namespace Ergo.Lang.Ast;

public interface ITerm : IComparable<ITerm>, IEquatable<ITerm>, IExplainable
{
    bool IsGround { get; }
    bool IsQualified { get; }
    bool IsParenthesized { get; }
    IEnumerable<Variable> Variables { get; }
    Maybe<IAbstractTerm> AbstractForm { get; }

    Maybe<Atom> GetFunctor() => this switch
    {
        Atom a => a,
        Complex c => c.Functor,
        _ => Maybe<Atom>.None
    };

    ITerm[] GetArguments() => this switch
    {
        Complex c => c.Arguments,
        _ => Array.Empty<ITerm>()
    };
    public ITerm GetVariant() => this switch
    {
        Complex c => c.WithArguments(c.Arguments.Select(x => x.GetVariant()).ToArray()),
        Variable v => v,
        Atom a => new Variable(a.Explain().ToUpper()),
        var x => x
    };

    ITerm WithFunctor(Atom newFunctor) => this switch
    {
        Atom => newFunctor,
        Variable v => v,
        Complex c => c.WithFunctor(newFunctor),
        var x => x
    };

    ITerm WithAbstractForm(Maybe<IAbstractTerm> abs) => this switch
    {
        Atom a => a.WithAbstractForm(abs),
        Variable v => v.WithAbstractForm(abs),
        Complex c => c.WithAbstractForm(abs),
        var x => x
    };
    ITerm AsParenthesized(bool parens) => this switch
    {
        Atom a => a,
        Variable v => v,
        Complex c => c.AsParenthesized(parens),
        var x => x
    };

    ITerm Qualified(Atom m)
    {
        return GetQualification(out var head)
            .Select(some => Inner(head))
            .GetOr(Inner(this));
        ITerm Inner(ITerm t)
        {
            return new Complex(WellKnown.Functors.Module.First(), m, t)
                .AsOperator(OperatorAffix.Infix);
        }
    }
    Maybe<Atom> GetQualification(out ITerm head)
    {
        head = this;
        if (!IsQualified || this is not Complex cplx || cplx.Arguments.Length != 2 || cplx.Arguments[0] is not Atom module)
            return default;
        head = cplx.Arguments[1];
        return Maybe.Some(module);
    }

    ITerm Substitute(Substitution s);
    ITerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null);
    ITerm Concat(params ITerm[] next)
    {
        if (this is Complex cplx)
            return cplx.WithArguments(cplx.Arguments.Concat(next).ToArray());
        if (this is Atom a)
            return new Complex(a, next);
        return this;
    }

    ITerm Substitute(IEnumerable<Substitution> subs)
    {
        var steps = subs.ToDictionary(s => s.Lhs);
        var variables = Variables.Where(var => steps.ContainsKey(var));
        var @base = this;
        while (variables.Any())
        {
            foreach (var var in variables)
            {
                @base = @base.Substitute(steps[var]);
            }

            variables = @base.Variables.Where(var => steps.ContainsKey(var));
        }
        return @base;
    }
}

