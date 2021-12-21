using System;
using System.Collections.Generic;
using System.Linq;

namespace Ergo.Lang
{
    public interface ITerm : IComparable<ITerm>, IEquatable<ITerm>
    {
        bool IsGround { get; }
        IEnumerable<Variable> Variables { get; }

        string Explain();
        ITerm Qualify(Atom m);
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
