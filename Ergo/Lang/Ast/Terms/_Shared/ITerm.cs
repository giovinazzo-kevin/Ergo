using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang.Ast
{
    public interface ITerm : IComparable<ITerm>, IEquatable<ITerm>, IExplainable
    {
        bool IsGround { get; }
        bool IsQualified { get; }
        bool IsParenthesized { get; }
        IEnumerable<Variable> Variables { get; }

        bool TryQualify(Atom m, out ITerm qualified)
        {
            if(IsQualified)
            {
                qualified = this;
                return false;
            }
            qualified = new Complex(new(":"), m, this)
                .AsOperator(OperatorAffix.Infix);
            return true;
        }
        bool TryGetQualification(out Atom module, out ITerm value)
        {
            if(!IsQualified || this is not Complex cplx)
            {
                module = default;
                value = this;
                return false;
            }
            module = (Atom)cplx.Arguments[0];
            value = cplx.Arguments[1];
            return true;
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

}
