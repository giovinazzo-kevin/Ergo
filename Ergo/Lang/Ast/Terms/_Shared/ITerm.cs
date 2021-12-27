﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang.Ast
{
    public interface ITerm : IComparable<ITerm>, IEquatable<ITerm>
    {
        bool IsGround { get; }
        bool IsQualified { get; }
        IEnumerable<Variable> Variables { get; }

        string Explain();
        bool TryQualify(Atom m, out ITerm qualified)
        {
            if(IsQualified)
            {
                qualified = this;
                return false;
            }
            qualified = new Complex(new(":"), m, this);
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
